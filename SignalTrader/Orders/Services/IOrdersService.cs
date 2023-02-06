using SignalTrader.Common.Enums;
using SignalTrader.Data.Entities;

namespace SignalTrader.Orders.Services;

public interface IOrdersService
{
    public Task CancelOrdersAsync(long? accountId, string? quoteAsset, string? baseAsset, Side? side);
    public Task UpdateOrdersAsync();
    public Task WaitForOrderCompletionAsync(Order order, OrdersService.OrderWaitType waitType);
}