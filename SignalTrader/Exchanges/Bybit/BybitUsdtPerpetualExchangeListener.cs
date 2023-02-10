using System.Collections.Concurrent;
using System.Text.Json;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.Socket;
using CryptoExchange.Net.Sockets;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Common.Enums;
using SignalTrader.Data;
using SignalTrader.Data.Entities;
using SignalTrader.Exchanges.Models;
using SignalTrader.Orders.Notifications;
using OrderStatus = SignalTrader.Common.Enums.OrderStatus;
using OrderType = Bybit.Net.Enums.OrderType;
using PositionStatus = Bybit.Net.Enums.PositionStatus;

namespace SignalTrader.Exchanges.Bybit;

public class BybitUsdtPerpetualExchangeListener : IBybitUsdtPerpetualExchangeListener, IDisposable
{
    #region Members

    private readonly ILogger<BybitUsdtPerpetualExchangeListener> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMediator _mediator;
    private CancellationTokenSource? _cancellationTokenSource = new();
    private readonly ConcurrentDictionary<long, UpdateSubscription> _userTradeUpdateSubscriptions = new();
    private readonly ConcurrentDictionary<long, UpdateSubscription> _stopOrderUpdateSubscriptions = new();
    private readonly ConcurrentDictionary<long, UpdateSubscription> _positionUpdateSubscriptions = new();
    private readonly ConcurrentDictionary<long, UpdateSubscription> _orderUpdateSubscriptions = new();
    private readonly ConcurrentDictionary<string, Queue<BybitUserTradeUpdate>> _userTradeUpdates = new();

    #endregion

    #region Constructors

    public BybitUsdtPerpetualExchangeListener(ILogger<BybitUsdtPerpetualExchangeListener> logger, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, IMediator mediator)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
        _mediator = mediator;
    }
    
    #endregion

    #region IDisposable

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    #endregion

    #region IBybitUsdtPerpetualExchangeListener

    public async Task<ExchangeSubscriptionResult> SubscribeToUserTradeUpdatesAsync(Account account)
    {
        try
        {
            if (_userTradeUpdateSubscriptions.ContainsKey(account.Id))
            {
                return new ExchangeSubscriptionResult(true);
            }
            
            _logger.LogInformation("Subscribing to UserTradeUpdates for account {AccountId}", account.Id);
            var bybitSocketClient = new BybitSocketClient(BybitFuturesExchange.BuildBybitSocketClientOptions(account, _configuration));
            var subscriptionResult = await bybitSocketClient.UsdPerpetualStreams.SubscribeToUserTradeUpdatesAsync(async @event =>
            {
                foreach (var userTradeUpdate in @event.Data)
                {
                    try
                    {
                        _logger.LogDebug("Received UserTradeUpdate:\n{UserTradeUpdate}", JsonSerializer.Serialize(userTradeUpdate).ToString());
                    
                        // STEP 1: Add update to our cache
                        _userTradeUpdates.AddOrUpdate(userTradeUpdate.OrderId, 
                            new Queue<BybitUserTradeUpdate>(new [] { userTradeUpdate }), 
                            (s, queue) =>
                            {
                                queue.Enqueue(userTradeUpdate);
                                return queue;
                            });
                    
                        // STEP 2: Process cached updates if Order can be found in db.
                        await ProcessUserTradeUpdatesAsync(userTradeUpdate.OrderId);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Caught Exception in UserTradeUpdates handler");
                    }
                }
            }, _cancellationTokenSource!.Token);
            if (subscriptionResult.Success)
            {
                _logger.LogInformation("Subscribed to UserTradeUpdates for account {AccountId}", account.Id);
                _userTradeUpdateSubscriptions.AddOrUpdate(account.Id, subscriptionResult.Data, (l, subscription) => subscriptionResult.Data);
                return new ExchangeSubscriptionResult(true);
            }

            _logger.LogInformation("Failed to subscribe to UserTradeUpdates for account {AccountId}: {Error}", account.Id, subscriptionResult.Error!.ToString());
            return new ExchangeSubscriptionResult(subscriptionResult.Error!.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in SubscribeToUserTradeUpdatesAsync");
            return new ExchangeSubscriptionResult(e.Message);
        }
    }

    public async Task ProcessUserTradeUpdatesAsync(string exchangeOrderId, bool orderComplete = false)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateAsyncScope();
            var signalTraderDbContext = scope.ServiceProvider.GetRequiredService<SignalTraderDbContext>();

            if (_userTradeUpdates.TryGetValue(exchangeOrderId, out var userTradeUpdates))
            {
                // Get Order by ExchangeOrderId.
                var orders = await signalTraderDbContext.Orders
                    .Include(o => o.Account)
                    .Include(o => o.Position)
                    .Where(o => o.Exchange == SupportedExchange.BybitUSDTPerpetual && o.ExchangeOrderId == exchangeOrderId)
                    .ToListAsync();
        
                // Should only be a single matching order.
                var order = orders.SingleOrDefault();
                if (order != null)
                {
                    OrderStatus previousStatus = order.Status;
                    
                    // Update QuantityFilled, Price.
                    decimal totalQuantityFilled = 0.0M;
                    decimal totalCost = 0.0M;
                    decimal quantityRemaining = Decimal.MaxValue;
                    int numUpdatesProcessed = 0;

                    while (userTradeUpdates.Count > 0)
                    {
                        var update = userTradeUpdates.Dequeue();
                        totalQuantityFilled += update.Quantity;
                        totalCost += (update.Quantity * update.Price);
                        quantityRemaining = Math.Min(quantityRemaining, update.QuantityRemaining);
                        numUpdatesProcessed++;
                    }

                    if (numUpdatesProcessed > 0)
                    {
                        if (totalQuantityFilled > 0.0M)
                        {
                            order.QuantityFilled = totalQuantityFilled;
                            order.Price = totalCost / totalQuantityFilled;
                            if (quantityRemaining > 0.0M)
                            {
                                order.Status = OrderStatus.PartiallyFilled;
                            }
                            else
                            {
                                order.Status = OrderStatus.Filled;
                            }
                        }
                    
                        order.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        await signalTraderDbContext.SaveChangesAsync();

                        if (order.Status != previousStatus)
                        {
                            _logger.LogDebug("Publishing {Notification} from {Source}", "OrderStatusChangedNotification", "BybitUsdtPerpetualExchangeListener");
                            await _mediator.Publish(new OrderStatusChangedNotification { Order = order });
                        }

                        // Remove updates from cache if trade is fully filled or order is complete (maybe cancelled and partially filled).
                        if (quantityRemaining <= 0.0M || orderComplete)
                        {
                            _userTradeUpdates.TryRemove(exchangeOrderId, out _);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in ProcessUserTradeUpdatesAsync");
        }
    }

    public async Task<ExchangeSubscriptionResult> SubscribeToStopOrderUpdatesAsync(Account account)
    {
        try
        {
            if (_stopOrderUpdateSubscriptions.ContainsKey(account.Id))
            {
                return new ExchangeSubscriptionResult(true);
            }

            var accountId = account.Id;
            _logger.LogInformation("Subscribing to StopOrderUpdates for account {AccountId}", account.Id);
            var bybitSocketClient = new BybitSocketClient(BybitFuturesExchange.BuildBybitSocketClientOptions(account, _configuration));
            var subscriptionResult = await bybitSocketClient.UsdPerpetualStreams.SubscribeToStopOrderUpdatesAsync(async @event =>
            {
                // Received an StopOrderUpdate.
                using var scope = _serviceScopeFactory.CreateAsyncScope();
                var signalTraderDbContext = scope.ServiceProvider.GetRequiredService<SignalTraderDbContext>();

                foreach (var stopOrderUpdate in @event.Data)
                {
                    try
                    {
                        _logger.LogDebug("Received StopOrderUpdate:\n{StopOrderUpdate}", JsonSerializer.Serialize(stopOrderUpdate).ToString());

                        // When StopLoss has been triggered add the exit order to Position, change Position status to StopLossInProgress.
                        if (stopOrderUpdate.Status == StopOrderStatus.Triggered)
                        {
                            _logger.LogWarning("StopLoss triggered for {Exchange}:{Symbol}", SupportedExchange.BybitUSDTPerpetual, stopOrderUpdate.Symbol);
                            // Get the Position.
                            var positions = await signalTraderDbContext.Positions
                                .Include(p => p.Account)
                                .Include(p => p.Orders)
                                .Where(p => p.AccountId == accountId &&
                                            p.Exchange == SupportedExchange.BybitUSDTPerpetual &&
                                            (p.BaseAsset + p.QuoteAsset) == stopOrderUpdate.Symbol &&
                                            p.Quantity == stopOrderUpdate.Quantity &&
                                            // We need a Position in the opposite direction to the StopLoss order.
                                            p.Direction == (stopOrderUpdate.Side == OrderSide.Buy ? Direction.Short : Direction.Long) &&
                                            (p.Status == Common.Enums.PositionStatus.Created || p.Status == Common.Enums.PositionStatus.Open))
                                .ToListAsync();
                            if (positions.Count != 1)
                            {
                                _logger.LogError("Ambiguous or no result {Count} when querying for Position matching StopOrderUpdate", positions.Count);
                                return;
                            }

                            var position = positions.Single();

                            var scopedAccount = await signalTraderDbContext.Accounts.FindAsync(accountId);
                            if (scopedAccount == null)
                            {
                                _logger.LogError("Failed to get Account {Id} while processing StopOrderUpdate", accountId);
                                return;
                            }

                            var stopLossOrder = new Order()
                            {
                                AccountId = accountId,
                                Account = scopedAccount,
                                Exchange = position.Exchange,
                                ExchangeOrderId = stopOrderUpdate.Id,
                                QuoteAsset = position.QuoteAsset,
                                BaseAsset = position.BaseAsset,
                                Side = stopOrderUpdate.Side switch
                                {
                                    OrderSide.Buy => Side.Buy,
                                    OrderSide.Sell => Side.Sell,
                                    _ => throw new ArgumentOutOfRangeException()
                                },
                                Type = stopOrderUpdate.Type switch
                                {
                                    OrderType.Limit => Common.Enums.OrderType.Limit,
                                    OrderType.Market => Common.Enums.OrderType.Market,
                                    _ => throw new ArgumentOutOfRangeException()
                                },
                                Price = stopOrderUpdate.Type == OrderType.Limit ? stopOrderUpdate.Price : null,
                                Quantity = stopOrderUpdate.Quantity,
                                ReduceOnly = stopOrderUpdate.ReduceOnly,
                                PositionId = position.Id,
                                Position = position
                            };
                            position.Orders.Add(stopLossOrder);
                            position.Status = Common.Enums.PositionStatus.StopLossInProgress;
                            position.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            await signalTraderDbContext.SaveChangesAsync();
                            _logger.LogInformation("Changed Position {Exchange}:{Base}{Quote} status to {PositionStatus}", position.Exchange, position.BaseAsset, position.QuoteAsset, position.Status);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Caught Exception in StopOrderUpdates handler");
                    }
                }
            }, _cancellationTokenSource!.Token);
            if (subscriptionResult.Success)
            {
                _logger.LogInformation("Subscribed to StopOrderUpdates for account {AccountId}", account.Id);
                _stopOrderUpdateSubscriptions.AddOrUpdate(account.Id, subscriptionResult.Data, (l, subscription) => subscriptionResult.Data);
                return new ExchangeSubscriptionResult(true);
            }

            _logger.LogInformation("Failed to subscribe to StopOrderUpdates for account {AccountId}: {Error}", account.Id, subscriptionResult.Error!.ToString());
            return new ExchangeSubscriptionResult(subscriptionResult.Error!.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in SubscribeToStopOrderUpdatesAsync");
            return new ExchangeSubscriptionResult(e.Message);
        }
    }

    public async Task<ExchangeSubscriptionResult> SubscribeToPositionUpdatesAsync(Account account)
    {
        try
        {
            if (_positionUpdateSubscriptions.ContainsKey(account.Id))
            {
                return new ExchangeSubscriptionResult(true);
            }

            var accountId = account.Id;
            _logger.LogInformation("Subscribing to PositionUpdates for account {AccountId}", account.Id);
            var bybitSocketClient = new BybitSocketClient(BybitFuturesExchange.BuildBybitSocketClientOptions(account, _configuration));
            var subscriptionResult = await bybitSocketClient.UsdPerpetualStreams.SubscribeToPositionUpdatesAsync(async @event =>
            {
                using var scope = _serviceScopeFactory.CreateAsyncScope();
                var signalTraderDbContext = scope.ServiceProvider.GetRequiredService<SignalTraderDbContext>();
                
                foreach (var positionUsdPerpetualUpdate in @event.Data)
                {
                    try
                    {
                        _logger.LogDebug("Received PositionUpdate:\n{PositionUpdate}", JsonSerializer.Serialize(positionUsdPerpetualUpdate).ToString());
                        
                        // Skip updates indicating a flat position on exchange
                        if (positionUsdPerpetualUpdate.Side == PositionSide.None)
                        {
                            _logger.LogDebug("Ignoring PositionUpdate for flat position");
                            continue;
                        }

                        // Get the Position.
                        var positions = await signalTraderDbContext.Positions
                            .Include(p => p.Account)
                            .Include(p => p.Orders)
                            .Where(p => p.AccountId == accountId &&
                                        p.Exchange == SupportedExchange.BybitUSDTPerpetual &&
                                        (p.BaseAsset + p.QuoteAsset) == positionUsdPerpetualUpdate.Symbol &&
                                        p.Direction == (positionUsdPerpetualUpdate.Side == PositionSide.Buy ? Direction.Long : Direction.Short) &&
                                        p.Quantity == positionUsdPerpetualUpdate.Quantity &&
                                        p.LeverageMultiplier == positionUsdPerpetualUpdate.Leverage &&
                                        (p.Status == Common.Enums.PositionStatus.Created || p.Status == Common.Enums.PositionStatus.Open))
                            .ToListAsync();
                        
                        if (positions.Count != 1)
                        {
                            _logger.LogError("Ambiguous or no result {Count} when querying for Position matching PositionUpdate", positions.Count);
                            return;
                        }
                        var position = positions.Single();
                        
                        // Update LiquidationPrice on Position.
                        if (position.LiquidationPrice == null || position.LiquidationPrice == 0.0M)
                        {
                            _logger.LogDebug("Updating LiquidationPrice for position {Exchange}:{Base}{Quote}", position.Exchange, position.BaseAsset, position.QuoteAsset);
                            position.LiquidationPrice = positionUsdPerpetualUpdate.LiquidationPrice;
                            position.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        
                        if (positionUsdPerpetualUpdate.PositionStatus == PositionStatus.Liqidation)
                        {
                            // TODO: Handle liquidation, change Position status to Liquidated, update PnL.
                            _logger.LogWarning("Updating position {Exchange}:{Base}{Quote} as liquidated", position.Exchange, position.BaseAsset, position.QuoteAsset);
                            position.Status = Common.Enums.PositionStatus.Liquidated;
                            position.CompletedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            position.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }

                        await signalTraderDbContext.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Caught Exception in PositionUpdates handler");
                    }
                }
            }, _cancellationTokenSource!.Token);
            if (subscriptionResult.Success)
            {
                _logger.LogInformation("Subscribed to PositionUpdates for account {AccountId}", account.Id);
                _positionUpdateSubscriptions.AddOrUpdate(account.Id, subscriptionResult.Data, (l, subscription) => subscriptionResult.Data);
                return new ExchangeSubscriptionResult(true);
            }

            _logger.LogInformation("Failed to subscribe to PositionUpdates for account {AccountId}: {Error}", account.Id, subscriptionResult.Error!.ToString());
            return new ExchangeSubscriptionResult(subscriptionResult.Error!.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in SubscribeToPositionUpdatesAsync");
            return new ExchangeSubscriptionResult(e.Message);
        }
    }

    public async Task<ExchangeSubscriptionResult> SubscribeToOrderUpdatesAsync(Account account)
    {
        try
        {
            if (_orderUpdateSubscriptions.ContainsKey(account.Id))
            {
                return new ExchangeSubscriptionResult(true);
            }
            
            _logger.LogInformation("Subscribing to OrderUpdates for account {AccountId}", account.Id);
            var bybitSocketClient = new BybitSocketClient(BybitFuturesExchange.BuildBybitSocketClientOptions(account, _configuration));
            var subscriptionResult = await bybitSocketClient.UsdPerpetualStreams.SubscribeToOrderUpdatesAsync(async @event =>
            {
                using var scope = _serviceScopeFactory.CreateAsyncScope();
                foreach (var orderUpdate in @event.Data)
                {
                    try
                    {
                        _logger.LogDebug("Received OrderUpdate:\n{OrderUpdate}", JsonSerializer.Serialize(orderUpdate).ToString());
                    
                        // Catch up with processing cached UserTradeUpdates, in case order is now in db.
                        await ProcessUserTradeUpdatesAsync(orderUpdate.Id);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Caught Exception in OrderUpdates handler");
                    }
                }
            }, _cancellationTokenSource!.Token);
            if (subscriptionResult.Success)
            {
                _logger.LogInformation("Subscribed to OrderUpdates for account {AccountId}", account.Id);
                _orderUpdateSubscriptions.AddOrUpdate(account.Id, subscriptionResult.Data, (l, subscription) => subscriptionResult.Data);
                return new ExchangeSubscriptionResult(true);
            }

            _logger.LogInformation("Failed to subscribe to OrderUpdates for account {AccountId}: {Error}", account.Id, subscriptionResult.Error!.ToString());
            return new ExchangeSubscriptionResult(subscriptionResult.Error!.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in SubscribeToOrderUpdatesAsync");
            return new ExchangeSubscriptionResult(e.Message);
        }
    }

    #endregion
}
