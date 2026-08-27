using ExchangeRate.Application.DTOs;
using ExchangeRate.Domain.Enums;
using MediatR;

namespace ExchangeRate.Application.Features.ExchangeRates.Queries.GetExchangeRates;

public record GetExchangeRatesQuery(DateOnly Date, CurrencyCode BaseCurrency) : IRequest<ExchangeRatesListResponse>;
