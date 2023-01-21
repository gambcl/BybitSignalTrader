using System.Collections.Concurrent;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Accounts.Models;
using SignalTrader.Accounts.Resources;
using SignalTrader.Data;
using SignalTrader.Data.Entities;
using SignalTrader.Exchanges;

namespace SignalTrader.Accounts.Services;

public class AccountsService : IAccountsService
{
    #region Members

    private static readonly ConcurrentDictionary<long, Dictionary<string, AccountWalletBalance>> AccountBalances = new ();
    private readonly ILogger<AccountsService> _logger;
    private readonly SignalTraderDbContext _signalTraderDbContext;
    private readonly IExchangeProvider _exchangeProvider;

    #endregion

    #region Constructors

    public AccountsService(ILogger<AccountsService> logger, SignalTraderDbContext signalTraderDbContext, IExchangeProvider exchangeProvider)
    {
        _logger = logger;
        _signalTraderDbContext = signalTraderDbContext;
        _exchangeProvider = exchangeProvider;
    }

    #endregion

    #region IAccountsService

    public async Task<Account> CreateAccountAsync(CreateAccountResource resource)
    {
        Guard.Against.Null(resource, nameof(resource));
        Guard.Against.NullOrWhiteSpace(resource.Name, "Name");
        Guard.Against.InvalidFormat(resource.Name, "Name", @"[a-zA-Z_][a-zA-Z0-9_]*", "Name may only contain letters, numbers and underscores");
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
        return await _signalTraderDbContext.Accounts.OrderBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Account?> GetAccountAsync(long accountId)
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
        Guard.Against.InvalidFormat(resource.Name, "Name", @"[a-zA-Z_][a-zA-Z0-9_]*", "Name may only contain letters, numbers and underscores");
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

    public Task<bool> DeleteAccountAsync(long accountId)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateBalances()
    {
        var allIds = await _signalTraderDbContext.Accounts.Select(a => a.Id).ToListAsync();
        foreach (var id in allIds)
        {
            await UpdateBalances(id);
        }
    }

    public async Task UpdateBalances(long accountId)
    {
        _logger.LogInformation("Updating balances for Account {AccountId}", accountId);
        var account = await _signalTraderDbContext.Accounts.FindAsync(accountId);
        if (account != null)
        {
            var exchange = _exchangeProvider.GetExchange(account.Exchange);
            if (exchange != null)
            {
                var balances = await exchange.GetAccountBalancesAsync(account);
                if (balances != null)
                {
                    if (AccountBalances.TryGetValue(accountId, out var oldBalances))
                    {
                        AccountBalances.TryUpdate(accountId, balances, oldBalances);
                    }
                    else
                    {
                        AccountBalances.TryAdd(accountId, balances);
                    }
                }
            }
        }
    }

    public async Task<Dictionary<long, Dictionary<string, AccountWalletBalance>>> GetBalancesAsync()
    {
        Dictionary<long, Dictionary<string, AccountWalletBalance>> result = new ();

        var allIds = await _signalTraderDbContext.Accounts.Select(a => a.Id).ToListAsync();
        foreach (var id in allIds)
        {
            var balances = GetBalances(id);
            result[id] = balances;
        }

        return result;
    }

    public Dictionary<string, AccountWalletBalance> GetBalances(long accountId)
    {
        Dictionary<string, AccountWalletBalance>? result;
        AccountBalances.TryGetValue(accountId, out result);
        return result ?? new Dictionary<string, AccountWalletBalance>();
    }

    #endregion
}
