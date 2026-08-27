using ExchangeRate.Application.Abstractions;
using ExchangeRate.Domain.Enums;
using ExchangeRate.Domain.Models;
using ExchangeRate.Infrastructure.Configuration;
using ExchangeRate.Infrastructure.ExternalServices.Frankfurter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace ExchangeRate.Infrastructure.ExternalServices.Frankfurter;

public class FrankfurterApiClient : IFrankfurterApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterApiClient> _logger;

    public FrankfurterApiClient(HttpClient httpClient, IOptions<FrankfurterOptions> options, ILogger<FrankfurterApiClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        _logger = logger;
    }

    public async Task<ExchangeRateModel> GetRateAsync(DateOnly date, CurrencyCode baseCurrency, CurrencyCode targetCurrency, CancellationToken cancellationToken)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var baseStr = baseCurrency.ToString();
        var targetStr = targetCurrency.ToString();
        
        var url = $"/v1/{dateStr}?from={baseStr}&to={targetStr}";
        _logger.LogInformation("Requesting Frankfurter API: {Url}", url);

        try
        {
            var response = await _httpClient.GetFromJsonAsync<FrankfurterRateResponse>(url, cancellationToken);

            if (response == null || !response.Rates.ContainsKey(targetStr))
            {
                throw new HttpRequestException("Rate not found in response.");
            }

            return new ExchangeRateModel
            {
                Date = response.Date,
                Base = response.Base,
                Target = targetStr,
                Rate = response.Rates[targetStr]
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Frankfurter API for single rate: {Url}", url);
            throw;
        }
    }

    public async Task<ExchangeRatesListModel> GetRatesAsync(DateOnly date, CurrencyCode baseCurrency, CancellationToken cancellationToken)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var baseStr = baseCurrency.ToString();
        
        var url = $"/v1/{dateStr}?from={baseStr}";
        _logger.LogInformation("Requesting Frankfurter API: {Url}", url);

        try
        {
            var response = await _httpClient.GetFromJsonAsync<FrankfurterRateResponse>(url, cancellationToken);

            if (response == null)
            {
                throw new HttpRequestException("Response was empty.");
            }

            return new ExchangeRatesListModel
            {
                Date = response.Date,
                Base = response.Base,
                Rates = response.Rates
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Frankfurter API for rates list: {Url}", url);
            throw;
        }
    }
}
