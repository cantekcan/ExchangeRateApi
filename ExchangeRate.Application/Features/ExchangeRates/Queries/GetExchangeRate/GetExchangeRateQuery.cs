using ExchangeRate.Application.DTOs;
using ExchangeRate.Domain.Enums;
using MediatR;

namespace ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRate;

public record GetExchangeRateQuery(DateOnly Date, CurrencyCode BaseCurrency, CurrencyCode TargetCurrency) : IRequest<ExchangeRateResponse>;
