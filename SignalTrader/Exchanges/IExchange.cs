using SignalTrader.Common.Enums;
using SignalTrader.Data.Entities;
using SignalTrader.Exchanges.Models;
using SignalTrader.Positions.Models;

namespace SignalTrader.Exchanges;

public interface IExchange
{
    public Task<AccountInfoResult> GetAccountInfoAsync(Account account);
    public Task<AccountBalancesResult> GetAccountBalancesAsync(Account account);
    public Task<Ticker?> GetTickerAsync(string quoteAsset, string baseAsset);
    public Task<OrderResult> PlaceOrderAsync(Account account, string quoteAsset, string baseAsset, Side side, OrderType orderType, decimal? price, decimal quantity, decimal? stopLoss, decimal? leverageMultiplier, LeverageType? leverageType, bool closing);
    public Task<PositionInfoResult> GetPositionInfoAsync(Account account, string quoteAsset, string baseAsset);
    public Task<ExchangeResult> CancelOrderAsync(Account account, string quoteAsset, string baseAsset, string orderId);
    public Task<OrderResult> GetOrderInfoAsync(Account account, string quoteAsset, string baseAsset, string orderId);
    public Task<ExchangeSubscriptionResult> SubscribeToUpdatesAsync(Account account);
    public Task ProcessPendingUpdatesAsync(string orderId, bool isComplete = false);
}
