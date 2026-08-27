namespace ExchangeRate.Infrastructure.Configuration;

public class FrankfurterOptions
{
    public const string SectionName = "Frankfurter";
    public string BaseUrl { get; set; } = "https://api.frankfurter.dev";
}
