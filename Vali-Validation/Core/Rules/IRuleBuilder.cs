using System.Linq.Expressions;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.Core.Rules;

/// <summary>
/// Provides a fluent interface for defining validation rules on a specific property of an object of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the object being validated.</typeparam>
/// <typeparam name="TProperty">The type of the property to validate.</typeparam>
public interface IRuleBuilder<T, TProperty> where T : class
{
    /// <summary>
    /// Sets a custom error message to be used if the validation rule fails.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    public RuleBuilder<T, TProperty> WithMessage(string? message);

    /// <summary>
    /// Validates that the value is not null, empty, or whitespace (for strings), and not an empty collection.
    /// </summary>
    public RuleBuilder<T, TProperty> NotEmpty();

    /// <summary>
    /// Validates the value using a custom boolean predicate.
    /// </summary>
    /// <param name="predicate">A function that defines the custom validation logic.</param>
    public RuleBuilder<T, TProperty> Must(Func<TProperty, bool>? predicate);

    /// <summary>
    /// Validates that the value contains the specified substring.
    /// Only applicable to values that can be converted to a string.
    /// </summary>
    /// <param name="substring">The substring that must be present in the value.</param>
    /// <param name="comparison">
    /// The type of string comparison to use when searching for the substring.
    /// Defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </param>
    /// <returns>
    /// The current <see cref="RuleBuilder{T, TProperty}"/> instance to allow method chaining.
    /// </returns>
    /// <example>
    /// RuleFor(x => x.Description).MustContain("error", StringComparison.OrdinalIgnoreCase);
    /// </example>
    /// <remarks>
    /// If the value is null or not a string (or cannot be converted to string), the validation fails.
    /// </remarks>
    public RuleBuilder<T, TProperty> MustContain(string substring,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Validates that the string representation of the value has a minimum length.
    /// </summary>
    /// <param name="length">The minimum allowed length.</param>
    public RuleBuilder<T, TProperty> MinimumLength(int length);

    /// <summary>
    /// Validates that the string representation of the value has a maximum length.
    /// </summary>
    /// <param name="length">The maximum allowed length.</param>
    public RuleBuilder<T, TProperty> MaximumLength(int length);

    /// <summary>
    /// Validates that the value matches a specified regular expression pattern.
    /// </summary>
    /// <param name="pattern">The regex pattern to match.</param>
    public RuleBuilder<T, TProperty> Matches(string pattern);

    /// <summary>
    /// Validates that the value is equal to another value.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    public RuleBuilder<T, TProperty> EqualTo(TProperty other);

    /// <summary>
    /// Validates that the value is greater than the specified threshold.
    /// </summary>
    /// <param name="threshold">The minimum exclusive value.</param>
    public RuleBuilder<T, TProperty> GreaterThan(IComparable threshold);

    /// <summary>
    /// Validates that the value is less than the specified threshold.
    /// </summary>
    /// <param name="threshold">The maximum exclusive value.</param>
    public RuleBuilder<T, TProperty> LessThan(IComparable threshold);

    /// <summary>
    /// Validates that the value is not null.
    /// </summary>
    public RuleBuilder<T, TProperty> NotNull();

    /// <summary>
    /// Validates that the value is null.
    /// </summary>
    public RuleBuilder<T, TProperty> Null();

    /// <summary>
    /// Validates that the value is empty. For strings, checks if length is 0. For collections, checks if there are no items.
    /// </summary>
    public RuleBuilder<T, TProperty> Empty();

    /// <summary>
    /// Validates that the value is a valid email address using a standard regular expression.
    /// </summary>
    public RuleBuilder<T, TProperty> Email();

    /// <summary>
    /// Validates that the value is a valid URL.
    /// </summary>
    public RuleBuilder<T, TProperty> Url();

    /// <summary>
    /// Validates that the value contains only alphabetic characters (A-Z, a-z).
    /// </summary>
    public RuleBuilder<T, TProperty> IsAlpha();

    /// <summary>
    /// Validates that the value contains only alphanumeric characters (letters and numbers).
    /// </summary>
    public RuleBuilder<T, TProperty> IsAlphanumeric();

    /// <summary>
    /// Validates that the value is a valid numeric value (integer or decimal).
    /// </summary>
    public RuleBuilder<T, TProperty> IsNumeric();

    /// <summary>
    /// Validates that the value is within the specified inclusive range.
    /// </summary>
    /// <typeparam name="TComparable">A comparable type for range validation.</typeparam>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    public RuleBuilder<T, TProperty> Between<TComparable>(TComparable min, TComparable max)
        where TComparable : IComparable;

    /// <summary>
    /// Validates that the string value starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    public RuleBuilder<T, TProperty> StartsWith(string prefix);

    /// <summary>
    /// Validates that the string value ends with the specified suffix.
    /// </summary>
    /// <param name="suffix">The suffix to match.</param>
    public RuleBuilder<T, TProperty> EndsWith(string suffix);

    /// <summary>
    /// Validates that the value exists in a predefined collection of values.
    /// </summary>
    /// <param name="values">The allowed values.</param>
    public RuleBuilder<T, TProperty> In(IEnumerable<TProperty> values);

    /// <summary>
    /// Validates that the numeric value is greater than zero.
    /// </summary>
    RuleBuilder<T, TProperty> Positive();

    /// <summary>
    /// Validates that the numeric value is less than zero.
    /// </summary>
    RuleBuilder<T, TProperty> Negative();

    /// <summary>
    /// Validates that the numeric value is not zero.
    /// </summary>
    RuleBuilder<T, TProperty> NotZero();

    /// <summary>
    /// Validates that the date value is in the future (greater than today).
    /// </summary>
    RuleBuilder<T, TProperty> FutureDate();

    /// <summary>
    /// Validates that the date value is in the past (less than today).
    /// </summary>
    RuleBuilder<T, TProperty> PastDate();

    /// <summary>
    /// Validates that the date value is today (ignores time component).
    /// </summary>
    RuleBuilder<T, TProperty> Today();

    /// <summary>
    /// Validates that a collection has exactly the specified number of elements.
    /// </summary>
    /// <param name="count">The required element count.</param>
    RuleBuilder<T, TProperty> HasCount(int count);

    /// <summary>
    /// Validates that a collection is not empty.
    /// </summary>
    RuleBuilder<T, TProperty> NotEmptyCollection();

    /// <summary>
    /// Validates the value using an asynchronous boolean predicate.
    /// </summary>
    /// <param name="predicateAsync">A function that defines the async validation logic.</param>
    public RuleBuilder<T, TProperty> MustAsync(Func<TProperty, Task<bool>> predicateAsync);

    /// <summary>
    /// Defines an asynchronous validation rule that evaluates a property in relation to another property of the object using an asynchronous predicate.
    /// The rule checks that the primary property satisfies a condition dependent on the value of a secondary property, with the evaluation performed asynchronously.
    /// </summary>
    /// <typeparam name="TDependent">The type of the dependent property.</typeparam>
    /// <param name="propertyExpression">A lambda expression identifying the primary property to validate (e.g., <c>x => x.Email</c>).</param>
    /// <param name="dependentPropertyExpression">A lambda expression identifying the dependent property whose relationship will be evaluated (e.g., <c>x => x.Username</c>).</param>
    /// <param name="predicateAsync">An asynchronous predicate that takes the values of the primary and dependent properties, returning <c>true</c> if the validation succeeds or <c>false</c> if it fails.</param>
    /// <returns>The same <see cref="RuleBuilder{T, TProperty}"/> to allow fluent chaining of validation rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="propertyExpression"/>, <paramref name="dependentPropertyExpression"/>, or <paramref name="predicateAsync"/> is <c>null</c>.</exception>
    /// <remarks>
    /// <para>The method registers an asynchronous rule in the associated validator, which is executed during synchronous (<see cref="AbstractValidator{T}.Validate(T)"/>) or asynchronous (<see cref="AbstractValidator{T}.ValidateAsync(T)"/>) validation.</para>
    /// <para>Property names are automatically extracted from the expressions to generate precise error messages.</para>
    /// <para>The default error message is "The field {propertyName} does not meet the condition dependent on {dependentPropertyName}.", but it can be customized using <see cref="WithMessage(string)"/>.</para>
    /// </remarks>
    public RuleBuilder<T, TProperty> DependentRuleAsync<TDependent>(
        Expression<Func<T, TProperty>> propertyExpression,
        Expression<Func<T, TDependent>> dependentPropertyExpression,
        Func<TProperty, TDependent, Task<bool>> predicateAsync);
}