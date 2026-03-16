using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Exceptions;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.MediatR;

/// <summary>
/// MediatR pipeline behavior that validates the incoming request using Vali-Validation.
/// <para>
/// Throws <see cref="ValidationException"/> when validation fails.
/// If no <see cref="IValidator{TRequest}"/> is registered, the behavior is a no-op.
/// </para>
/// <para>
/// Tip: if you want richer error handling (e.g. returning a typed Result instead of throwing),
/// consider using <b>Vali-Mediator</b> with the <c>Vali-Mediator.Validation</c> integration —
/// it automatically maps validation failures to <c>Result&lt;T&gt;.Fail(...)</c> without exceptions.
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
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validator = _serviceProvider.GetService<IValidator<TRequest>>();
        if (validator is null)
            return await next().ConfigureAwait(false);

        var result = await validator.ValidateAsync(request).ConfigureAwait(false);
        if (result.IsValid)
            return await next().ConfigureAwait(false);

        throw new ValidationException(result);
    }
}
