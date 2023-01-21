using SignalTrader.Common.Enums;

namespace SignalTrader.Accounts.Resources;

public class CreateAccountResource
{
    public SupportedExchange Exchange { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Comment { get; set; }
    public AccountType AccountType { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? ApiPassphrase { get; set; }
}
