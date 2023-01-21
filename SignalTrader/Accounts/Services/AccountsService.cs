using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Accounts.Resources;
using SignalTrader.Data;
using SignalTrader.Data.Entities;

namespace SignalTrader.Accounts.Services;

public class AccountsService : IAccountsService
{
    #region Members

    private readonly ILogger<AccountsService> _logger;
    private readonly SignalTraderDbContext _signalTraderDbContext;

    #endregion

    #region Constructors

    public AccountsService(ILogger<AccountsService> logger, SignalTraderDbContext signalTraderDbContext)
    {
        _logger = logger;
        _signalTraderDbContext = signalTraderDbContext;
    }

    #endregion

    #region IAccountsService

    public async Task<Account> CreateAccountAsync(CreateAccountResource resource)
    {
        Guard.Against.Null(resource, nameof(resource));
        Guard.Against.NullOrWhiteSpace(resource.Name, "Name");
        Guard.Against.NullOrWhiteSpace(resource.QuoteAsset, "QuoteAsset");

        // Fetch all accounts, to force encrypted fields to be decrypted.
        var allAccounts = await _signalTraderDbContext.Accounts.ToListAsync();

        // Check that no other accounts exist with the same Name.
        var accountByName = allAccounts.Find(ea => ea.Name == resource.Name);
        if (accountByName != null)
        {
            throw new ArgumentException($"Account already exists with Name '{resource.Name}'");
        }
        
        // Check that no other accounts exist with the same Exchange+ApiKey combination.
        var accountByApiKey = allAccounts.Find(ea => (ea.Exchange == resource.Exchange) && (ea.ApiKey == resource.ApiKey));
        if (accountByApiKey != null)
        {
            throw new ArgumentException($"Account already exists for {resource.Exchange} with ApiKey '{resource.ApiKey}'");
        }

        // Looks good, create Account.
        var account = new Account
        {
            Name = resource.Name,
            Comment = resource.Comment,
            Exchange = resource.Exchange,
            QuoteAsset = resource.QuoteAsset,
            AccountType = resource.AccountType,
            ApiKey = resource.ApiKey,
            ApiSecret = resource.ApiSecret,
            ApiPassphrase = resource.ApiPassphrase
        };
        
        _signalTraderDbContext.Accounts.Add(account);
        await _signalTraderDbContext.SaveChangesAsync();
        _logger.LogInformation("Added Account {Id}", account.Id);
        return account;
    }

    public async Task<List<Account>> GetAccountsAsync()
    {
        return await _signalTraderDbContext.Accounts
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Account?> GetAccountAsync(int accountId)
    {
        Guard.Against.NegativeOrZero(accountId, nameof(accountId));
        
        return await _signalTraderDbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(ea => ea.Id == accountId);
    }

    public async Task<Account> UpdateAccountAsync(UpdateAccountResource resource)
    {
        Guard.Against.Null(resource, nameof(resource));
        Guard.Against.NegativeOrZero(resource.Id, "Id");
        Guard.Against.NullOrWhiteSpace(resource.Name, "Name");
        Guard.Against.NullOrWhiteSpace(resource.QuoteAsset, "QuoteAsset");

        // Find existing Account with the given Id.
        var account = await _signalTraderDbContext.Accounts.FindAsync(resource.Id);
        if (account == null)
        {
            throw new ArgumentException($"Account {resource.Id} not found");
        }
        
        // Fetch all accounts, to force encrypted fields to be decrypted.
        var allAccounts = await _signalTraderDbContext.Accounts.ToListAsync();

        // Check that no other accounts exist with the same Name.
        var accountByName = allAccounts.Find(ea => ea.Name == resource.Name);
        if (accountByName != null && accountByName.Id != account.Id)
        {
            throw new ArgumentException($"Account already exists with Name '{resource.Name}'");
        }
        
        // Check that no other accounts exist with the same Exchange+ApiKey combination.
        var accountByApiKey = allAccounts.Find(ea => (ea.Exchange == account.Exchange) && (ea.ApiKey == resource.ApiKey));
        if (accountByApiKey != null && accountByApiKey.Id != account.Id)
        {
            throw new ArgumentException($"Account already exists for {account.Exchange} with ApiKey '{resource.ApiKey}'");
        }

        // Looks good, update Account.
        account.Name = resource.Name;
        account.Comment = resource.Comment;
        account.QuoteAsset = resource.QuoteAsset;
        account.ApiKey = resource.ApiKey;
        account.ApiSecret = resource.ApiSecret;
        account.ApiPassphrase = resource.ApiPassphrase;
        account.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        await _signalTraderDbContext.SaveChangesAsync();
        _logger.LogInformation("Updated Account {Id}", account.Id);
        
        // Return updated Account.
        var updatedAccount = await _signalTraderDbContext.Accounts.FindAsync(resource.Id);
        if (updatedAccount == null)
        {
            throw new ArgumentException($"Updated Account {resource.Id} not found");
        }
        return updatedAccount;
    }

    public Task<bool> DeleteAccountAsync(int accountId)
    {
        throw new NotImplementedException();
    }

    #endregion
}
