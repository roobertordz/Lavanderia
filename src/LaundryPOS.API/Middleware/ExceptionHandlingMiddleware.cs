using FluentValidation;
using System.Net;
using System.Text.Json;
using LaundryPOS.Domain.Exceptions;

namespace LaundryPOS.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred.",
                    Details = validationEx.Errors.Select(e => e.ErrorMessage).ToArray()
                }),

            EntityNotFoundException entityEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse { Code = entityEx.Code, Message = entityEx.Message }),

            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse { Code = domainEx.Code, Message = domainEx.Message }),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ErrorResponse { Code = "UNAUTHORIZED", Message = "Access denied." }),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse { Code = "INTERNAL_ERROR", Message = "An unexpected error occurred." })
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning("Handled exception: {Code} - {Message}", errorResponse.Code, errorResponse.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}

public record ErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string[]? Details { get; init; }
}
