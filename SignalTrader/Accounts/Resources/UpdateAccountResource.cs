namespace SignalTrader.Accounts.Resources;

public class UpdateAccountResource
{
    public long Id { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Comment { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? ApiPassphrase { get; set; }
}
