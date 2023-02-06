using SignalTrader.Accounts.Resources;
using SignalTrader.Data.Entities;
using SignalTrader.Exchanges.Models;

namespace SignalTrader.Accounts.Services;

public interface IAccountsService
{
    public Task<Account> CreateAccountAsync(CreateAccountResource resource);
    public Task<List<Account>> GetAccountsAsync();
    public List<Account> GetAccounts();
    public Task<Account?> GetAccountAsync(long accountId);
    public Task<Account> UpdateAccountAsync(UpdateAccountResource resource);
    public Task<bool> DeleteAccountAsync(long accountId);
    public Task UpdateAccountsAsync();
    public Task UpdateAccountBalancesAsync(Account account);
    public Task<Dictionary<long, Dictionary<string,AccountWalletBalance>>> GetBalancesAsync();
    public Dictionary<string,AccountWalletBalance> GetBalances(long accountId);
}
