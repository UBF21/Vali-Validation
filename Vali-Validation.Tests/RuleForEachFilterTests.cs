using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class FilterableDto
{
    public List<FilterableItemDto> Items { get; set; } = new();
}

public class FilterableItemDto
{
    public bool IsActive { get; set; }
    public string? Name { get; set; }

    public override string ToString() => Name ?? "";
}

public class ActiveOnlyItemsValidator : AbstractValidator<FilterableDto>
{
    public ActiveOnlyItemsValidator()
    {
        RuleForEach(x => x.Items, item => item.IsActive)
            .NotEmpty();
    }
}

public class RuleForEachFilterTests
{
    [Fact]
    public void RuleForEach_WithFilter_SkipsElementsThatDontMatch()
    {
        var validator = new ActiveOnlyItemsValidator();
        var dto = new FilterableDto
        {
            Items = new List<FilterableItemDto>
            {
                new() { IsActive = false, Name = "" }, // invalid but excluded by the filter
                new() { IsActive = true, Name = "Valid" },
            }
        };

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RuleForEach_WithFilter_ValidatesElementsThatMatch()
    {
        var validator = new ActiveOnlyItemsValidator();
        var dto = new FilterableDto
        {
            Items = new List<FilterableItemDto>
            {
                new() { IsActive = true, Name = "" }, // matches filter and is invalid -> must fail
            }
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RuleForEach_WithFilter_IndexesErrorsByFilteredPosition()
    {
        var validator = new ActiveOnlyItemsValidator();
        var dto = new FilterableDto
        {
            Items = new List<FilterableItemDto>
            {
                new() { IsActive = false, Name = "skip" },  // excluded, original index 0
                new() { IsActive = true, Name = "" },        // matches filter, FILTERED index 0, invalid
            }
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrorFor("Items[0]"), "error should be keyed by filtered position 0, not original position 1");
        Assert.False(result.HasErrorFor("Items[1]"), "no second filtered element exists, so Items[1] must not appear");
    }
}
