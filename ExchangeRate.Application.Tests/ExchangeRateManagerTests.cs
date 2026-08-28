using ExchangeRate.Application.Abstractions;
using ExchangeRate.Application.Services;
using ExchangeRate.Domain.Enums;
using ExchangeRate.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ExchangeRate.Application.Tests;

public class ExchangeRateManagerTests
{
    private readonly IFrankfurterApiClient _apiClientMock;
    private readonly ILogger<ExchangeRateManager> _loggerMock;
    private readonly ExchangeRateManager _sut; // System Under Test

    public ExchangeRateManagerTests()
    {
        _apiClientMock = Substitute.For<IFrankfurterApiClient>();
        _loggerMock = Substitute.For<ILogger<ExchangeRateManager>>();
        _sut = new ExchangeRateManager(_apiClientMock, _loggerMock);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ShouldReturnOne_WhenBaseAndTargetCurrenciesAreSame()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 28);
        var baseCurrency = CurrencyCode.USD;
        var targetCurrency = CurrencyCode.USD;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.GetExchangeRateAsync(date, baseCurrency, targetCurrency, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2026-08-28", result.Date);
        Assert.Equal("USD", result.Base);
        Assert.Equal("USD", result.Target);
        Assert.Equal(1.0m, result.Rate);

        // Verify that the external API client was NOT called
        await _apiClientMock.DidNotReceiveWithAnyArgs().GetRateAsync(default, default, default, default);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ShouldCallApiClient_WhenBaseAndTargetCurrenciesAreDifferent()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 28);
        var baseCurrency = CurrencyCode.USD;
        var targetCurrency = CurrencyCode.TRY;
        var cancellationToken = CancellationToken.None;

        var expectedModel = new ExchangeRateModel
        {
            Date = "2026-08-28",
            Base = "USD",
            Target = "TRY",
            Rate = 34.5m
        };

        _apiClientMock.GetRateAsync(date, baseCurrency, targetCurrency, cancellationToken)
            .Returns(expectedModel);

        // Act
        var result = await _sut.GetExchangeRateAsync(date, baseCurrency, targetCurrency, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedModel.Date, result.Date);
        Assert.Equal(expectedModel.Base, result.Base);
        Assert.Equal(expectedModel.Target, result.Target);
        Assert.Equal(expectedModel.Rate, result.Rate);

        // Verify the API client was indeed called with correct arguments
        await _apiClientMock.Received(1).GetRateAsync(date, baseCurrency, targetCurrency, cancellationToken);
    }
}
