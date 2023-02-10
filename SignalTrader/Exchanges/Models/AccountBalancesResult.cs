using SignalTrader.Common.Models;

namespace SignalTrader.Exchanges.Models;

public class AccountBalancesResult : ServiceResult
{
    public AccountBalancesResult(bool success) : base(success)
    {
    }

    public AccountBalancesResult(string message) : base(message)
    {
    }

    public Dictionary<string, AccountWalletBalance> AccountWalletBalances { get; set; } = new();
}
