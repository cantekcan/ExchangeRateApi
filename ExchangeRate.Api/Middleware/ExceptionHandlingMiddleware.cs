using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRate.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.RequestAborted.IsCancellationRequested && exception is OperationCanceledException)
        {
            // Client canceled the request, no need to send response
            return Task.CompletedTask;
        }

        var code = HttpStatusCode.InternalServerError;
        var title = "An internal server error occurred.";
        var detail = _env.IsDevelopment() 
            ? exception.ToString() 
            : "An unexpected error occurred. Please try again later.";

        switch (exception)
        {
            case HttpRequestException httpRequestException:
                code = HttpStatusCode.BadGateway;
                title = "External API error.";
                detail = _env.IsDevelopment() 
                    ? httpRequestException.Message 
                    : "Failed to retrieve data from the external provider.";
                break;

            case OperationCanceledException _:
                code = HttpStatusCode.RequestTimeout;
                title = "The request timed out.";
                detail = "The operation was canceled because it exceeded the timeout limit.";
                break;

            case ArgumentException argumentException:
                code = HttpStatusCode.BadRequest;
                title = "Invalid request.";
                detail = argumentException.Message;
                break;
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)code,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsJsonAsync(problemDetails, JsonOptions, "application/problem+json");
    }
}
