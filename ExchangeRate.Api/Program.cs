using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ExchangeRate.Api.Middleware;
using ExchangeRate.Api.ModelBinders;
using ExchangeRate.Application.Abstractions;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRate;
using ExchangeRate.Application.Services;
using ExchangeRate.Infrastructure.Configuration;
using ExchangeRate.Infrastructure.ExternalServices.Frankfurter;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using OpenTelemetry.Trace;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
    {
        options.ModelBinderProviders.Insert(0, new DateOnlyModelBinderProvider());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Format = "date"
    });
});

// Options Pattern
builder.Services.Configure<FrankfurterOptions>(
    builder.Configuration.GetSection(FrankfurterOptions.SectionName));

// DI Registrations
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IExchangeRateManager, ExchangeRateManager>();

// HttpClient with Typed Client and Standard Resilience
builder.Services.AddHttpClient<IFrankfurterApiClient, FrankfurterApiClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.Delay = TimeSpan.FromMilliseconds(200);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    });

// OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation();
    });

// IP-Based Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please try again later.",
            Instance = context.HttpContext.Request.Path
        };

        await context.HttpContext.Response.WriteAsJsonAsync(
            problemDetails,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            "application/problem+json",
            token);
    };

    options.AddPolicy("ip-policy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetExchangeRateQuery).Assembly));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("System is running"));

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<CorrelationIdMiddleware>();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();

app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteResponse
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers().RequireRateLimiting("ip-policy");

await app.RunAsync();

static async Task WriteResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var options = new JsonWriterOptions { Indented = true };

    await using var writer = new Utf8JsonWriter(context.Response.Body, options);

    writer.WriteStartObject();
    writer.WriteString("status", report.Status.ToString());
    writer.WriteStartObject("results");

    foreach (var entry in report.Entries)
    {
        writer.WriteStartObject(entry.Key);
        writer.WriteString("status", entry.Value.Status.ToString());
        writer.WriteString("description", entry.Value.Description);
        writer.WriteEndObject();
    }

    writer.WriteEndObject();
    writer.WriteEndObject();
}

public partial class Program { }
