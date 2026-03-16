using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Vali_Validation.Core.Exceptions;

namespace Vali_Validation.AspNetCore;

/// <summary>
/// Middleware that catches <see cref="ValidationException"/> from the pipeline
/// and returns an HTTP 400 response with a ProblemDetails-style JSON body.
/// </summary>
public class ValiValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ValiValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var errors = ex.ValidationResult.Errors
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToArray());

            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Validation Failed",
                status = 400,
                errors = errors
            };

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false
            });

            await context.Response.WriteAsync(json);
        }
    }
}
