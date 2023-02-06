namespace SignalTrader.Exchanges.Models;

public class AccountBalancesResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public Dictionary<string, AccountWalletBalance> AccountWalletBalances { get; set; } = new();
}
