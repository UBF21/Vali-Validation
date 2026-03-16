using Vali_Validation.Core.Validators;
using Vali_Validation.Tests.Models;
using Xunit;

namespace Vali_Validation.Tests;

// Validators defined here for sync rule tests
public class NotEmptyValidator : AbstractValidator<PersonDto>
{
    public NotEmptyValidator() { RuleFor(x => x.Name).NotEmpty(); }
}

public class NotNullValidator : AbstractValidator<PersonDto>
{
    public NotNullValidator() { RuleFor(x => x.Name).NotNull(); }
}

public class EmailValidator : AbstractValidator<PersonDto>
{
    public EmailValidator() { RuleFor(x => x.Email).Email(); }
}

public class GreaterThanValidator : AbstractValidator<PersonDto>
{
    public GreaterThanValidator() { RuleFor(x => x.Age).GreaterThan(0); }
}

public class LessThanValidator : AbstractValidator<PersonDto>
{
    public LessThanValidator() { RuleFor(x => x.Age).LessThan(120); }
}

public class BetweenValidator : AbstractValidator<PersonDto>
{
    public BetweenValidator() { RuleFor(x => x.Age).Between(18, 65); }
}

public class PositiveValidator : AbstractValidator<PersonDto>
{
    public PositiveValidator() { RuleFor(x => x.Age).Positive(); }
}

public class NegativeValidator : AbstractValidator<PersonDto>
{
    public NegativeValidator() { RuleFor(x => x.Age).Negative(); }
}

public class NotZeroValidator : AbstractValidator<PersonDto>
{
    public NotZeroValidator() { RuleFor(x => x.Age).NotZero(); }
}

public class NotEqualValidator : AbstractValidator<PersonDto>
{
    public NotEqualValidator() { RuleFor(x => x.Name).NotEqual("Forbidden"); }
}

public class LengthBetweenValidator : AbstractValidator<PersonDto>
{
    public LengthBetweenValidator() { RuleFor(x => x.Name).LengthBetween(2, 50); }
}

public class ExclusiveBetweenValidator : AbstractValidator<PersonDto>
{
    public ExclusiveBetweenValidator() { RuleFor(x => x.Age).ExclusiveBetween(18, 65); }
}

public enum StatusEnum { Active, Inactive, Pending }

public class IsEnumValidator : AbstractValidator<PersonDto>
{
    public IsEnumValidator() { RuleFor(x => x.Status).IsEnum<StatusEnum>(); }
}

public class GuidValidator : AbstractValidator<PersonDto>
{
    public GuidValidator() { RuleFor(x => x.GuidValue).Guid(); }
}

public class InValidator : AbstractValidator<PersonDto>
{
    public InValidator()
    {
        RuleFor(x => x.Name).In(new List<string> { "Alice", "Bob", "Charlie" });
    }
}

public class MatchesValidator : AbstractValidator<PersonDto>
{
    public MatchesValidator() { RuleFor(x => x.Name).Matches(@"^[A-Z][a-z]+$"); }
}

public class StartsWithValidator : AbstractValidator<PersonDto>
{
    public StartsWithValidator() { RuleFor(x => x.Name).StartsWith("Mr."); }
}

public class EndsWithValidator : AbstractValidator<PersonDto>
{
    public EndsWithValidator() { RuleFor(x => x.Name).EndsWith(".com"); }
}

public class MustContainValidator : AbstractValidator<PersonDto>
{
    public MustContainValidator() { RuleFor(x => x.Email).MustContain("@"); }
}

public class RuleBuilderSyncTests
{
    [Fact]
    public void NotEmpty_WhenValueIsNotEmpty_Passes()
    {
        var result = new NotEmptyValidator().Validate(new PersonDto { Name = "Alice" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotEmpty_WhenValueIsEmpty_Fails()
    {
        var result = new NotEmptyValidator().Validate(new PersonDto { Name = "" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public void NotEmpty_WhenValueIsWhitespace_Fails()
    {
        var result = new NotEmptyValidator().Validate(new PersonDto { Name = "   " });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NotNull_WhenValueIsNotNull_Passes()
    {
        var result = new NotNullValidator().Validate(new PersonDto { Name = "Alice" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotNull_WhenValueIsNull_Fails()
    {
        var result = new NotNullValidator().Validate(new PersonDto { Name = null });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public void Email_WhenValidEmail_Passes()
    {
        var result = new EmailValidator().Validate(new PersonDto { Email = "user@example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Email_WhenInvalidEmail_Fails()
    {
        var result = new EmailValidator().Validate(new PersonDto { Email = "not-an-email" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Email"));
    }

    [Fact]
    public void GreaterThan_WhenAboveThreshold_Passes()
    {
        var result = new GreaterThanValidator().Validate(new PersonDto { Age = 25 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void GreaterThan_WhenAtThreshold_Fails()
    {
        var result = new GreaterThanValidator().Validate(new PersonDto { Age = 0 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LessThan_WhenBelowThreshold_Passes()
    {
        var result = new LessThanValidator().Validate(new PersonDto { Age = 30 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LessThan_WhenAtThreshold_Fails()
    {
        var result = new LessThanValidator().Validate(new PersonDto { Age = 120 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Between_WhenWithinRange_Passes()
    {
        var result = new BetweenValidator().Validate(new PersonDto { Age = 30 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Between_WhenAtBoundary_Passes()
    {
        var result = new BetweenValidator().Validate(new PersonDto { Age = 18 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Between_WhenOutsideRange_Fails()
    {
        var result = new BetweenValidator().Validate(new PersonDto { Age = 10 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Positive_WhenPositive_Passes()
    {
        var result = new PositiveValidator().Validate(new PersonDto { Age = 5 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Positive_WhenZero_Fails()
    {
        var result = new PositiveValidator().Validate(new PersonDto { Age = 0 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Negative_WhenNegative_Passes()
    {
        var result = new NegativeValidator().Validate(new PersonDto { Age = -5 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Negative_WhenZero_Fails()
    {
        var result = new NegativeValidator().Validate(new PersonDto { Age = 0 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NotZero_WhenNotZero_Passes()
    {
        var result = new NotZeroValidator().Validate(new PersonDto { Age = 1 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotZero_WhenZero_Fails()
    {
        var result = new NotZeroValidator().Validate(new PersonDto { Age = 0 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void NotEqual_WhenDifferent_Passes()
    {
        var result = new NotEqualValidator().Validate(new PersonDto { Name = "Alice" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotEqual_WhenEqual_Fails()
    {
        var result = new NotEqualValidator().Validate(new PersonDto { Name = "Forbidden" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public void LengthBetween_WhenWithinRange_Passes()
    {
        var result = new LengthBetweenValidator().Validate(new PersonDto { Name = "Alice" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LengthBetween_WhenTooShort_Fails()
    {
        var result = new LengthBetweenValidator().Validate(new PersonDto { Name = "A" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LengthBetween_WhenTooLong_Fails()
    {
        var result = new LengthBetweenValidator().Validate(new PersonDto { Name = new string('X', 51) });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ExclusiveBetween_WhenStrictlyInside_Passes()
    {
        var result = new ExclusiveBetweenValidator().Validate(new PersonDto { Age = 30 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ExclusiveBetween_WhenAtMin_Fails()
    {
        var result = new ExclusiveBetweenValidator().Validate(new PersonDto { Age = 18 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ExclusiveBetween_WhenAtMax_Fails()
    {
        var result = new ExclusiveBetweenValidator().Validate(new PersonDto { Age = 65 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void IsEnum_WhenValidEnumName_Passes()
    {
        var result = new IsEnumValidator().Validate(new PersonDto { Status = "Active" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void IsEnum_WhenInvalidValue_Fails()
    {
        var result = new IsEnumValidator().Validate(new PersonDto { Status = "Unknown" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Status"));
    }

    [Fact]
    public void Guid_WhenValidGuid_Passes()
    {
        var result = new GuidValidator().Validate(new PersonDto { GuidValue = System.Guid.NewGuid().ToString() });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Guid_WhenInvalidGuid_Fails()
    {
        var result = new GuidValidator().Validate(new PersonDto { GuidValue = "not-a-guid" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("GuidValue"));
    }

    [Fact]
    public void In_WhenValueInList_Passes()
    {
        var result = new InValidator().Validate(new PersonDto { Name = "Alice" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void In_WhenValueNotInList_Fails()
    {
        var result = new InValidator().Validate(new PersonDto { Name = "Dave" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Matches_WhenPatternMatches_Passes()
    {
        var result = new MatchesValidator().Validate(new PersonDto { Name = "Alice" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Matches_WhenPatternDoesNotMatch_Fails()
    {
        var result = new MatchesValidator().Validate(new PersonDto { Name = "alice" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void StartsWith_WhenMatchingPrefix_Passes()
    {
        var result = new StartsWithValidator().Validate(new PersonDto { Name = "Mr. Smith" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void StartsWith_WhenNoMatchingPrefix_Fails()
    {
        var result = new StartsWithValidator().Validate(new PersonDto { Name = "John" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void EndsWith_WhenMatchingSuffix_Passes()
    {
        var result = new EndsWithValidator().Validate(new PersonDto { Name = "example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EndsWith_WhenNoMatchingSuffix_Fails()
    {
        var result = new EndsWithValidator().Validate(new PersonDto { Name = "example.org" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MustContain_WhenContainsSubstring_Passes()
    {
        var result = new MustContainValidator().Validate(new PersonDto { Email = "user@example.com" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MustContain_WhenNotContainsSubstring_Fails()
    {
        var result = new MustContainValidator().Validate(new PersonDto { Email = "userexample.com" });
        Assert.False(result.IsValid);
    }
}
