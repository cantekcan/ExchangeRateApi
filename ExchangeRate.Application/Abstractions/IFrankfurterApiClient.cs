using ExchangeRate.Domain.Enums;
using ExchangeRate.Domain.Models;

namespace ExchangeRate.Application.Abstractions;

public interface IFrankfurterApiClient
{
    Task<ExchangeRateModel> GetRateAsync(DateOnly date, CurrencyCode baseCurrency, CurrencyCode targetCurrency, CancellationToken cancellationToken);
    Task<ExchangeRatesListModel> GetRatesAsync(DateOnly date, CurrencyCode baseCurrency, CancellationToken cancellationToken);
}
