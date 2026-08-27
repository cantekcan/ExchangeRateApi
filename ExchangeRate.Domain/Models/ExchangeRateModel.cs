namespace ExchangeRate.Domain.Models;

public class ExchangeRateModel
{
    public string Date { get; set; } = string.Empty;
    public string Base { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}
