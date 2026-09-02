using ExchangeRate.Application.Abstractions;
using ExchangeRate.Domain.Enums;
using ExchangeRate.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ExchangeRate.Application.Services;

public class ExchangeRateManager : IExchangeRateManager
{
    private static readonly DateOnly MinSupportedDate = new(1999, 1, 4);

    private readonly IFrankfurterApiClient _apiClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExchangeRateManager> _logger;

    public ExchangeRateManager(
        IFrankfurterApiClient apiClient, 
        TimeProvider timeProvider, 
        ILogger<ExchangeRateManager> logger)
    {
        _apiClient = apiClient;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ExchangeRateModel> GetExchangeRateAsync(DateOnly date, CurrencyCode baseCurrency, CurrencyCode targetCurrency, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting exchange rate for {BaseCurrency}/{TargetCurrency} on {Date}", baseCurrency, targetCurrency, date);
        
        ValidateDate(date);

        if (baseCurrency == targetCurrency)
        {
            throw new ArgumentException("Base currency and target currency must be different.");
        }

        return await _apiClient.GetRateAsync(date, baseCurrency, targetCurrency, cancellationToken);
    }

    public async Task<ExchangeRatesListModel> GetExchangeRatesAsync(DateOnly date, CurrencyCode baseCurrency, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all exchange rates for base {BaseCurrency} on {Date}", baseCurrency, date);
        
        ValidateDate(date);

        return await _apiClient.GetRatesAsync(date, baseCurrency, cancellationToken);
    }

    private void ValidateDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        if (date > today)
        {
            throw new ArgumentException("Date cannot be in the future.");
        }

        if (date < MinSupportedDate)
        {
            throw new ArgumentException("Date cannot be before January 4, 1999.");
        }
    }
}
