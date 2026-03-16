using Microsoft.Extensions.DependencyInjection;
using Vali_Mediator.Core.General.Behavior;
using Vali_Mediator.Core.Request;
using Vali_Mediator.Core.Result;
using Vali_Validation.Core.Exceptions;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.ValiMediator;

/// <summary>
/// Pipeline behavior that validates the incoming request before it reaches its handler.
/// <para>
/// If <typeparamref name="TResponse"/> is <see cref="Result{T}"/>, validation failures are
/// returned as <c>Result.Fail(errors, ErrorType.Validation)</c> without throwing.
/// Otherwise, a <see cref="ValidationException"/> is thrown.
/// </para>
/// <para>
/// If no <see cref="IValidator{TRequest}"/> is registered for the request type,
/// the behavior is a no-op and the pipeline continues normally.
/// </para>
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var validator = _serviceProvider.GetService<IValidator<TRequest>>();
        if (validator is null)
            return await next().ConfigureAwait(false);

        var validationResult = await validator.ValidateAsync(request).ConfigureAwait(false);
        if (validationResult.IsValid)
            return await next().ConfigureAwait(false);

        // Check if TResponse is Result<T> (avoid throwing for Result-based handlers)
        bool isResultType = typeof(TResponse).IsGenericType &&
                            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>);

        // Check if TResponse is non-generic Result (void result)
        bool isVoidResultType = typeof(TResponse) == typeof(Vali_Mediator.Core.Result.Result);

        if (isResultType)
        {
            var innerType = typeof(TResponse).GetGenericArguments()[0];
            var failMethod = typeof(Result<>)
                .MakeGenericType(innerType)
                .GetMethod("Fail", new[] { typeof(Dictionary<string, List<string>>), typeof(ErrorType) });

            var errorsDict = validationResult.Errors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);

            return (TResponse)failMethod!.Invoke(null, new object[] { errorsDict, ErrorType.Validation })!;
        }
        else if (isVoidResultType)
        {
            string errorMessage = string.Join("; ", validationResult.Errors.SelectMany(e => e.Value));
            var voidFail = Vali_Mediator.Core.Result.Result.Fail(errorMessage, Vali_Mediator.Core.Result.ErrorType.Validation);
            return (TResponse)(object)voidFail;
        }

        throw new ValidationException(validationResult);
    }
}
