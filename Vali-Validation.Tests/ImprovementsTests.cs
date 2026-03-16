using System.Threading;
using Vali_Validation.Core.Results;
using Vali_Validation.Core.Rules;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

// ---------------------------------------------------------------------------
// Models
// ---------------------------------------------------------------------------

public class PasswordDto
{
    public string? Password { get; set; }
}

public class CollectionDto
{
    public List<string>? Items { get; set; }
    public List<int>? Numbers { get; set; }
    public string? Text { get; set; }
    public string? Category { get; set; }
}

public class CodedDto
{
    public string? Name { get; set; }
    public int Age { get; set; }
}

// ---------------------------------------------------------------------------
// ITEM 1: CancellationToken tests
// ---------------------------------------------------------------------------

public class CancelTokenValidator : AbstractValidator<PasswordDto>
{
    public CancelTokenValidator()
    {
        RuleFor(x => x.Password).MustAsync(async (value, ct) =>
        {
            await Task.Delay(1, ct);
            return !string.IsNullOrEmpty(value);
        });
    }
}

public class CancellationTokenTests
{
    [Fact]
    public async Task ValidateAsync_WithCancellationToken_Passes()
    {
        var validator = new CancelTokenValidator();
        using var cts = new CancellationTokenSource();
        var result = await validator.ValidateAsync(new PasswordDto { Password = "hello" }, cts.Token);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_Fails()
    {
        var validator = new CancelTokenValidator();
        using var cts = new CancellationTokenSource();
        var result = await validator.ValidateAsync(new PasswordDto { Password = null }, cts.Token);
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Password"));
    }

    [Fact]
    public async Task ValidateAndThrowAsync_WithCancellationToken_Works()
    {
        var validator = new CancelTokenValidator();
        using var cts = new CancellationTokenSource();
        // Should not throw
        await validator.ValidateAndThrowAsync(new PasswordDto { Password = "hello" }, cts.Token);
    }

    [Fact]
    public async Task ValidateParallelAsync_WithCancellationToken_Works()
    {
        var validator = new CancelTokenValidator();
        using var cts = new CancellationTokenSource();
        var result = await validator.ValidateParallelAsync(new PasswordDto { Password = "hello" }, cts.Token);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task MustAsyncWithToken_WhenPredicatePasses_ReturnsValid()
    {
        var called = false;
        var validator = new InlinePasswordValidator((value, ct) =>
        {
            called = true;
            return Task.FromResult(!string.IsNullOrEmpty(value));
        });
        var result = await validator.ValidateAsync(new PasswordDto { Password = "secret" });
        Assert.True(result.IsValid);
        Assert.True(called);
    }

    [Fact]
    public async Task MustAsyncWithToken_WhenPredicateFails_ReturnsInvalid()
    {
        var validator = new InlinePasswordValidator((value, ct) => Task.FromResult(false));
        var result = await validator.ValidateAsync(new PasswordDto { Password = "secret" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Password"));
    }
}

public class InlinePasswordValidator : AbstractValidator<PasswordDto>
{
    public InlinePasswordValidator(Func<string?, CancellationToken, Task<bool>> predicate)
    {
        RuleFor(x => x.Password).MustAsync((value, ct) => predicate(value, ct));
    }
}

// ---------------------------------------------------------------------------
// ITEM 2: Password strength rules
// ---------------------------------------------------------------------------

public class PasswordStrengthValidator : AbstractValidator<PasswordDto>
{
    public PasswordStrengthValidator()
    {
        RuleFor(x => x.Password)
            .HasUppercase()
            .HasLowercase()
            .HasDigit()
            .HasSpecialChar();
    }
}

public class PasswordStrengthTests
{
    [Fact]
    public void HasUppercase_WhenPresent_Passes()
    {
        var v = new HasUppercaseValidator();
        Assert.True(v.Validate(new PasswordDto { Password = "Hello" }).IsValid);
    }

    [Fact]
    public void HasUppercase_WhenAbsent_Fails()
    {
        var v = new HasUppercaseValidator();
        var result = v.Validate(new PasswordDto { Password = "hello" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Password"));
    }

    [Fact]
    public void HasLowercase_WhenPresent_Passes()
    {
        var v = new HasLowercaseValidator();
        Assert.True(v.Validate(new PasswordDto { Password = "Hello" }).IsValid);
    }

    [Fact]
    public void HasLowercase_WhenAbsent_Fails()
    {
        var v = new HasLowercaseValidator();
        var result = v.Validate(new PasswordDto { Password = "HELLO" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Password"));
    }

    [Fact]
    public void HasDigit_WhenPresent_Passes()
    {
        var v = new HasDigitValidator();
        Assert.True(v.Validate(new PasswordDto { Password = "abc1" }).IsValid);
    }

    [Fact]
    public void HasDigit_WhenAbsent_Fails()
    {
        var v = new HasDigitValidator();
        var result = v.Validate(new PasswordDto { Password = "abcdef" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Password"));
    }

    [Fact]
    public void HasSpecialChar_WhenPresent_Passes()
    {
        var v = new HasSpecialCharValidator();
        Assert.True(v.Validate(new PasswordDto { Password = "abc!" }).IsValid);
    }

    [Fact]
    public void HasSpecialChar_WhenAbsent_Fails()
    {
        var v = new HasSpecialCharValidator();
        var result = v.Validate(new PasswordDto { Password = "abcdef1" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Password"));
    }

    [Fact]
    public void StrongPassword_PassesAll()
    {
        var v = new PasswordStrengthValidator();
        var result = v.Validate(new PasswordDto { Password = "Abc1!" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void WeakPassword_FailsMultiple()
    {
        var v = new PasswordStrengthValidator();
        var result = v.Validate(new PasswordDto { Password = "abc" });
        Assert.False(result.IsValid);
        // Missing uppercase, digit, and special char
        Assert.True(result.ErrorsFor("Password").Count >= 3);
    }
}

public class HasUppercaseValidator : AbstractValidator<PasswordDto>
{
    public HasUppercaseValidator() { RuleFor(x => x.Password).HasUppercase(); }
}

public class HasLowercaseValidator : AbstractValidator<PasswordDto>
{
    public HasLowercaseValidator() { RuleFor(x => x.Password).HasLowercase(); }
}

public class HasDigitValidator : AbstractValidator<PasswordDto>
{
    public HasDigitValidator() { RuleFor(x => x.Password).HasDigit(); }
}

public class HasSpecialCharValidator : AbstractValidator<PasswordDto>
{
    public HasSpecialCharValidator() { RuleFor(x => x.Password).HasSpecialChar(); }
}

// ---------------------------------------------------------------------------
// ITEM 3: Collection and string rules
// ---------------------------------------------------------------------------

public class CollectionRulesTests
{
    [Fact]
    public void NotIn_WhenValueNotInList_Passes()
    {
        var v = new NotInCategoryValidator(new List<string> { "banned", "restricted" });
        var result = v.Validate(new CollectionDto { Category = "allowed" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotIn_WhenValueInList_Fails()
    {
        var v = new NotInCategoryValidator(new List<string> { "banned", "restricted" });
        var result = v.Validate(new CollectionDto { Category = "banned" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Category"));
    }

    [Fact]
    public void MinCount_WhenEnoughItems_Passes()
    {
        var v = new MinCountValidator(2);
        var result = v.Validate(new CollectionDto { Items = new List<string> { "a", "b", "c" } });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MinCount_WhenTooFew_Fails()
    {
        var v = new MinCountValidator(3);
        var result = v.Validate(new CollectionDto { Items = new List<string> { "a" } });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Items"));
    }

    [Fact]
    public void MaxCount_WhenFewEnough_Passes()
    {
        var v = new MaxCountValidator(5);
        var result = v.Validate(new CollectionDto { Items = new List<string> { "a", "b" } });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MaxCount_WhenTooMany_Fails()
    {
        var v = new MaxCountValidator(2);
        var result = v.Validate(new CollectionDto { Items = new List<string> { "a", "b", "c" } });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Items"));
    }

    [Fact]
    public void Unique_WhenNoDuplicates_Passes()
    {
        var v = new UniqueItemsValidator();
        var result = v.Validate(new CollectionDto { Items = new List<string> { "a", "b", "c" } });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Unique_WhenDuplicates_Fails()
    {
        var v = new UniqueItemsValidator();
        var result = v.Validate(new CollectionDto { Items = new List<string> { "a", "b", "a" } });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Items"));
    }

    [Fact]
    public void AllSatisfy_WhenAllMatch_Passes()
    {
        var v = new AllSatisfyValidator();
        var result = v.Validate(new CollectionDto { Numbers = new List<int> { 2, 4, 6 } });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AllSatisfy_WhenOneFails_Fails()
    {
        var v = new AllSatisfyValidator();
        var result = v.Validate(new CollectionDto { Numbers = new List<int> { 2, 3, 6 } });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Numbers"));
    }

    [Fact]
    public void AnySatisfy_WhenOneMatches_Passes()
    {
        var v = new AnySatisfyValidator();
        var result = v.Validate(new CollectionDto { Numbers = new List<int> { 1, 3, 4 } });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AnySatisfy_WhenNoneMatch_Fails()
    {
        var v = new AnySatisfyValidator();
        var result = v.Validate(new CollectionDto { Numbers = new List<int> { 1, 3, 5 } });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Numbers"));
    }

    [Fact]
    public void Lowercase_WhenAllLower_Passes()
    {
        var v = new LowercaseTextValidator();
        Assert.True(v.Validate(new CollectionDto { Text = "hello world" }).IsValid);
    }

    [Fact]
    public void Lowercase_WhenHasUpper_Fails()
    {
        var v = new LowercaseTextValidator();
        var result = v.Validate(new CollectionDto { Text = "Hello" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Text"));
    }

    [Fact]
    public void Uppercase_WhenAllUpper_Passes()
    {
        var v = new UppercaseTextValidator();
        Assert.True(v.Validate(new CollectionDto { Text = "HELLO" }).IsValid);
    }

    [Fact]
    public void Uppercase_WhenHasLower_Fails()
    {
        var v = new UppercaseTextValidator();
        var result = v.Validate(new CollectionDto { Text = "Hello" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Text"));
    }

    [Fact]
    public void MinWords_WhenEnoughWords_Passes()
    {
        var v = new MinWordsValidator(3);
        Assert.True(v.Validate(new CollectionDto { Text = "one two three four" }).IsValid);
    }

    [Fact]
    public void MinWords_WhenTooFew_Fails()
    {
        var v = new MinWordsValidator(3);
        var result = v.Validate(new CollectionDto { Text = "one two" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Text"));
    }

    [Fact]
    public void MaxWords_WhenFewEnough_Passes()
    {
        var v = new MaxWordsValidator(3);
        Assert.True(v.Validate(new CollectionDto { Text = "one two" }).IsValid);
    }

    [Fact]
    public void MaxWords_WhenTooMany_Fails()
    {
        var v = new MaxWordsValidator(2);
        var result = v.Validate(new CollectionDto { Text = "one two three" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Text"));
    }

    [Fact]
    public void MinWords_WhenNull_Fails()
    {
        var v = new MinWordsValidator(1);
        var result = v.Validate(new CollectionDto { Text = null });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MaxWords_WhenNull_Passes()
    {
        var v = new MaxWordsValidator(5);
        Assert.True(v.Validate(new CollectionDto { Text = null }).IsValid);
    }
}

public class NotInCategoryValidator : AbstractValidator<CollectionDto>
{
    public NotInCategoryValidator(List<string> banned)
    {
        RuleFor(x => x.Category).NotIn(banned);
    }
}

public class MinCountValidator : AbstractValidator<CollectionDto>
{
    public MinCountValidator(int min) { RuleFor(x => x.Items).MinCount(min); }
}

public class MaxCountValidator : AbstractValidator<CollectionDto>
{
    public MaxCountValidator(int max) { RuleFor(x => x.Items).MaxCount(max); }
}

public class UniqueItemsValidator : AbstractValidator<CollectionDto>
{
    public UniqueItemsValidator() { RuleFor(x => x.Items).Unique(); }
}

public class AllSatisfyValidator : AbstractValidator<CollectionDto>
{
    public AllSatisfyValidator() { RuleFor(x => x.Numbers).AllSatisfy(o => o is int n && n % 2 == 0); }
}

public class AnySatisfyValidator : AbstractValidator<CollectionDto>
{
    public AnySatisfyValidator() { RuleFor(x => x.Numbers).AnySatisfy(o => o is int n && n % 2 == 0); }
}

public class LowercaseTextValidator : AbstractValidator<CollectionDto>
{
    public LowercaseTextValidator() { RuleFor(x => x.Text).Lowercase(); }
}

public class UppercaseTextValidator : AbstractValidator<CollectionDto>
{
    public UppercaseTextValidator() { RuleFor(x => x.Text).Uppercase(); }
}

public class MinWordsValidator : AbstractValidator<CollectionDto>
{
    public MinWordsValidator(int min) { RuleFor(x => x.Text).MinWords(min); }
}

public class MaxWordsValidator : AbstractValidator<CollectionDto>
{
    public MaxWordsValidator(int max) { RuleFor(x => x.Text).MaxWords(max); }
}

// ---------------------------------------------------------------------------
// ITEM 4: Message templates
// ---------------------------------------------------------------------------

public class MessageTemplateTests
{
    [Fact]
    public void WithMessage_ReplacesPropertyName()
    {
        var v = new TemplatePropertyNameValidator();
        var result = v.Validate(new CodedDto { Name = "" });
        Assert.False(result.IsValid);
        var errors = result.ErrorsFor("Name");
        Assert.Contains("Name", errors[0]);
    }

    [Fact]
    public void WithMessage_ReplacesPropertyValue()
    {
        var v = new TemplatePropertyValueValidator();
        var result = v.Validate(new CodedDto { Age = -5 });
        Assert.False(result.IsValid);
        var errors = result.ErrorsFor("Age");
        Assert.Contains("-5", errors[0]);
    }

    [Fact]
    public void WithMessage_BothPlaceholders_Replaced()
    {
        var v = new TemplateBothPlaceholdersValidator();
        var result = v.Validate(new CodedDto { Age = 3 });
        Assert.False(result.IsValid);
        var errors = result.ErrorsFor("Age");
        Assert.Contains("Age", errors[0]);
        Assert.Contains("3", errors[0]);
    }
}

public class TemplatePropertyNameValidator : AbstractValidator<CodedDto>
{
    public TemplatePropertyNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("The field '{PropertyName}' is required.");
    }
}

public class TemplatePropertyValueValidator : AbstractValidator<CodedDto>
{
    public TemplatePropertyValueValidator()
    {
        RuleFor(x => x.Age)
            .GreaterThan(0)
            .WithMessage("Value '{PropertyValue}' must be positive.");
    }
}

public class TemplateBothPlaceholdersValidator : AbstractValidator<CodedDto>
{
    public TemplateBothPlaceholdersValidator()
    {
        RuleFor(x => x.Age)
            .GreaterThan(5)
            .WithMessage("Field '{PropertyName}' has invalid value '{PropertyValue}'.");
    }
}

// ---------------------------------------------------------------------------
// ITEM 5: CascadeMode at validator level
// ---------------------------------------------------------------------------

public class CascadeModeTests
{
    [Fact]
    public void CascadeMode_StopOnFirstFailure_StopsAfterFirstProperty()
    {
        var v = new GlobalCascadeValidator();
        var result = v.Validate(new CodedDto { Name = null, Age = -1 });
        Assert.False(result.IsValid);
        // Should stop after first property fails — only one property has errors
        Assert.Single(result.Errors);
    }

    [Fact]
    public void CascadeMode_Continue_ReportsAllErrors()
    {
        var v = new ContinueCascadeValidator();
        var result = v.Validate(new CodedDto { Name = null, Age = -1 });
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void CascadeMode_StopOnFirstFailure_WhenAllValid_IsValid()
    {
        var v = new GlobalCascadeValidator();
        var result = v.Validate(new CodedDto { Name = "Alice", Age = 25 });
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CascadeMode_StopOnFirstFailure_ValidateAsync_StopsAfterFirst()
    {
        var v = new GlobalCascadeValidator();
        var result = await v.ValidateAsync(new CodedDto { Name = null, Age = -1 });
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }
}

public class GlobalCascadeValidator : AbstractValidator<CodedDto>
{
    protected override CascadeMode GlobalCascadeMode => CascadeMode.StopOnFirstFailure;

    public GlobalCascadeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Age).Positive();
    }
}

public class ContinueCascadeValidator : AbstractValidator<CodedDto>
{
    protected override CascadeMode GlobalCascadeMode => CascadeMode.Continue;

    public ContinueCascadeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Age).Positive();
    }
}

// ---------------------------------------------------------------------------
// ITEM 6: WithErrorCode
// ---------------------------------------------------------------------------

public class ErrorCodeTests
{
    [Fact]
    public void WithErrorCode_WhenRuleFails_ErrorCodeIsSet()
    {
        var v = new ErrorCodeValidator();
        var result = v.Validate(new CodedDto { Name = "" });
        Assert.False(result.IsValid);
        Assert.True(result.ErrorCodes.ContainsKey("Name"));
        Assert.Contains("NAME_REQUIRED", result.ErrorCodes["Name"]);
    }

    [Fact]
    public void WithErrorCode_WhenRulePasses_ErrorCodesEmpty()
    {
        var v = new ErrorCodeValidator();
        var result = v.Validate(new CodedDto { Name = "Alice" });
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorCodes);
    }

    [Fact]
    public void WithErrorCode_MultipleRules_CorrectCodesSet()
    {
        var v = new MultipleErrorCodeValidator();
        var result = v.Validate(new CodedDto { Name = "", Age = -1 });
        Assert.False(result.IsValid);
        Assert.True(result.ErrorCodes.ContainsKey("Name"));
        Assert.True(result.ErrorCodes.ContainsKey("Age"));
        Assert.Contains("NAME_REQUIRED", result.ErrorCodes["Name"]);
        Assert.Contains("AGE_INVALID", result.ErrorCodes["Age"]);
    }

    [Fact]
    public void ValidationResult_Merge_AlsoMergesErrorCodes()
    {
        var r1 = new ValidationResult();
        r1.AddError("Name", "Required", "NAME_REQUIRED");

        var r2 = new ValidationResult();
        r2.AddError("Age", "Invalid", "AGE_INVALID");

        r1.Merge(r2);

        Assert.True(r1.ErrorCodes.ContainsKey("Name"));
        Assert.True(r1.ErrorCodes.ContainsKey("Age"));
        Assert.Contains("NAME_REQUIRED", r1.ErrorCodes["Name"]);
        Assert.Contains("AGE_INVALID", r1.ErrorCodes["Age"]);
    }
}

public class ErrorCodeValidator : AbstractValidator<CodedDto>
{
    public ErrorCodeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("NAME_REQUIRED");
    }
}

public class MultipleErrorCodeValidator : AbstractValidator<CodedDto>
{
    public MultipleErrorCodeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("NAME_REQUIRED");
        RuleFor(x => x.Age).Positive().WithErrorCode("AGE_INVALID");
    }
}

// ---------------------------------------------------------------------------
// ITEM 7: Custom(action) rule
// ---------------------------------------------------------------------------

public class CustomRuleTests
{
    [Fact]
    public void Custom_CanAddFailureManually()
    {
        var v = new CustomRuleValidator();
        var result = v.Validate(new CodedDto { Name = "bad" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Name"));
    }

    [Fact]
    public void Custom_WhenNoFailureAdded_Passes()
    {
        var v = new CustomRuleValidator();
        var result = v.Validate(new CodedDto { Name = "good" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Custom_CanAddFailureWithCustomProperty()
    {
        var v = new CustomRuleWithPropertyValidator();
        var result = v.Validate(new CodedDto { Name = "fail" });
        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("CustomProp"));
    }

    [Fact]
    public void Custom_CanAddFailureWithErrorCode()
    {
        var v = new CustomRuleWithCodeValidator();
        var result = v.Validate(new CodedDto { Name = "fail" });
        Assert.False(result.IsValid);
        Assert.True(result.ErrorCodes.ContainsKey("Name"));
        Assert.Contains("CUSTOM_CODE", result.ErrorCodes["Name"]);
    }

    [Fact]
    public void Custom_HasAccessToFullInstance()
    {
        var v = new CustomRuleInstanceAccessValidator();
        var result = v.Validate(new CodedDto { Name = "Alice", Age = 5 });
        Assert.False(result.IsValid); // Age < 18 => error
    }
}

public class CustomRuleValidator : AbstractValidator<CodedDto>
{
    public CustomRuleValidator()
    {
        RuleFor(x => x.Name).Custom((value, ctx) =>
        {
            if (value == "bad")
                ctx.AddFailure("Name cannot be 'bad'.");
        });
    }
}

public class CustomRuleWithPropertyValidator : AbstractValidator<CodedDto>
{
    public CustomRuleWithPropertyValidator()
    {
        RuleFor(x => x.Name).Custom((value, ctx) =>
        {
            if (value == "fail")
                ctx.AddFailure("CustomProp", "Custom property error.");
        });
    }
}

public class CustomRuleWithCodeValidator : AbstractValidator<CodedDto>
{
    public CustomRuleWithCodeValidator()
    {
        RuleFor(x => x.Name).Custom((value, ctx) =>
        {
            if (value == "fail")
                ctx.AddFailure("Name", "Custom error.", "CUSTOM_CODE");
        });
    }
}

public class CustomRuleInstanceAccessValidator : AbstractValidator<CodedDto>
{
    public CustomRuleInstanceAccessValidator()
    {
        RuleFor(x => x.Name).Custom((value, ctx) =>
        {
            if (ctx.Instance.Age < 18)
                ctx.AddFailure("Age must be at least 18.");
        });
    }
}

// ---------------------------------------------------------------------------
// ITEM 8: Transform(selector)
// ---------------------------------------------------------------------------

public class TransformTests
{
    [Fact]
    public void Transform_CanValidateTransformedValue()
    {
        var v = new TransformLengthValidator();
        // Name "Hi" has length 2, which is >= 2, so valid
        var result = v.Validate(new CodedDto { Name = "Hi" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Transform_WhenTransformedValueFails_ReturnsInvalid()
    {
        var v = new TransformLengthValidator();
        // Name "H" has length 1, which is < 2, so invalid
        var result = v.Validate(new CodedDto { Name = "H" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Transform_CanChainRules()
    {
        var v = new TransformUppercaseValidator();
        // Name "hello" uppercased is "HELLO", which equals "HELLO"
        var result = v.Validate(new CodedDto { Name = "hello" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Transform_WhenChainedRuleFails()
    {
        var v = new TransformUppercaseValidator();
        // Name "world" uppercased is "WORLD", which does not equal "HELLO"
        var result = v.Validate(new CodedDto { Name = "world" });
        Assert.False(result.IsValid);
    }
}

public class TransformLengthValidator : AbstractValidator<CodedDto>
{
    public TransformLengthValidator()
    {
        ((RuleBuilder<CodedDto, string?>)RuleFor(x => x.Name))
            .Transform(s => s?.Length ?? 0)
            .GreaterThanOrEqualTo(2);
    }
}

public class TransformUppercaseValidator : AbstractValidator<CodedDto>
{
    public TransformUppercaseValidator()
    {
        ((RuleBuilder<CodedDto, string?>)RuleFor(x => x.Name))
            .Transform(s => s?.ToUpper() ?? "")
            .EqualTo("HELLO");
    }
}
