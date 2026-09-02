using ExchangeRate.Application.Abstractions;
using ExchangeRate.Application.DTOs;
using MediatR;

namespace ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRate;

public class GetExchangeRateQueryHandler : IRequestHandler<GetExchangeRateQuery, ExchangeRateResponse>
{
    private readonly IExchangeRateManager _exchangeRateManager;

    public GetExchangeRateQueryHandler(IExchangeRateManager exchangeRateManager)
    {
        _exchangeRateManager = exchangeRateManager;
    }

    public async Task<ExchangeRateResponse> Handle(GetExchangeRateQuery request, CancellationToken cancellationToken)
    {
        var result = await _exchangeRateManager.GetExchangeRateAsync(
            request.Date, 
            request.BaseCurrency, 
            request.TargetCurrency, 
            cancellationToken);

        return new ExchangeRateResponse
        {
            Date = result.Date,
            BaseCurrency = result.Base,
            TargetCurrency = result.Target,
            Rate = result.Rate
        };
    }
}
