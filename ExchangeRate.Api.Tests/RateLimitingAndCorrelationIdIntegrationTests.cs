using ExchangeRate.Api.Middleware;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ExchangeRate.Api.Tests;

public class RateLimitingAndCorrelationIdIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RateLimitingAndCorrelationIdIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeCorrelationIdHeader_InResponse()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeader));
        var correlationId = response.Headers.GetValues(CorrelationIdMiddleware.CorrelationIdHeader).FirstOrDefault();
        Assert.NotNull(correlationId);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task GetHealth_ShouldEchoProvidedCorrelationIdHeader_InResponse()
    {
        // Arrange
        const string customId = "my-custom-tracing-correlation-id-999";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeader, customId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeader));
        var correlationId = response.Headers.GetValues(CorrelationIdMiddleware.CorrelationIdHeader).FirstOrDefault();
        Assert.Equal(customId, correlationId);
    }

    [Fact]
    public async Task GetHealth_ShouldBeExemptFromRateLimiting()
    {
        // Act - hit health check multiple times rapidly
        for (var i = 0; i < 10; i++)
        {
            var response = await _client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ApiEndpoint_ShouldReturnCorrelationIdAndProblemDetails_WhenValidationFailsInPipeline()
    {
        // Act - hit API endpoint with future date to exercise ASP.NET Core pipeline, OpenTelemetry filter, and model binding
        var response = await _client.GetAsync("/api/exchange-rates/2099-01-01?baseCurrency=USD&targetCurrency=TRY");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(response.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeader));

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("One or more validation errors occurred.", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task RateLimiter_ShouldReturn429WithProblemDetails_WhenRateLimitExceeded()
    {
        // Arrange
        var customClient = _client;
        HttpResponseMessage? rejectedResponse = null;

        // Act - send 65 requests to exceed the 60 req/min limit
        for (var i = 0; i < 65; i++)
        {
            var response = await customClient.GetAsync("/api/exchange-rates/2099-01-01?baseCurrency=USD&targetCurrency=TRY");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rejectedResponse = response;
                break;
            }
        }

        // Assert
        Assert.NotNull(rejectedResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        Assert.Equal("application/problem+json", rejectedResponse.Content.Headers.ContentType?.MediaType);
        Assert.True(rejectedResponse.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeader));

        var content = await rejectedResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal(429, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Too Many Requests", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("Rate limit exceeded. Please try again later.", doc.RootElement.GetProperty("detail").GetString());
    }
}
