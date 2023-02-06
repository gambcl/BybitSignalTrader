using SignalTrader.Data.Entities;
using SignalTrader.Exchanges.Models;

namespace SignalTrader.Exchanges.Bybit;

public interface IBybitUsdtPerpetualExchangeListener
{
    public Task<ExchangeSubscriptionResult> SubscribeToUserTradeUpdatesAsync(Account account);
    public Task ProcessUserTradeUpdatesAsync(string exchangeOrderId, bool orderComplete = false);
    
    public Task<ExchangeSubscriptionResult> SubscribeToStopOrderUpdatesAsync(Account account);
    
    public Task<ExchangeSubscriptionResult> SubscribeToPositionUpdatesAsync(Account account);
    
    public Task<ExchangeSubscriptionResult> SubscribeToOrderUpdatesAsync(Account account);
}
