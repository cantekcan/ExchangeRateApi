using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRate;
using ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRates;
using ExchangeRate.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRate.Api.Controllers;

[ApiController]
[Route("api/exchange-rates")]
public class ExchangeRateController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly TimeProvider _timeProvider;

    public ExchangeRateController(IMediator mediator, TimeProvider timeProvider)
    {
        _mediator = mediator;
        _timeProvider = timeProvider;
    }

    [HttpGet("{baseCurrency}/{targetCurrency}")]
    public async Task<IActionResult> GetExchangeRate(
        CurrencyCode baseCurrency, 
        CurrencyCode targetCurrency, 
        [FromQuery] DateOnly? date, 
        CancellationToken cancellationToken)
    {
        var queryDate = date ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var query = new GetExchangeRateQuery(queryDate, baseCurrency, targetCurrency);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{baseCurrency}")]
    public async Task<IActionResult> GetExchangeRates(
        CurrencyCode baseCurrency, 
        [FromQuery] DateOnly? date, 
        CancellationToken cancellationToken)
    {
        var queryDate = date ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var query = new GetExchangeRatesQuery(queryDate, baseCurrency);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
