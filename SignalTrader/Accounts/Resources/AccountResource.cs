using SignalTrader.Common.Enums;

namespace SignalTrader.Accounts.Resources;

public class AccountResource
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Comment { get; set; }
    public SupportedExchange Exchange { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public AccountType AccountType { get; set; }
    public string? ApiKey { get; set; }
    public long? CreatedUtcMillis { get; set; }
    public long? UpdatedUtcMillis { get; set; }
}
