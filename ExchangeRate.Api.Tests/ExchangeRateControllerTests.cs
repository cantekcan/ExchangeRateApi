using ExchangeRate.Api.Controllers;
using ExchangeRate.Application.DTOs;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRate;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRates;
using ExchangeRate.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace ExchangeRate.Api.Tests;

public class ExchangeRateControllerTests
{
    private readonly IMediator _mediatorMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ExchangeRateController _sut;

    public ExchangeRateControllerTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        
        var mockToday = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
        _timeProvider = new FakeTimeProvider(mockToday);
        
        _sut = new ExchangeRateController(_mediatorMock, _timeProvider);
    }

    [Fact]
    public async Task GetExchangeRate_ShouldCallMediator_AndReturnOk()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 28);
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
        var result = await _sut.GetExchangeRate(baseCurrency, targetCurrency, date, cancellationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<ExchangeRateResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Rate, returnedResponse.Rate);

        await _mediatorMock.Received(1).Send(Arg.Is<GetExchangeRateQuery>(q => 
            q.Date == date && 
            q.BaseCurrency == baseCurrency && 
            q.TargetCurrency == targetCurrency), 
            cancellationToken);
    }

    [Fact]
    public async Task GetExchangeRates_ShouldCallMediator_AndReturnOk()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 28);
        var baseCurrency = CurrencyCode.USD;
        var cancellationToken = CancellationToken.None;

        var expectedResponse = new ExchangeRatesListResponse
        {
            Date = "2026-08-28",
            BaseCurrency = "USD",
            Rates = new Dictionary<string, decimal> { { "TRY", 34.5m } }
        };

        _mediatorMock.Send(Arg.Any<GetExchangeRatesQuery>(), cancellationToken)
            .Returns(expectedResponse);

        // Act
        var result = await _sut.GetExchangeRates(baseCurrency, date, cancellationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<ExchangeRatesListResponse>(okResult.Value);
        Assert.Equal(expectedResponse.BaseCurrency, returnedResponse.BaseCurrency);

        await _mediatorMock.Received(1).Send(Arg.Is<GetExchangeRatesQuery>(q => 
            q.Date == date && 
            q.BaseCurrency == baseCurrency), 
            cancellationToken);
    }
}
