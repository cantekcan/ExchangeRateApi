namespace ExchangeRate.Domain.Models;

public class ExchangeRatesListModel
{
    public string Date { get; set; } = string.Empty;
    public string Base { get; set; } = string.Empty;
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
