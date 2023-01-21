using Bybit.Net.Clients;
using Bybit.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using SignalTrader.Accounts.Models;
using SignalTrader.Data.Entities;

namespace SignalTrader.Exchanges.Bybit;

public class BybitUsdtPerpetualExchange : IBybitUsdtPerpetualExchange
{
    #region Constants

    private const long ReceiveWindow = 30000;

    #endregion
    
    #region Members

    private readonly ILogger<BybitUsdtPerpetualExchange> _logger;
    private readonly IConfiguration _configuration;

    #endregion

    #region Constructors

    public BybitUsdtPerpetualExchange(ILogger<BybitUsdtPerpetualExchange> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    #endregion

    #region IBybitUsdtPerpetualExchange

    public async Task<Dictionary<string, AccountWalletBalance>?> GetAccountBalancesAsync(Account account)
    {
        try
        {
            var bybitClient = new BybitClient(BuildBybitClientOptions(account));
            var balancesResult = await bybitClient.UsdPerpetualApi.Account.GetBalancesAsync(receiveWindow:ReceiveWindow);
            if (balancesResult.Success)
            {
                _logger.LogInformation("Fetched account balances for account {AccountId}", account.Id);
                var result = new Dictionary<string, AccountWalletBalance>();
                foreach (var kv in balancesResult.Data)
                {
                    result[kv.Key] = new AccountWalletBalance(kv.Key, kv.Value.WalletBalance, kv.Value.AvailableBalance);
                }
                return result;
            }

            _logger.LogError("Failed to fetch account balances for account {AccountId}: {Error}", account.Id, balancesResult.Error!.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetAccountBalancesAsync");
        }

        return null;
    }

    #endregion

    #region Private

    BybitClientOptions BuildBybitClientOptions(Account account)
    {
        return new BybitClientOptions()
        {
            LogLevel = LogLevel.Trace,
            UsdPerpetualApiOptions = new RestApiClientOptions(_configuration["Exchanges:Bybit:ApiBase"])
            {
                ApiCredentials = new ApiCredentials(
                    account.ApiKey ?? string.Empty,
                    account.ApiSecret ?? String.Empty),
                AutoTimestamp = false,
                RequestTimeout = TimeSpan.FromSeconds(30),
            }
        };
    }

    #endregion
}
