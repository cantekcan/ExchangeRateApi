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

    public ExchangeRateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{baseCurrency}/{targetCurrency}")]
    public async Task<IActionResult> GetExchangeRate(CurrencyCode baseCurrency, CurrencyCode targetCurrency, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var queryDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var query = new GetExchangeRateQuery(queryDate, baseCurrency, targetCurrency);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{baseCurrency}")]
    public async Task<IActionResult> GetExchangeRates(CurrencyCode baseCurrency, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var queryDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var query = new GetExchangeRatesQuery(queryDate, baseCurrency);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
