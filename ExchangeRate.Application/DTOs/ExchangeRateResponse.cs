namespace ExchangeRate.Application.DTOs;

public class ExchangeRateResponse
{
    public string Date { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}
