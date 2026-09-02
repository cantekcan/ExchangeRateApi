using ExchangeRate.Application.Abstractions;
using ExchangeRate.Application.DTOs;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRate;
using ExchangeRate.Domain.Enums;
using ExchangeRate.Domain.Models;
using NSubstitute;
using Xunit;

namespace ExchangeRate.Application.Tests;

public class GetExchangeRateQueryHandlerTests
{
    private readonly IExchangeRateManager _exchangeRateManagerMock;
    private readonly GetExchangeRateQueryHandler _sut;

    public GetExchangeRateQueryHandlerTests()
    {
        _exchangeRateManagerMock = Substitute.For<IExchangeRateManager>();
        _sut = new GetExchangeRateQueryHandler(_exchangeRateManagerMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedResponse_WhenManagerReturnsRate()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 28);
        var query = new GetExchangeRateQuery(date, CurrencyCode.USD, CurrencyCode.TRY);
        var cancellationToken = CancellationToken.None;

        var managerResult = new ExchangeRateModel
        {
            Date = "2026-08-28",
            Base = "USD",
            Target = "TRY",
            Rate = 34.5m
        };

        _exchangeRateManagerMock.GetExchangeRateAsync(date, CurrencyCode.USD, CurrencyCode.TRY, cancellationToken)
            .Returns(managerResult);

        // Act
        var result = await _sut.Handle(query, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(managerResult.Date, result.Date);
        Assert.Equal(managerResult.Base, result.BaseCurrency);
        Assert.Equal(managerResult.Target, result.TargetCurrency);
        Assert.Equal(managerResult.Rate, result.Rate);

        // Verify collaboration
        await _exchangeRateManagerMock.Received(1).GetExchangeRateAsync(date, CurrencyCode.USD, CurrencyCode.TRY, cancellationToken);
    }
}
