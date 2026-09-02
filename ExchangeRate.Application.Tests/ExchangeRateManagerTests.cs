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
    private readonly TimeProvider _timeProviderMock;
    private readonly ILogger<ExchangeRateManager> _loggerMock;
    private readonly ExchangeRateManager _sut; // System Under Test

    public ExchangeRateManagerTests()
    {
        _apiClientMock = Substitute.For<IFrankfurterApiClient>();
        _timeProviderMock = Substitute.For<TimeProvider>();
        _timeProviderMock.GetUtcNow().Returns(new DateTimeOffset(2026, 8, 28, 0, 0, 0, TimeSpan.Zero));
        _loggerMock = Substitute.For<ILogger<ExchangeRateManager>>();
        _sut = new ExchangeRateManager(_apiClientMock, _timeProviderMock, _loggerMock);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ShouldThrowArgumentException_WhenBaseAndTargetCurrenciesAreSame()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 28);
        var baseCurrency = CurrencyCode.USD;
        var targetCurrency = CurrencyCode.USD;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetExchangeRateAsync(date, baseCurrency, targetCurrency, cancellationToken));

        Assert.Equal("Base currency and target currency must be different.", exception.Message);
        await _apiClientMock.DidNotReceiveWithAnyArgs().GetRateAsync(default, default, default, default);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ShouldThrowArgumentException_WhenDateIsInFuture()
    {
        // Arrange
        var futureDate = new DateOnly(2026, 8, 29); // 1 day after mock today
        var baseCurrency = CurrencyCode.USD;
        var targetCurrency = CurrencyCode.TRY;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetExchangeRateAsync(futureDate, baseCurrency, targetCurrency, cancellationToken));

        Assert.Equal("Date cannot be in the future.", exception.Message);
        await _apiClientMock.DidNotReceiveWithAnyArgs().GetRateAsync(default, default, default, default);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ShouldThrowArgumentException_WhenDateIsBeforeMinSupportedDate()
    {
        // Arrange
        var oldDate = new DateOnly(1999, 1, 3); // 1 day before min supported date (Jan 4, 1999)
        var baseCurrency = CurrencyCode.USD;
        var targetCurrency = CurrencyCode.TRY;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetExchangeRateAsync(oldDate, baseCurrency, targetCurrency, cancellationToken));

        Assert.Equal("Date cannot be before January 4, 1999.", exception.Message);
        await _apiClientMock.DidNotReceiveWithAnyArgs().GetRateAsync(default, default, default, default);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ShouldCallApiClient_WhenInputsAreValid()
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

        await _apiClientMock.Received(1).GetRateAsync(date, baseCurrency, targetCurrency, cancellationToken);
    }

    [Fact]
    public async Task GetExchangeRatesAsync_ShouldThrowArgumentException_WhenDateIsInFuture()
    {
        // Arrange
        var futureDate = new DateOnly(2026, 8, 29);
        var baseCurrency = CurrencyCode.USD;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetExchangeRatesAsync(futureDate, baseCurrency, cancellationToken));

        Assert.Equal("Date cannot be in the future.", exception.Message);
        await _apiClientMock.DidNotReceiveWithAnyArgs().GetRatesAsync(default, default, default);
    }

    [Fact]
    public async Task GetExchangeRatesAsync_ShouldCallApiClient_WhenInputsAreValid()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 28);
        var baseCurrency = CurrencyCode.USD;
        var cancellationToken = CancellationToken.None;

        var expectedModel = new ExchangeRatesListModel
        {
            Date = "2026-08-28",
            Base = "USD",
            Rates = new Dictionary<string, decimal> { { "TRY", 34.5m } }
        };

        _apiClientMock.GetRatesAsync(date, baseCurrency, cancellationToken)
            .Returns(expectedModel);

        // Act
        var result = await _sut.GetExchangeRatesAsync(date, baseCurrency, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedModel.Date, result.Date);
        Assert.Equal(expectedModel.Base, result.Base);
        Assert.Equal(expectedModel.Rates, result.Rates);

        await _apiClientMock.Received(1).GetRatesAsync(date, baseCurrency, cancellationToken);
    }
}
