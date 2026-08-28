using ExchangeRate.Api.Controllers;
using ExchangeRate.Application.DTOs;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRate;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRates;
using ExchangeRate.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace ExchangeRate.Application.Tests;

public class ExchangeRateControllerTests
{
    private readonly IMediator _mediatorMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ExchangeRateController _sut;

    public ExchangeRateControllerTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        
        // Mock current date as 2026-08-28
        var mockToday = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
        _timeProvider = new FakeTimeProvider(mockToday);
        
        _sut = new ExchangeRateController(_mediatorMock, _timeProvider);
    }

    [Fact]
    public async Task GetExchangeRate_ShouldReturnBadRequest_WhenDateIsInFuture()
    {
        // Arrange
        var futureDate = new DateOnly(2026, 8, 29); // 1 day after mockToday
        var baseCurrency = CurrencyCode.USD;
        var targetCurrency = CurrencyCode.TRY;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.GetExchangeRate(baseCurrency, targetCurrency, futureDate, cancellationToken);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Date cannot be in the future.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetExchangeRate_ShouldReturnOk_WhenDateIsTodayOrPast()
    {
        // Arrange
        var todayDate = new DateOnly(2026, 8, 28);
        var baseCurrency = CurrencyCode.USD;
        var targetCurrency = CurrencyCode.TRY;
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new ExchangeRateResponse
        {
            Date = "2026-08-28",
            BaseCurrency = "USD",
            TargetCurrency = "TRY",
            Rate = 34.5m
        };

        _mediatorMock.Send(Arg.Any<GetExchangeRateQuery>(), cancellationToken)
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetExchangeRate(baseCurrency, targetCurrency, todayDate, cancellationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<ExchangeRateResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Rate, returnedResponse.Rate);
    }

    [Fact]
    public async Task GetExchangeRates_ShouldReturnBadRequest_WhenDateIsInFuture()
    {
        // Arrange
        var futureDate = new DateOnly(2026, 8, 29);
        var baseCurrency = CurrencyCode.USD;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.GetExchangeRates(baseCurrency, futureDate, cancellationToken);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Date cannot be in the future.", badRequestResult.Value);
    }

    private class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
