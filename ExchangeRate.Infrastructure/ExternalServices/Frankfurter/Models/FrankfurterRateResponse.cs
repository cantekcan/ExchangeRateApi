using System.Text.Json.Serialization;

namespace ExchangeRate.Infrastructure.ExternalServices.Frankfurter.Models;

public class FrankfurterRateResponse
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("base")]
    public string Base { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
