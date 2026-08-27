using System.Net;
using System.Text.Json;

namespace ExchangeRate.Api.Middleware;

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
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.RequestAborted.IsCancellationRequested && exception is OperationCanceledException)
        {
            // Client canceled the request, no need to send response
            return Task.CompletedTask;
        }

        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;

        switch (exception)
        {
            case HttpRequestException httpEx:
                code = HttpStatusCode.BadGateway;
                result = JsonSerializer.Serialize(new { error = "External API error.", detail = httpEx.Message });
                break;
            case OperationCanceledException _:
                code = HttpStatusCode.RequestTimeout;
                result = JsonSerializer.Serialize(new { error = "The request timed out." });
                break;
            default:
                result = JsonSerializer.Serialize(new { error = "An internal server error occurred." });
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(result);
    }
}
