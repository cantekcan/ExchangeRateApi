using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using ExchangeRate.Application.Abstractions;
using ExchangeRate.Application.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text;
using ExchangeRate.Infrastructure.Configuration;
using ExchangeRate.Infrastructure.ExternalServices.Frankfurter;
using ExchangeRate.Api.Middleware;
using MediatR;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRate;

using ExchangeRate.Api.ModelBinders;

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

// HttpClient with Typed Client
builder.Services.AddHttpClient<IFrankfurterApiClient, FrankfurterApiClient>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetExchangeRateQuery).Assembly));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("System is running"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteResponse
});

app.Run();

static Task WriteResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var options = new JsonWriterOptions { Indented = true };

    using var memoryStream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(memoryStream, options))
    {
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

    return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
}

public partial class Program { }
