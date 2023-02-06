using SignalTrader.Data.Entities;
using SignalTrader.Exchanges.Models;

namespace SignalTrader.Exchanges.Bybit;

public interface IBybitFuturesExchange
{
    public Task<AccountInfoResult> GetAccountInfoAsync(Account account);
    public Task<AccountBalancesResult> GetAccountBalancesAsync(Account account);
    public Task<Ticker?> GetTickerAsync(string quoteAsset, string baseAsset);
    public Task UpdateSymbolInfoAsync();
}
