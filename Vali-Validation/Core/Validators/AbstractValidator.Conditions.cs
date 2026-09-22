namespace Vali_Validation.Core.Validators;

public abstract partial class AbstractValidator<T> where T : class
{
    private Func<T, bool>? _ambientCondition;

    /// <summary>
    /// Wraps <paramref name="ruleBlock"/> so every <c>RuleFor</c>/<c>RuleForEach</c> call made
    /// inside it automatically carries <paramref name="condition"/> as a guard — equivalent to
    /// calling <c>.When(condition)</c> on every rule defined within the block, without repeating
    /// it per property. Nested <see cref="When"/>/<see cref="Unless"/> blocks compose with AND
    /// (an inner block's condition combines with any already active from an outer block). Rules
    /// outside the block are never affected, even ones defined earlier or later in the same
    /// constructor.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> or <paramref name="ruleBlock"/> is null.</exception>
    protected void When(Func<T, bool> condition, Action ruleBlock)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        if (ruleBlock == null) throw new ArgumentNullException(nameof(ruleBlock));
        RunWithAmbientCondition(condition, ruleBlock);
    }

    /// <summary>
    /// Wraps <paramref name="ruleBlock"/> so every rule defined inside it automatically carries the
    /// negation of <paramref name="condition"/> as a guard. See <see cref="When"/> for full semantics.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> or <paramref name="ruleBlock"/> is null.</exception>
    protected void Unless(Func<T, bool> condition, Action ruleBlock)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        if (ruleBlock == null) throw new ArgumentNullException(nameof(ruleBlock));
        RunWithAmbientCondition(instance => !condition(instance), ruleBlock);
    }

    private void RunWithAmbientCondition(Func<T, bool> condition, Action ruleBlock)
    {
        var previous = _ambientCondition;
        _ambientCondition = previous == null ? condition : CombineAmbient(previous, condition);
        try
        {
            ruleBlock();
        }
        finally
        {
            _ambientCondition = previous;
        }
    }

    private static Func<T, bool> CombineAmbient(Func<T, bool> outer, Func<T, bool> inner)
        => instance => outer(instance) && inner(instance);
}
