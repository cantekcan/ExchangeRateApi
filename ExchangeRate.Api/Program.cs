using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using ExchangeRate.Application.Abstractions;
using ExchangeRate.Application.Services;
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
builder.Services.AddScoped<IExchangeRateManager, ExchangeRateManager>();

// HttpClient with Typed Client
builder.Services.AddHttpClient<IFrankfurterApiClient, FrankfurterApiClient>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetExchangeRateQuery).Assembly));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
