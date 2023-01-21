using SignalTrader.Accounts.Models;
using SignalTrader.Accounts.Resources;
using SignalTrader.Data.Entities;

namespace SignalTrader.Accounts.Services;

public interface IAccountsService
{
    public Task<Account> CreateAccountAsync(CreateAccountResource resource);
    public Task<List<Account>> GetAccountsAsync();
    public Task<Account?> GetAccountAsync(long accountId);
    public Task<Account> UpdateAccountAsync(UpdateAccountResource resource);
    public Task<bool> DeleteAccountAsync(long accountId);
    public Task UpdateBalances();
    public Task UpdateBalances(long accountId);
    public Task<Dictionary<long, Dictionary<string,AccountWalletBalance>>> GetBalancesAsync();
    public Dictionary<string,AccountWalletBalance> GetBalances(long accountId);
}
