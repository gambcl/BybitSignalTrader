using MediatR;
using SignalTrader.Common.Enums;
using SignalTrader.Common.Extensions;
using SignalTrader.Positions.Services;
using SignalTrader.Telegram.Services;

namespace SignalTrader.Orders.Notifications;

public class OrderStatusChangedNotificationHandler : INotificationHandler<OrderStatusChangedNotification>
{
    #region Members

    private readonly ILogger<OrderStatusChangedNotificationHandler> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion

    #region Constructors

    public OrderStatusChangedNotificationHandler(ILogger<OrderStatusChangedNotificationHandler> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion

    #region INotificationHandler<OrderStatusChangedNotification>

    public async Task Handle(OrderStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling OrderStatusChangedNotification");
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var positionsService = scope.ServiceProvider.GetRequiredService<IPositionsService>();
            var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            
            var order = notification.Order;
            
            if (order.Status == OrderStatus.Rejected)
            {
                await telegramService.SendMessageNotificationAsync(
                    order.Position.Direction.ToEmoji(),
                    order.Account.Name,
                    $"{order.Type} {order.Side} order for {order.BaseAsset}{order.QuoteAsset} rejected by exchange",
                    null,
                    Telegram.Constants.Emojis.Prohibited);
            }
            else if (order.Status == OrderStatus.Cancelled)
            {
                await telegramService.SendMessageNotificationAsync(
                    order.Position.Direction.ToEmoji(),
                    order.Account.Name,
                    $"{order.Type} {order.Side} order for {order.Quantity} {order.BaseAsset}{order.QuoteAsset} cancelled");
            }
            else if (order.Status == OrderStatus.CancelledPartiallyFilled)
            {
                var filledPercent = order.QuantityFilled / order.Quantity;
                await telegramService.SendMessageNotificationAsync(
                    order.Position.Direction.ToEmoji(),
                    order.Account.Name,
                    $"{order.Type} {order.Side} order for {order.Quantity} {order.BaseAsset}{order.QuoteAsset} cancelled with {filledPercent:P1} filled");
            }
            else if (order.Status == OrderStatus.Filled)
            {
                await telegramService.SendMessageNotificationAsync(
                    order.Position.Direction.ToEmoji(),
                    order.Account.Name,
                    $"{order.Type} {order.Side} order for {order.Quantity} {order.BaseAsset}{order.QuoteAsset} filled");
            }

            // Update Position now that Order has been updated.
            await positionsService.UpdatePositionAsync(order.Position);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in Handle<OrderStatusChangedNotification>");
        }
    }

    #endregion
}
