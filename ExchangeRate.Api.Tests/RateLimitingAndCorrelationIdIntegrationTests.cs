using ExchangeRate.Api.Middleware;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
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
}
