using ExchangeRate.Application.Abstractions;
using ExchangeRate.Domain.Enums;
using ExchangeRate.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ExchangeRate.Application.Services;

public class ExchangeRateManager : IExchangeRateManager
{
    private readonly IFrankfurterApiClient _apiClient;
    private readonly ILogger<ExchangeRateManager> _logger;

    public ExchangeRateManager(IFrankfurterApiClient apiClient, ILogger<ExchangeRateManager> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<ExchangeRateModel> GetExchangeRateAsync(DateOnly date, CurrencyCode baseCurrency, CurrencyCode targetCurrency, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting exchange rate for {BaseCurrency}/{TargetCurrency} on {Date}", baseCurrency, targetCurrency, date);
        
        if (baseCurrency == targetCurrency)
        {
            return new ExchangeRateModel
            {
                Date = date.ToString("yyyy-MM-dd"),
                Base = baseCurrency.ToString(),
                Target = targetCurrency.ToString(),
                Rate = 1.0m
            };
        }

        return await _apiClient.GetRateAsync(date, baseCurrency, targetCurrency, cancellationToken);
    }

    public async Task<ExchangeRatesListModel> GetExchangeRatesAsync(DateOnly date, CurrencyCode baseCurrency, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all exchange rates for base {BaseCurrency} on {Date}", baseCurrency, date);
        
        return await _apiClient.GetRatesAsync(date, baseCurrency, cancellationToken);
    }
}
