using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

internal sealed class SwitchOnBuilder<T, TProperty, TKey> : SwitchRegistry<T, TKey>, ISwitchOnBuilder<T, TProperty, TKey>
    where T : class
{
    private readonly Func<T, TProperty> _propertyFunc;
    private readonly string _propertyName;

    public SwitchOnBuilder(
        AbstractValidator<T> parentValidator,
        Func<T, TProperty> propertyFunc,
        string propertyName,
        Func<T, TKey> keyFunc,
        Func<T, bool>? ambientCondition = null)
        : base(parentValidator, keyFunc, ambientCondition)
    {
        _propertyFunc = propertyFunc;
        _propertyName = propertyName;
    }

    public ISwitchOnBuilder<T, TProperty, TKey> Case(TKey value, Action<IRuleBuilder<T, TProperty>> configure)
    {
        var tempValidator = new InlineSwitchValidator<T>();
        var tempBuilder = new RuleBuilder<T, TProperty>(tempValidator, _propertyFunc, _propertyName);
        configure(tempBuilder);

        AddCase(value, tempValidator);
        return this;
    }

    public ISwitchOnBuilder<T, TProperty, TKey> Default(Action<IRuleBuilder<T, TProperty>> configure)
    {
        var tempValidator = new InlineSwitchValidator<T>();
        var tempBuilder = new RuleBuilder<T, TProperty>(tempValidator, _propertyFunc, _propertyName);
        configure(tempBuilder);

        SetDefault(tempValidator);
        return this;
    }
}
