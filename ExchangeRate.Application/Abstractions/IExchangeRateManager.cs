using ExchangeRate.Domain.Enums;
using ExchangeRate.Domain.Models;

namespace ExchangeRate.Application.Abstractions;

public interface IExchangeRateManager
{
    Task<ExchangeRateModel> GetExchangeRateAsync(DateOnly date, CurrencyCode baseCurrency, CurrencyCode targetCurrency, CancellationToken cancellationToken);
    Task<ExchangeRatesListModel> GetExchangeRatesAsync(DateOnly date, CurrencyCode baseCurrency, CancellationToken cancellationToken);
}
