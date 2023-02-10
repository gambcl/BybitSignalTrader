using System.Collections.Concurrent;
using Bybit.Net.Clients;
using Bybit.Net.Objects;
using Bybit.Net.Objects.Models;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using SignalTrader.Common.Enums;
using SignalTrader.Data.Entities;
using SignalTrader.Exchanges.Models;

namespace SignalTrader.Exchanges.Bybit;

public class BybitFuturesExchange : IBybitFuturesExchange
{
    #region Constants

    public const long ReceiveWindow = 30000;

    #endregion
    
    #region Members

    protected static readonly ConcurrentDictionary<string, BybitSymbol> _bybitSymbols = new();
    protected readonly ILogger _logger;
    protected readonly IConfiguration _configuration;

    #endregion

    #region Constructors

    public BybitFuturesExchange(ILogger logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    #endregion

    #region IBybitFuturesExchange

    public async Task<AccountInfoResult> GetAccountInfoAsync(Account account)
    {
        try
        {
            var bybitClient = new BybitClient(BuildBybitClientOptions(account, _configuration));
            var apiKeyInfoResult = await bybitClient.UsdPerpetualApi.Account.GetApiKeyInfoAsync(receiveWindow:ReceiveWindow);
            if (apiKeyInfoResult.Success && apiKeyInfoResult.Data.Any())
            {
                var apiKeyInfo = apiKeyInfoResult.Data.First();
                return new AccountInfoResult(true)
                {
                    ExchangeAccountId = apiKeyInfo.UserId.ToString(),
                    ExchangeType = ExchangeType.Futures
                };
            }

            return new AccountInfoResult(apiKeyInfoResult.Error!.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetAccountInfoAsync");
            return new AccountInfoResult(e.Message);
        }
    }

    public async Task<AccountBalancesResult> GetAccountBalancesAsync(Account account)
    {
        try
        {
            var bybitClient = new BybitClient(BuildBybitClientOptions(account, _configuration));
            var balancesResult = await bybitClient.UsdPerpetualApi.Account.GetBalancesAsync(receiveWindow:ReceiveWindow);
            if (balancesResult.Success)
            {
                _logger.LogInformation("Fetched account balances for account {AccountId}", account.Id);
                var result = new Dictionary<string, AccountWalletBalance>();
                foreach (var kv in balancesResult.Data)
                {
                    result[kv.Key] = new AccountWalletBalance(kv.Key, kv.Value.WalletBalance, kv.Value.AvailableBalance);
                }
                return new AccountBalancesResult(true)
                {
                    AccountWalletBalances = result
                };
            }

            _logger.LogError("Failed to fetch account balances for account {AccountId}: {Error}", account.Id, balancesResult.Error!.ToString());
            return new AccountBalancesResult(balancesResult.Error!.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetAccountBalancesAsync");
            return new AccountBalancesResult(e.Message);
        }
    }

    public async Task<Ticker?> GetTickerAsync(string quoteAsset, string baseAsset)
    {
        try
        {
            var symbol = $"{baseAsset}{quoteAsset}";
            var bybitClient = new BybitClient(BuildBybitClientOptions(_configuration));
            var tickerResult = await bybitClient.UsdPerpetualApi.ExchangeData.GetTickerAsync(symbol);
            if (tickerResult.Success)
            {
                _logger.LogInformation("Fetched ticker for symbol {Symbol}", symbol);

                var ticker = tickerResult.Data.FirstOrDefault();
                if (ticker != null)
                {
                    return new Ticker
                    (
                        SupportedExchange.BybitUSDTPerpetual,
                        quoteAsset,
                        baseAsset,
                        symbol,
                        ticker.BestBidPrice,
                        ticker.BestAskPrice,
                        ticker.LastPrice,
                        ticker.Turnover24H,
                        ticker.Volume24H
                    );
                }
            }

            _logger.LogError("Failed to fetch ticker for symbol {Symbol}: {Error}", symbol, tickerResult.Error!.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetTickerAsync");
        }

        return null;
    }

    public async Task UpdateSymbolInfoAsync()
    {
        try
        {
            var bybitClient = new BybitClient(BuildBybitClientOptions(_configuration));
            var symbolsResult = await bybitClient.UsdPerpetualApi.ExchangeData.GetSymbolsAsync();
            if (symbolsResult.Success)
            {
                _logger.LogInformation("Fetched Bybit symbols");
                foreach (var bybitSymbol in symbolsResult.Data)
                {
                    _bybitSymbols.AddOrUpdate(bybitSymbol.Name, bybitSymbol, (s, symbol) => bybitSymbol);
                }
            }
            else
            {
                _logger.LogError("Failed to fetch Bybit symbols: {Error}", symbolsResult.Error!.ToString());
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in UpdateSymbolInfoAsync");
        }
    }
    
    public static BybitClientOptions BuildBybitClientOptions(Account account, IConfiguration configuration)
    {
        return new BybitClientOptions
        {
            LogLevel = LogLevel.Trace,
            UsdPerpetualApiOptions = new RestApiClientOptions(configuration["Exchanges:Bybit:ApiBase"])
            {
                ApiCredentials = new ApiCredentials(
                    account.ApiKey ?? string.Empty,
                    account.ApiSecret ?? String.Empty),
                AutoTimestamp = false,
                RequestTimeout = TimeSpan.FromSeconds(30),
            }
        };
    }

    public static BybitClientOptions BuildBybitClientOptions(IConfiguration configuration)
    {
        return new BybitClientOptions
        {
            LogLevel = LogLevel.Trace,
            UsdPerpetualApiOptions = new RestApiClientOptions(configuration["Exchanges:Bybit:ApiBase"])
            {
                AutoTimestamp = false,
                RequestTimeout = TimeSpan.FromSeconds(30),
            }
        };
    }

    public static BybitSocketClientOptions BuildBybitSocketClientOptions(Account account, IConfiguration configuration)
    {
        return new BybitSocketClientOptions
        {
            LogLevel = LogLevel.Trace,
            ApiCredentials = new ApiCredentials(
                account.ApiKey ?? string.Empty,
                account.ApiSecret ?? String.Empty),
            UsdPerpetualStreamsOptions = new BybitSocketApiClientOptions
            {
                ApiCredentials = new ApiCredentials(
                    account.ApiKey ?? string.Empty,
                    account.ApiSecret ?? String.Empty),
                AutoReconnect = true,
                BaseAddress = configuration["Exchanges:Bybit:WebSocketBasePublic"],
                BaseAddressAuthenticated = configuration["Exchanges:Bybit:WebSocketBasePrivate"]
            }
        };
    }

    #endregion
}
