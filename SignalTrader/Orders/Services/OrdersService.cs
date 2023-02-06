using System.Collections.Concurrent;
using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Accounts.Services;
using SignalTrader.Common.Enums;
using SignalTrader.Common.Extensions;
using SignalTrader.Data;
using SignalTrader.Data.Entities;
using SignalTrader.Exchanges;
using SignalTrader.Orders.Notifications;
using SignalTrader.Telegram.Services;

namespace SignalTrader.Orders.Services;

public class OrdersService : IOrdersService
{
    #region Enums

    public enum OrderWaitType
    {
        Fill,
        Cancel
    }

    #endregion
    
    #region Members

    private readonly ILogger<OrdersService> _logger;
    private readonly IExchangeProvider _exchangeProvider;
    private readonly IAccountsService _accountsService;
    private readonly ITelegramService _telegramService;
    private readonly IConfiguration _configuration;
    private readonly IMediator _mediator;
    private readonly SignalTraderDbContext _signalTraderDbContext;
    private static SemaphoreSlim _updateOrderSemaphoreSlim = new SemaphoreSlim(1, 1);
    private static ConcurrentDictionary<string, string> _fillWarningsSent = new();

    #endregion

    #region Constructors

    public OrdersService(ILogger<OrdersService> logger, IExchangeProvider exchangeProvider, IAccountsService accountsService, ITelegramService telegramService, IConfiguration configuration, IMediator mediator, SignalTraderDbContext signalTraderDbContext)
    {
        _logger = logger;
        _exchangeProvider = exchangeProvider;
        _accountsService = accountsService;
        _telegramService = telegramService;
        _configuration = configuration;
        _mediator = mediator;
        _signalTraderDbContext = signalTraderDbContext;
    }

    #endregion

    #region IOrdersService

    public async Task CancelOrdersAsync(long? accountId, string? quoteAsset, string? baseAsset, Side? side)
    {
        Account? account = null;
        
        try
        {
            Guard.Against.Null(accountId, nameof(accountId));
            Guard.Against.NegativeOrZero(accountId.Value, nameof(accountId));
            Guard.Against.NullOrWhiteSpace(quoteAsset, nameof(quoteAsset));
            Guard.Against.NullOrWhiteSpace(baseAsset, nameof(baseAsset));
        
            _logger.LogInformation("CancelOrdersAsync({AccountId}, \"{QuoteAsset}\", \"{BaseAsset}\", {Side})", accountId, quoteAsset, baseAsset, side);

            // Get Account object.
            account = await _accountsService.GetAccountAsync(accountId.Value);
            if (account == null)
            {
                throw new ApplicationException($"Failed to get account {accountId.Value}");
            }

            // Get Exchange instance.
            var exchange = _exchangeProvider.GetExchange(account.Exchange);
            if (exchange == null)
            {
                throw new ApplicationException($"Failed to get exchange {account.Exchange}");
            }

            var activeOrders = await _signalTraderDbContext.Orders
                .Include(o => o.Account)
                .Include(o => o.Position)
                .Where(o => 
                    o.AccountId == account.Id &&
                    o.QuoteAsset == quoteAsset &&
                    o.BaseAsset == baseAsset &&
                    (side == null || (o.Side == side.Value)))
                .ToListAsync();

            foreach (var activeOrder in activeOrders)
            {
                _logger.LogInformation("Cancelling active order {Exchange}:{ExchangeOrderId}", account.Exchange, activeOrder.ExchangeOrderId);
                var result = await exchange.CancelOrderAsync(account, quoteAsset, baseAsset, activeOrder.ExchangeOrderId!);
                if (result.Success)
                {
                    // Update Order.
                    activeOrder.Status = OrderStatus.CancelInProgress;
                    activeOrder.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    await _signalTraderDbContext.SaveChangesAsync();
                    
                    // Wait a short while for order to actually cancel on exchange.
                    await WaitForOrderCompletionAsync(activeOrder, OrderWaitType.Cancel);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in CancelOrdersAsync");
            await _telegramService.SendMessageNotificationAsync(
                Telegram.Constants.Emojis.NameBadge,
                account?.Name,
                $"Failed to cancel active {baseAsset}{quoteAsset} {(side != null ? side.ToString() + " " : string.Empty)}orders on {account?.Exchange}",
                e.Message
            );
        }
    }

    public async Task UpdateOrdersAsync()
    {
        var activeOrders = await _signalTraderDbContext.Orders
            .Include(o => o.Account)
            .Include(o => o.Position)
            .Where(o => o.Status == OrderStatus.Created ||
                        o.Status == OrderStatus.PartiallyFilled ||
                        o.Status == OrderStatus.CancelInProgress)
            .ToListAsync();
        foreach (var activeOrder in activeOrders)
        {
            await UpdateOrderFromExchangeAsync(activeOrder);
        }
    }

    public async Task WaitForOrderCompletionAsync(Order order, OrderWaitType waitType)
    {
        var maxWaitTimeSeconds = _configuration.GetValue<int>("Orders:WaitForOrderCompletionTimeoutSeconds");
        var waitIntervalSeconds = _configuration.GetValue<int>("Orders:WaitForOrderCompletionIntervalSeconds");

        var startTime = DateTime.UtcNow;
        do
        {
            if (!order.IsComplete)
            {
                await Task.Delay(waitIntervalSeconds * 1000);
            }
            await UpdateOrderFromExchangeAsync(order);
        } while ((DateTime.UtcNow - startTime) < TimeSpan.FromSeconds(maxWaitTimeSeconds) && !order.IsComplete);

        if (!order.IsComplete)
        {
            _logger.LogWarning("Exceeded {Timeout}s timeout waiting for order {Exchange}:{ExchangeOrderId} to {WaitType}", maxWaitTimeSeconds, order.Exchange, order.ExchangeOrderId, waitType.ToString().ToLowerInvariant());
            await _telegramService.SendMessageNotificationAsync(Telegram.Constants.Emojis.Clock, 
                order.Account.Name,
                $"Exceeded {maxWaitTimeSeconds}s timeout waiting for order {order.Exchange}:{order.ExchangeOrderId} to {waitType.ToString().ToLowerInvariant()}");
        }
    }
    
    #endregion

    #region Private

    private async Task UpdateOrderFromExchangeAsync(Order order)
    {
        var statusChanged = false;
        await _updateOrderSemaphoreSlim.WaitAsync();
        try
        {
            // Get Account object.
            var account = order.Account;
            if (account == null)
            {
                throw new ApplicationException($"Failed to get account {order.AccountId}");
            }

            var exchange = _exchangeProvider.GetExchange(order.Exchange);
            if (exchange == null)
            {
                throw new ApplicationException($"Failed to get exchange {account.Exchange}");
            }

            await exchange.ProcessPendingUpdatesAsync(order.ExchangeOrderId!, order.IsComplete);
            // Refresh entities in memory with any changes in db.
            await _signalTraderDbContext.Entry(order).ReloadAsync();
            await _signalTraderDbContext.Entry(order.Position).ReloadAsync();
            await _signalTraderDbContext.Entry(order.Account).ReloadAsync();

            var result = await exchange.GetOrderInfoAsync(account, order.QuoteAsset, order.BaseAsset, order.ExchangeOrderId!);
            if (result.Success)
            {
                // Update Order from exchange.
                statusChanged = (order.Status != result.Status);
                if (statusChanged)
                {
                    order.Status = result.Status;
                    order.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    await _signalTraderDbContext.SaveChangesAsync();
                        
                    _logger.LogDebug("Publishing {Notification} from {Source}", "OrderStatusChangedNotification", "UpdateOrderFromExchangeAsync");
                    await _mediator.Publish(new OrderStatusChangedNotification { Order = order });
                }
            }
            
            // Send OrderFillWarning notification.
            long orderFillWarningThresholdMinutes = _configuration.GetValue<long>("Orders:OrderFillWarningThresholdMinutes");
            var throttleKey = $"{order.Exchange}:{order.ExchangeOrderId}";
            if ((order.Status == OrderStatus.Created || order.Status == OrderStatus.PartiallyFilled) &&
                (DateTime.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(order.CreatedUtcMillis).DateTime) > TimeSpan.FromMinutes(orderFillWarningThresholdMinutes) &&
                !_fillWarningsSent.ContainsKey(throttleKey))
            {
                await _telegramService.SendMessageNotificationAsync(
                    order.Position.Direction.ToEmoji(),
                    account.Name,
                    $"{Telegram.Constants.Emojis.Clock} {order.Type} {order.Side} order for {order.Quantity} {order.BaseAsset}{order.QuoteAsset} did not fill within {orderFillWarningThresholdMinutes} minutes");
                _fillWarningsSent.AddOrUpdate(throttleKey, throttleKey, (s, s1) => throttleKey);
            }

            if (order.IsComplete)
            {
                // No need to throttle fill warnings for this order any longer.
                _fillWarningsSent.TryRemove(new KeyValuePair<string, string>(throttleKey, throttleKey));
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in UpdateOrderFromExchangeAsync({Exchange}:{ExchangeOrderId})", order.Exchange, order.ExchangeOrderId);
        }
        finally
        {
            _updateOrderSemaphoreSlim.Release();
        }
    }

    #endregion
}
