using SignalTrader.Accounts.Models;
using SignalTrader.Data.Entities;

namespace SignalTrader.Exchanges;

public interface IExchange
{
    public Task<Dictionary<string, AccountWalletBalance>?> GetAccountBalancesAsync(Account account);
}
