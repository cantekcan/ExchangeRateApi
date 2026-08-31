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
    // Test branch protection and CI pipeline trigger
    intentionally_broken_syntax_error_here;

    public ExchangeRateController(IMediator mediator, TimeProvider timeProvider)
    {
        _mediator = mediator;
        _timeProvider = timeProvider;
    }

    [HttpGet("{baseCurrency}/{targetCurrency}")]
    public async Task<IActionResult> GetExchangeRate(CurrencyCode baseCurrency, CurrencyCode targetCurrency, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var validationResult = GetValidatedDate(date, out var queryDate);
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
        var validationResult = GetValidatedDate(date, out var queryDate);
        if (validationResult != null)
        {
            return validationResult;
        }

        var query = new GetExchangeRatesQuery(queryDate, baseCurrency);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    private IActionResult? GetValidatedDate(DateOnly? inputDate, out DateOnly finalDate)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        finalDate = inputDate ?? today;

        if (finalDate > today)
        {
            return BadRequest("Date cannot be in the future.");
        }

        return null;
    }
}
