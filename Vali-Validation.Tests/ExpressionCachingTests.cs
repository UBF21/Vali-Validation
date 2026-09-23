using System.Reflection;
using Vali_Validation.Core.Validators;
using Xunit;

namespace Vali_Validation.Tests;

public class ExpressionCachingDto
{
    public string? Name { get; set; }
}

public class ExpressionCachingValidator : AbstractValidator<ExpressionCachingDto>
{
    public ExpressionCachingValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class ExpressionCachingTests
{
    [Fact]
    public void ConstructingSameValidatorTypeTwice_ReusesCompiledDelegate_CacheDoesNotGrowUnbounded()
    {
        // Access the private static cache via reflection to prove reuse without exposing
        // internal implementation details on the public API.
        var cacheField = typeof(AbstractValidator<ExpressionCachingDto>)
            .GetField("_compiledExpressionCache", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(cacheField);

        _ = new ExpressionCachingValidator();
        var cache = cacheField!.GetValue(null);
        int countAfterFirst = (int)cache!.GetType().GetProperty("Count")!.GetValue(cache)!;

        _ = new ExpressionCachingValidator();
        int countAfterSecond = (int)cache.GetType().GetProperty("Count")!.GetValue(cache)!;

        Assert.Equal(countAfterFirst, countAfterSecond);
        Assert.True(countAfterFirst > 0);
    }

    [Fact]
    public void CachedDelegate_StillProducesCorrectValidationBehavior()
    {
        var validator1 = new ExpressionCachingValidator();
        var validator2 = new ExpressionCachingValidator();

        var result1 = validator1.Validate(new ExpressionCachingDto { Name = "" });
        var result2 = validator2.Validate(new ExpressionCachingDto { Name = "Ana" });

        Assert.False(result1.IsValid);
        Assert.True(result2.IsValid);
    }
}
