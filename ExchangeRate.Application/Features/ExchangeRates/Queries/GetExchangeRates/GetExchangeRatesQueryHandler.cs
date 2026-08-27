using ExchangeRate.Application.Abstractions;
using ExchangeRate.Application.DTOs;
using MediatR;

namespace ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRates;

public class GetExchangeRatesQueryHandler : IRequestHandler<GetExchangeRatesQuery, ExchangeRatesListResponse>
{
    private readonly IExchangeRateManager _exchangeRateManager;

    public GetExchangeRatesQueryHandler(IExchangeRateManager exchangeRateManager)
    {
        _exchangeRateManager = exchangeRateManager;
    }

    public async Task<ExchangeRatesListResponse> Handle(GetExchangeRatesQuery request, CancellationToken cancellationToken)
    {
        var result = await _exchangeRateManager.GetExchangeRatesAsync(
            request.Date, 
            request.BaseCurrency, 
            cancellationToken);

        return new ExchangeRatesListResponse
        {
            Date = result.Date,
            BaseCurrency = result.Base,
            Rates = result.Rates
        };
    }
}
