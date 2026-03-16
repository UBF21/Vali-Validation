using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Vali_Validation.Core.Validators;

namespace Vali_Validation.AspNetCore;

/// <summary>
/// Action filter attribute that automatically validates all action arguments
/// that have a registered <see cref="IValidator{T}"/> before the action executes.
/// Returns HTTP 400 with structured validation errors when any argument fails validation.
/// </summary>
/// <remarks>
/// Apply to individual actions or entire controllers.
/// Validators must be registered in the DI container (e.g. via <c>AddValidationsFromAssembly</c>).
/// </remarks>
/// <example>
/// <code>
/// [ApiController]
/// [ValiValidate]
/// public class UsersController : ControllerBase
/// {
///     [HttpPost]
///     public IActionResult Create(CreateUserDto dto) => Ok();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ValiValidateAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = services.GetService(validatorType);

            if (validator is null) continue;

            // Invoke ValidateAsync via reflection (IValidator<T> is generic)
            var validateMethod = validatorType.GetMethod(
                nameof(IValidator<object>.ValidateAsync),
                new[] { argumentType, typeof(CancellationToken) });

            if (validateMethod is null) continue;

            var task = (Task)validateMethod.Invoke(validator, new object[] { argument, CancellationToken.None })!;
            await task.ConfigureAwait(false);

            // Get the ValidationResult from the completed task
            var resultProperty = task.GetType().GetProperty("Result");
            var validationResult = resultProperty?.GetValue(task) as Vali_Validation.Core.Results.ValidationResult;

            if (validationResult is null || validationResult.IsValid) continue;

            var errors = validationResult.Errors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray());

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
            return;
        }

        await next();
    }
}
