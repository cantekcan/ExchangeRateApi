namespace ExchangeRate.Application.DTOs;

public class ExchangeRatesListResponse
{
    public string Date { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
