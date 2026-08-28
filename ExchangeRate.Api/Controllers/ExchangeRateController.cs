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
    public async Task<IActionResult> GetExchangeRate(CurrencyCode baseCurrency, CurrencyCode targetCurrency, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var queryDate = date ?? today;

        var validationResult = ValidateDate(queryDate, today);
        if (validationResult != null)
        {
            return validationResult;
        }

        var query = new GetExchangeRateQuery(queryDate, baseCurrency, targetCurrency);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{baseCurrency}")]
    public async Task<IActionResult> GetExchangeRates(CurrencyCode baseCurrency, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var queryDate = date ?? today;

        var validationResult = ValidateDate(queryDate, today);
        if (validationResult != null)
        {
            return validationResult;
        }

        var query = new GetExchangeRatesQuery(queryDate, baseCurrency);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    private IActionResult? ValidateDate(DateOnly queryDate, DateOnly today)
    {
        if (queryDate > today)
        {
            return BadRequest("Date cannot be in the future.");
        }
        return null;
    }
}
