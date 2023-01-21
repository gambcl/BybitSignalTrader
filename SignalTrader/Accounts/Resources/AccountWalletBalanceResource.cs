namespace SignalTrader.Accounts.Resources;

public class AccountWalletBalanceResource
{
    public string Asset { get; set; } = null!;
    public decimal WalletAmount { get; set; }
    public decimal AvailableAmount { get; set; }
}
