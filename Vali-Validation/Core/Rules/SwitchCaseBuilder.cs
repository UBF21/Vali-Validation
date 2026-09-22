using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

internal sealed class SwitchCaseBuilder<T, TKey> : SwitchRegistry<T, TKey>, ICaseBuilder<T, TKey> where T : class
{
    public SwitchCaseBuilder(AbstractValidator<T> validator, Func<T, TKey> keyFunc)
        : base(validator, keyFunc)
    {
    }

    public ICaseBuilder<T, TKey> Case(TKey value, Action<AbstractValidator<T>> configure)
    {
        var sub = new InlineSwitchValidator<T>();
        configure(sub);

        AddCase(value, sub);
        return this;
    }

    public ICaseBuilder<T, TKey> Default(Action<AbstractValidator<T>> configure)
    {
        var sub = new InlineSwitchValidator<T>();
        configure(sub);

        SetDefault(sub);
        return this;
    }
}
