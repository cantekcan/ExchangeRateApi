using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExchangeRate.Api.Middleware;

public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Items["CorrelationId"] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
            }
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
