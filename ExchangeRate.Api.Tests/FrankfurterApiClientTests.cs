using ExchangeRate.Domain.Enums;
using ExchangeRate.Infrastructure.Configuration;
using ExchangeRate.Infrastructure.ExternalServices.Frankfurter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using System.Text;
using Xunit;

namespace ExchangeRate.Api.Tests;

public class FrankfurterApiClientTests
{
    private readonly ILogger<FrankfurterApiClient> _loggerMock;
    private readonly IOptions<FrankfurterOptions> _options;

    public FrankfurterApiClientTests()
    {
        _loggerMock = Substitute.For<ILogger<FrankfurterApiClient>>();
        _options = Options.Create(new FrankfurterOptions
        {
            BaseUrl = "https://api.frankfurter.dev"
        });
    }

    [Fact]
    public async Task GetRateAsync_ShouldReturnExchangeRateModel_WhenValidResponseReturned()
    {
        // Arrange
        var jsonResponse = """
        {
            "amount": 1.0,
            "base": "USD",
            "date": "2026-08-28",
            "rates": {
                "TRY": 34.05
            }
        }
        """;

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handler);
        var sut = new FrankfurterApiClient(httpClient, _options, _loggerMock);

        // Act
        var result = await sut.GetRateAsync(new DateOnly(2026, 8, 28), CurrencyCode.USD, CurrencyCode.TRY, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("USD", result.Base);
        Assert.Equal("TRY", result.Target);
        Assert.Equal(34.05m, result.Rate);
        Assert.Equal("2026-08-28", result.Date);
    }

    [Fact]
    public async Task GetRateAsync_ShouldThrowHttpRequestException_WhenRateNotFoundInResponse()
    {
        // Arrange
        var jsonResponse = """
        {
            "amount": 1.0,
            "base": "USD",
            "date": "2026-08-28",
            "rates": {
                "EUR": 0.92
            }
        }
        """;

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handler);
        var sut = new FrankfurterApiClient(httpClient, _options, _loggerMock);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.GetRateAsync(new DateOnly(2026, 8, 28), CurrencyCode.USD, CurrencyCode.TRY, CancellationToken.None));

        Assert.Equal("Rate not found in response.", ex.Message);
    }

    [Fact]
    public async Task GetRatesAsync_ShouldReturnExchangeRatesListModel_WhenValidResponseReturned()
    {
        // Arrange
        var jsonResponse = """
        {
            "amount": 1.0,
            "base": "USD",
            "date": "2026-08-28",
            "rates": {
                "EUR": 0.92,
                "TRY": 34.05
            }
        }
        """;

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handler);
        var sut = new FrankfurterApiClient(httpClient, _options, _loggerMock);

        // Act
        var result = await sut.GetRatesAsync(new DateOnly(2026, 8, 28), CurrencyCode.USD, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("USD", result.Base);
        Assert.Equal("2026-08-28", result.Date);
        Assert.Equal(2, result.Rates.Count);
        Assert.Equal(0.92m, result.Rates["EUR"]);
        Assert.Equal(34.05m, result.Rates["TRY"]);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
