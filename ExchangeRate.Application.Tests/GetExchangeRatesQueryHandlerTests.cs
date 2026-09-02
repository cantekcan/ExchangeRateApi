using ExchangeRate.Application.Abstractions;
using ExchangeRate.Application.DTOs;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRates;
using ExchangeRate.Domain.Enums;
using ExchangeRate.Domain.Models;
using NSubstitute;
using Xunit;

namespace ExchangeRate.Application.Tests;

public class GetExchangeRatesQueryHandlerTests
{
    private readonly IExchangeRateManager _exchangeRateManagerMock;
    private readonly GetExchangeRatesQueryHandler _sut;

    public GetExchangeRatesQueryHandlerTests()
    {
        _exchangeRateManagerMock = Substitute.For<IExchangeRateManager>();
        _sut = new GetExchangeRatesQueryHandler(_exchangeRateManagerMock);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedResponse_WhenManagerReturnsRates()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 28);
        var query = new GetExchangeRatesQuery(date, CurrencyCode.USD);
        var cancellationToken = CancellationToken.None;

        var managerResult = new ExchangeRatesListModel
        {
            Date = "2026-08-28",
            Base = "USD",
            Rates = new Dictionary<string, decimal>
            {
                { "TRY", 34.5m },
                { "EUR", 0.92m }
            }
        };

        _exchangeRateManagerMock.GetExchangeRatesAsync(date, CurrencyCode.USD, cancellationToken)
            .Returns(managerResult);

        // Act
        var result = await _sut.Handle(query, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(managerResult.Date, result.Date);
        Assert.Equal(managerResult.Base, result.BaseCurrency);
        Assert.Equal(managerResult.Rates, result.Rates);

        // Verify collaboration
        await _exchangeRateManagerMock.Received(1).GetExchangeRatesAsync(date, CurrencyCode.USD, cancellationToken);
    }
}
