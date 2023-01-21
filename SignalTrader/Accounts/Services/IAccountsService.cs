using SignalTrader.Accounts.Resources;
using SignalTrader.Data.Entities;

namespace SignalTrader.Accounts.Services;

public interface IAccountsService
{
    public Task<Account> CreateAccountAsync(CreateAccountResource resource);
    public Task<List<Account>> GetAccountsAsync();
    public Task<Account?> GetAccountAsync(int accountId);
    public Task<Account> UpdateAccountAsync(UpdateAccountResource resource);
    public Task<bool> DeleteAccountAsync(int accountId);
}
