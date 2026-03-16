using Vali_Validation.Core.Exceptions;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Vali_Validation.Tests.Models;
using Xunit;

namespace Vali_Validation.Tests;

// Validators for advanced feature tests

public class AddressValidator : AbstractValidator<AddressDto>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
    }
}

public class PersonWithAddressValidator : AbstractValidator<PersonDto>
{
    public PersonWithAddressValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Address).SetValidator(new AddressValidator());
    }
}

public class TagsValidator : AbstractValidator<PersonDto>
{
    public TagsValidator()
    {
        RuleForEach(x => x.Tags!).NotEmpty().MinimumLength(2);
    }
}

public class BasePersonValidator : AbstractValidator<PersonDto>
{
    public BasePersonValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Age).Positive();
    }
}

public class ExtendedPersonValidator : AbstractValidator<PersonDto>
{
    public ExtendedPersonValidator()
    {
        Include(new BasePersonValidator());
        RuleFor(x => x.Email).Email();
    }
}

public class OverrideNameValidator : AbstractValidator<PersonDto>
{
    public OverrideNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .OverridePropertyName("FullName");
    }
}

public class ThrowingValidator : AbstractValidator<PersonDto>
{
    public ThrowingValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class AsyncThrowingValidator : AbstractValidator<PersonDto>
{
    public AsyncThrowingValidator()
    {
        RuleFor(x => x.Email).MustAsync(async email =>
        {
            await Task.Delay(1);
            return !string.IsNullOrEmpty(email) && email!.Contains("@");
        });
    }
}

public class AdvancedFeaturesTests
{
    [Fact]
    public void RuleForEach_WhenAllElementsValid_Passes()
    {
        var validator = new TagsValidator();
        var person = new PersonDto { Tags = new List<string> { "tag1", "tag2", "tag3" } };
        var result = validator.Validate(person);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RuleForEach_WhenElementInvalid_ReportsIndexedError()
    {
        var validator = new TagsValidator();
        var person = new PersonDto { Tags = new List<string> { "tag1", "", "tag3" } };
        var result = validator.Validate(person);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Tags[1]"));
    }

    [Fact]
    public void RuleForEach_WhenMultipleElementsInvalid_ReportsAllErrors()
    {
        var validator = new TagsValidator();
        var person = new PersonDto { Tags = new List<string> { "", "x", "tag3" } };
        var result = validator.Validate(person);
        Assert.False(result.IsValid);
        // Tags[0] empty, Tags[1] too short
        Assert.True(result.HasErrorFor("Tags[0]") || result.HasErrorFor("Tags[1]"));
    }

    [Fact]
    public async Task SetValidator_WhenNestedValid_Passes()
    {
        var validator = new PersonWithAddressValidator();
        var person = new PersonDto
        {
            Name = "Alice",
            Address = new AddressDto { Street = "123 Main St", City = "Springfield" }
        };
        var result = await validator.ValidateAsync(person);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task SetValidator_WhenNestedInvalid_ReportsPrefixedErrors()
    {
        var validator = new PersonWithAddressValidator();
        var person = new PersonDto
        {
            Name = "Alice",
            Address = new AddressDto { Street = "", City = "" }
        };
        var result = await validator.ValidateAsync(person);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Address.Street"));
        Assert.True(result.HasErrorFor("Address.City"));
    }

    [Fact]
    public async Task SetValidator_WhenAddressIsNull_Passes()
    {
        var validator = new PersonWithAddressValidator();
        var person = new PersonDto { Name = "Alice", Address = null };
        var result = await validator.ValidateAsync(person);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Include_InheritsAllRulesFromOtherValidator()
    {
        var validator = new ExtendedPersonValidator();
        // Name empty, Age 0 (not positive), Email invalid — all from base + extended
        var result = validator.Validate(new PersonDto { Name = "", Age = 0, Email = "user@example.com" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
        Assert.True(result.HasErrorFor("Age"));
    }

    [Fact]
    public async Task Include_AlsoIncludesAsyncRules()
    {
        var validator = new ExtendedPersonValidator();
        var result = await validator.ValidateAsync(new PersonDto { Name = "Alice", Age = 25, Email = "bad-email" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Email"));
    }

    [Fact]
    public void Include_WhenAllValid_Passes()
    {
        var validator = new ExtendedPersonValidator();
        var result = validator.Validate(new PersonDto { Name = "Alice", Age = 25, Email = "alice@example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void OverridePropertyName_UsesCustomKeyInErrors()
    {
        var validator = new OverrideNameValidator();
        var result = validator.Validate(new PersonDto { Name = "" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("FullName"));
        Assert.False(result.HasErrorFor("Name"));
    }

    [Fact]
    public void ValidateAndThrow_WhenValid_DoesNotThrow()
    {
        var validator = new ThrowingValidator();
        var ex = Record.Exception(() => validator.ValidateAndThrow(new PersonDto { Name = "Alice" }));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateAndThrow_WhenInvalid_ThrowsValidationException()
    {
        var validator = new ThrowingValidator();
        var ex = Assert.Throws<ValidationException>(() =>
            validator.ValidateAndThrow(new PersonDto { Name = "" }));
        Assert.NotNull(ex.ValidationResult);
        Assert.False(ex.ValidationResult.IsValid);
    }

    [Fact]
    public async Task ValidateAndThrowAsync_WhenValid_DoesNotThrow()
    {
        var validator = new AsyncThrowingValidator();
        var ex = await Record.ExceptionAsync(() =>
            validator.ValidateAndThrowAsync(new PersonDto { Email = "user@example.com" }));
        Assert.Null(ex);
    }

    [Fact]
    public async Task ValidateAndThrowAsync_WhenInvalid_ThrowsValidationException()
    {
        var validator = new AsyncThrowingValidator();
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            validator.ValidateAndThrowAsync(new PersonDto { Email = "not-an-email" }));
        Assert.NotNull(ex.ValidationResult);
        Assert.False(ex.ValidationResult.IsValid);
    }
}
