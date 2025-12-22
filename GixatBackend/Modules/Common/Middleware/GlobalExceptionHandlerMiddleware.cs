using System.Net;
using System.Text.Json;
using GixatBackend.Modules.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GixatBackend.Modules.Common.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and returns appropriate HTTP responses
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = exception switch
        {
            EntityNotFoundException ex => (
                HttpStatusCode.NotFound,
                CreateErrorResponse("Entity Not Found", ex.Message, ex)
            ),
            EntityNotFoundInOrganizationException ex => (
                HttpStatusCode.NotFound,
                CreateErrorResponse("Entity Not Found", ex.Message, ex)
            ),
            BusinessRuleViolationException ex => (
                HttpStatusCode.BadRequest,
                CreateErrorResponse("Business Rule Violation", ex.Message, ex)
            ),
            ValidationException ex => (
                HttpStatusCode.BadRequest,
                CreateValidationErrorResponse(ex)
            ),
            ExternalServiceException ex => (
                HttpStatusCode.BadGateway,
                CreateErrorResponse("External Service Error", ex.Message, ex)
            ),
            UnauthorizedAccessException ex => (
                HttpStatusCode.Unauthorized,
                CreateErrorResponse("Unauthorized", ex.Message, ex)
            ),
            InvalidOperationException ex => (
                HttpStatusCode.BadRequest,
                CreateErrorResponse("Invalid Operation", ex.Message, ex)
            ),
            ArgumentNullException ex => (
                HttpStatusCode.BadRequest,
                CreateErrorResponse("Invalid Argument", $"Required parameter '{ex.ParamName}' is missing", ex)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                CreateErrorResponse("Internal Server Error", "An unexpected error occurred", exception)
            )
        };

        LogException(exception, statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, jsonOptions)).ConfigureAwait(false);
    }

    private ProblemDetails CreateErrorResponse(string title, string detail, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Type = exception.GetType().Name
        };

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
        }

        return problemDetails;
    }

    private ProblemDetails CreateValidationErrorResponse(ValidationException exception)
    {
        var problemDetails = new ProblemDetails
        {
            Title = "Validation Failed",
            Detail = exception.Message,
            Type = nameof(ValidationException)
        };

        problemDetails.Extensions["errors"] = exception.Errors;

        return problemDetails;
    }

    private void LogException(Exception exception, HttpStatusCode statusCode)
    {
        var logLevel = statusCode switch
        {
            HttpStatusCode.InternalServerError => LogLevel.Error,
            HttpStatusCode.BadGateway => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, exception, 
            "Exception caught by global handler: {ExceptionType} - {Message}", 
            exception.GetType().Name, 
            exception.Message);
    }
}

/// <summary>
/// Extension method to register the global exception handler middleware
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
