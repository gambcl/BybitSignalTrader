using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Accounts.Services;
using SignalTrader.Common.Enums;
using SignalTrader.Common.Extensions;
using SignalTrader.Data;
using SignalTrader.Data.Entities;
using SignalTrader.Exchanges;
using SignalTrader.Exchanges.Models;
using SignalTrader.Orders.Services;
using SignalTrader.Positions.Models;
using SignalTrader.Positions.Notifications;
using SignalTrader.Signals.SignalScript;
using SignalTrader.Telegram.Services;

namespace SignalTrader.Positions.Services;

public class PositionsService : IPositionsService
{
    #region Members

    private static SemaphoreSlim _updatePositionSemaphoreSlim = new SemaphoreSlim(1, 1);
    private readonly ILogger<PositionsService> _logger;
    private readonly IExchangeProvider _exchangeProvider;
    private readonly IAccountsService _accountsService;
    private readonly ITelegramService _telegramService;
    private readonly IOrdersService _ordersService;
    private readonly IMediator _mediator;
    private readonly SignalTraderDbContext _signalTraderDbContext;

    #endregion

    #region Constructors

    public PositionsService(ILogger<PositionsService> logger, IExchangeProvider exchangeProvider, IAccountsService accountsService, ITelegramService telegramService, IOrdersService ordersService, IMediator mediator, SignalTraderDbContext signalTraderDbContext)
    {
        _logger = logger;
        _exchangeProvider = exchangeProvider;
        _accountsService = accountsService;
        _telegramService = telegramService;
        _ordersService = ordersService;
        _mediator = mediator;
        _signalTraderDbContext = signalTraderDbContext;
    }

    #endregion

    #region IPositionsService

    public async Task OpenPositionAsync(long? accountId, string? quoteAsset, string? baseAsset, decimal? leverageMultiplier, LeverageType? leverageType, Direction? direction, OrderType? orderType, decimal? quantity, ValueWrapper? costValue, ValueWrapper? priceValue, ValueWrapper? offsetValue, ValueWrapper? stopLossValue)
    {
        Account? account = null;
        
        try
        {
            Guard.Against.Null(accountId, nameof(accountId));
            Guard.Against.NegativeOrZero(accountId.Value, nameof(accountId));
            Guard.Against.NullOrWhiteSpace(quoteAsset, nameof(quoteAsset));
            Guard.Against.NullOrWhiteSpace(baseAsset, nameof(baseAsset));
            Guard.Against.Null(leverageMultiplier, nameof(leverageMultiplier));
            Guard.Against.Null(leverageType, nameof(leverageType));
            Guard.Against.Null(direction, nameof(direction));
            Guard.Against.Null(orderType, nameof(orderType));
            if (costValue == null)
            {
                // One of cost/quantity must be provided.
                Guard.Against.Null(quantity, nameof(quantity));
            }
            if (quantity == null)
            {
                // One of cost/quantity must be provided.
                Guard.Against.Null(costValue, nameof(costValue));
            }
            if (orderType == OrderType.Limit)
            {
                // Price is required for limit orders.
                Guard.Against.Null(priceValue, nameof(priceValue));
            }
            Guard.Against.Null(offsetValue, nameof(offsetValue));
            //Guard.Against.Null(stopLossValue, nameof(stopLossValue));
            if (leverageMultiplier < 1.0M)
            {
                throw new ArgumentOutOfRangeException(nameof(leverageMultiplier), "Leverage multiplier must be >= 1.0");
            }

            _logger.LogInformation("OpenPositionAsync({AccountId}, \"{QuoteAsset}\", \"{BaseAsset}\", {LeverageMultiplier}, {LeverageType}, {Direction}, {OrderType}, {Quantity}, {Cost}, {Price}, {Offset}, {StopLoss})", accountId, quoteAsset, baseAsset, leverageMultiplier, leverageType, direction, orderType, quantity, costValue?.Text, priceValue?.Text, offsetValue?.Text, stopLossValue?.Text);
            
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

            // Check current position on exchange is flat.
            var positionInfo = await exchange.GetPositionInfoAsync(account, quoteAsset, baseAsset);
            if (positionInfo == null)
            {
                throw new ApplicationException($"Failed to get position info for {account.Exchange}:{baseAsset}{quoteAsset}");
            }
            if (positionInfo.Direction == null)
            {
                // Exchange position is flat, we are OK to open new position.
                
                // Get available quote asset in account.
                await _accountsService.UpdateAccountBalancesAsync(account);
                var accountBalances = _accountsService.GetBalances(account.Id);
                var availableQuote = accountBalances[quoteAsset].AvailableBalance;

                // Get symbol ticker from exchange.
                var symbolTicker = await exchange.GetTickerAsync(quoteAsset, baseAsset);
                if (symbolTicker == null)
                {
                    throw new ApplicationException($"Failed to get ticker for {account.Exchange}:{baseAsset}{quoteAsset}");
                }

                Side side = direction == Direction.Long ? Side.Buy : Side.Sell;
                decimal? price = DeterminePrice(symbolTicker, orderType.Value, priceValue, offsetValue);
                
                decimal quantityBase = DetermineQuantity(symbolTicker, quantity, costValue, price, leverageMultiplier, availableQuote);

                decimal? stopLoss = DetermineStopLoss(symbolTicker, direction.Value, price, stopLossValue);
                
                var result = await exchange.PlaceOrderAsync(account, quoteAsset, baseAsset, side, orderType.Value, price, quantityBase, stopLoss, leverageMultiplier, leverageType, false);
                if (result.Success)
                {
                    // Create Position record.
                    var position = new Position();
                    position.AccountId = account.Id;
                    position.Account = account;
                    position.Exchange = account.Exchange;
                    position.QuoteAsset = quoteAsset;
                    position.BaseAsset = baseAsset;
                    position.Direction = direction.Value;
                    position.Quantity = result.Quantity;
                    position.StopLoss = stopLoss;
                    position.LeverageMultiplier = leverageMultiplier;
                    position.LeverageType = leverageType.Value;
                    position.Status = PositionStatus.Created;
                    _signalTraderDbContext.Positions.Add(position);
                    var dbResult = await _signalTraderDbContext.SaveChangesAsync();
                    
                    // Create Order record.
                    var order = new Order();
                    order.ExchangeOrderId = result.Id;
                    order.AccountId = account.Id;
                    order.Account = account;
                    order.Exchange = result.Exchange;
                    order.QuoteAsset = quoteAsset;
                    order.BaseAsset = baseAsset;
                    order.Side = result.Side;
                    order.Type = result.Type;
                    order.Price = price;
                    order.Quantity = result.Quantity;
                    order.TakeProfit = result.TakeProfit;
                    order.StopLoss = result.StopLoss;
                    order.ReduceOnly = result.ReduceOnly;
                    order.Status = result.Status;
                    order.PositionId = position.Id;
                    order.Position = position;
                    position.Orders.Add(order);
                    _signalTraderDbContext.Orders.Add(order);
                    dbResult = await _signalTraderDbContext.SaveChangesAsync();

                    await _telegramService.SendMessageNotificationAsync(
                        direction?.ToEmoji(),
                        account.Name,
                        $"Opening {direction} position of {result.Quantity} {baseAsset}{quoteAsset} at {position.LeverageMultiplier:N2}x leverage",
                        null);
                }
                else
                {
                    throw new ApplicationException(result.Message);
                }
            }
            else
            {
                // Attempted to open a new position when already in a position on exchange.
                if (positionInfo.Direction == direction)
                {
                    // Exchange says we have a position in the same direction as what we are trying to open.
                    // Most likely reason is that we received two or more identical signals in a row.
                    // i.e. Attempting to enter a long when we are already long, so we'll just leave position untouched.
                    _logger.LogInformation("Attempting to open {BaseAsset}{QuoteAsset} {Direction} position when {Exchange} position is already {ExchangeDirection}", baseAsset, quoteAsset, direction, account.Exchange, positionInfo.Direction);
                }
                else
                {
                    // This is bit more serious, something has gone wrong with closing previous position (in opposite direction).
                    // Need to let user know so they can investigate and manually close on exchange.
                    _logger.LogError("Attempting to open {BaseAsset}{QuoteAsset} {Direction} position when {Exchange} position is still {ExchangeDirection}", baseAsset, quoteAsset, direction, account.Exchange, positionInfo.Direction);
                    throw new ApplicationException($"Attempting to open {baseAsset}{quoteAsset} {direction} position when {account.Exchange} position is still {positionInfo.Direction}");
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in OpenPositionAsync");
            await _telegramService.SendMessageNotificationAsync(
                Telegram.Constants.Emojis.NameBadge,
                account?.Name,
                $"Failed to open {(direction.HasValue ? direction.Value : "unknown")} position",
                e.Message
                );
        }
    }

    public async Task ClosePositionAsync(long? accountId, string? quoteAsset, string? baseAsset, Direction? direction, OrderType? orderType, ValueWrapper? priceValue, ValueWrapper? offsetValue)
    {
        Account? account = null;
        
        try
        {
            Guard.Against.Null(accountId, nameof(accountId));
            Guard.Against.NegativeOrZero(accountId.Value, nameof(accountId));
            Guard.Against.NullOrWhiteSpace(quoteAsset, nameof(quoteAsset));
            Guard.Against.NullOrWhiteSpace(baseAsset, nameof(baseAsset));
            Guard.Against.Null(direction, nameof(direction));
            Guard.Against.Null(orderType, nameof(orderType));
            if (orderType == OrderType.Limit)
            {
                // Price is required for limit orders.
                Guard.Against.Null(priceValue, nameof(offsetValue));
            }
            Guard.Against.Null(offsetValue, nameof(offsetValue));

            _logger.LogInformation("ClosePositionAsync({AccountId}, \"{QuoteAsset}\", \"{BaseAsset}\", {Direction}, {OrderType}, {Price}, {Offset})", accountId, quoteAsset, baseAsset, direction, orderType, priceValue?.Text, offsetValue?.Text);
            
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
            
            // Get current position size.
            var positionInfo = await exchange.GetPositionInfoAsync(account, quoteAsset, baseAsset);
            if (positionInfo == null)
            {
                throw new ApplicationException($"Failed to get position info for {account.Exchange}:{baseAsset}{quoteAsset}");
            }

            // Find open Position object in db.
            var position = await _signalTraderDbContext.Positions
                .Include(p => p.Account)
                .Include(p => p.Orders)
                .FirstOrDefaultAsync(p => p.AccountId == account.Id &&
                    p.QuoteAsset == quoteAsset &&
                    p.BaseAsset == baseAsset &&
                    p.Direction == direction && 
                    (p.Status == PositionStatus.Created || p.Status == PositionStatus.Open));

            if (positionInfo.Direction != null && positionInfo.Direction == direction)
            {
                // The exchange thinks we have a position in the expected direction.
                
                if (position == null)
                {
                    throw new ApplicationException($"Failed to find open Position for {account.Exchange}:{baseAsset}{quoteAsset}");
                }

                // Get symbol ticker from exchange.
                var symbolTicker = await exchange.GetTickerAsync(quoteAsset, baseAsset);
                if (symbolTicker == null)
                {
                    throw new ApplicationException($"Failed to get ticker for {account.Exchange}:{baseAsset}{quoteAsset}");
                }

                Side side = direction == Direction.Long ? Side.Sell : Side.Buy;
                decimal? price = DeterminePrice(symbolTicker, orderType.Value, priceValue, offsetValue);

                var quantityBase = positionInfo.Quantity;
                
                var result = await exchange.PlaceOrderAsync(account, quoteAsset, baseAsset, side, orderType.Value, price, quantityBase, null, null, null, true);
                if (result.Success)
                {
                    // Update Position record.
                    position.Status = PositionStatus.CloseInProgress;
                    position.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    
                    // Create Order record.
                    var order = new Order();
                    order.ExchangeOrderId = result.Id;
                    order.AccountId = account.Id;
                    order.Account = account;
                    order.Exchange = result.Exchange;
                    order.QuoteAsset = quoteAsset;
                    order.BaseAsset = baseAsset;
                    order.Side = result.Side;
                    order.Type = result.Type;
                    order.Price = price;
                    order.Quantity = result.Quantity;
                    order.TakeProfit = result.TakeProfit;
                    order.StopLoss = result.StopLoss;
                    order.ReduceOnly = result.ReduceOnly;
                    order.Status = result.Status;
                    order.PositionId = position.Id;
                    order.Position = position;
                    position.Orders.Add(order);
                    _signalTraderDbContext.Orders.Add(order);
                    var dbResult = await _signalTraderDbContext.SaveChangesAsync();

                    await _telegramService.SendMessageNotificationAsync(
                        direction?.ToEmoji(),
                        account.Name,
                        $"Closing {direction} position of {result.Quantity} {baseAsset}{quoteAsset} at {position.LeverageMultiplier:N2}x leverage",
                        null);
                    
                    // Wait a short while for order to fill on exchange.
                    await _ordersService.WaitForOrderCompletionAsync(order, OrdersService.OrderWaitType.Fill);
                }
                else
                {
                    throw new ApplicationException(result.Message);
                }
            }
            else
            {
                // Nothing to do.
                if (positionInfo.Direction == null)
                {
                    // Exchange says we are flat, no position to close.
                    _logger.LogInformation("Attempting to close {BaseAsset}{QuoteAsset} {ExpectedDirection} position when {Exchange} position is flat", baseAsset, quoteAsset, direction, account.Exchange);

                    if (position != null)
                    {
                        _logger.LogWarning("Found open {BaseAsset}{QuoteAsset} {Direction} position in database [id:{PositionId}] when {Exchange} position is flat", baseAsset, quoteAsset, position.Direction, position.Id, account.Exchange);
                    }
                }
                else
                {
                    // Exchange says we have a position in the opposite direction to what we are trying to close.
                    // Most likely reason is that we received two or more identical signals in a row.
                    // i.e. Attempting to enter a long when we are already long, so we'll just leave position untouched.
                    _logger.LogInformation("Attempting to close {BaseAsset}{QuoteAsset} {ExpectedDirection} position on {Exchange}, but found a {ActualPosition} position, ignoring", baseAsset, quoteAsset, direction, account.Exchange, positionInfo.Direction);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in ClosePositionAsync");
            await _telegramService.SendMessageNotificationAsync(
                Telegram.Constants.Emojis.NameBadge,
                account?.Name,
                $"Failed to close {(direction.HasValue ? direction.Value : "unknown")} position",
                e.Message
                );
        }
    }

    public async Task UpdatePositionsAsync()
    {
        var positions = await _signalTraderDbContext.Positions
            .Include(p => p.Account)
            .Include(p => p.Orders)
            .Where(p => p.Status != PositionStatus.Closed &&
                        p.Status != PositionStatus.StopLoss &&
                        p.Status != PositionStatus.Liquidated)
            .ToListAsync();
        foreach (var position in positions)
        {
            await UpdatePositionAsync(position);
        }
    }

    public async Task UpdatePositionAsync(Position position)
    {
        await _updatePositionSemaphoreSlim.WaitAsync();
        try
        {
            bool statusChanged = false;

            // Refresh entities in memory with any changes in db.
            await _signalTraderDbContext.Entry(position).ReloadAsync();
            await _signalTraderDbContext.Entry(position.Account).ReloadAsync();

            // Get Account object.
            var account = position.Account;
            if (account == null)
            {
                throw new ApplicationException($"Failed to get account {position.AccountId}");
            }

            // Get Exchange instance.
            var exchange = _exchangeProvider.GetExchange(account.Exchange);
            if (exchange == null)
            {
                throw new ApplicationException($"Failed to get exchange {account.Exchange}");
            }

            // Get current position size.
            var positionInfo = await exchange.GetPositionInfoAsync(account, position.QuoteAsset, position.BaseAsset);
            if (positionInfo == null)
            {
                throw new ApplicationException($"Failed to get position info for {account.Exchange}:{position.BaseAsset}{position.QuoteAsset}");
            }

            // Check we are talking about the current exchange position before getting Liquidation Price.
            if (positionInfo.Exchange == position.Exchange &&
                positionInfo.QuoteAsset == position.QuoteAsset &&
                positionInfo.BaseAsset == position.BaseAsset &&
                positionInfo.Direction == position.Direction &&
                positionInfo.Quantity == position.Quantity &&
                positionInfo.LeverageMultiplier == position.LeverageMultiplier)
            {
                position.LiquidationPrice = positionInfo.LiquidationPrice;
            }

            // Update Position status in db
            if (position.Status == PositionStatus.Created)
            {
                // Options are:
                // - Move to Open if any entry orders have been (partially) filled
                // - Move to Closed if all entry orders were cancelled with nothing filled

                Side entrySide = position.Direction == Direction.Long ? Side.Buy : Side.Sell;
                
                var entryOrders = position.Orders
                    .FindAll(o => o.PositionId == position.Id &&
                                  o.Exchange == position.Exchange &&
                                  o.QuoteAsset == position.QuoteAsset &&
                                  o.BaseAsset == position.BaseAsset &&
                                  o.Side == entrySide);
                bool anyFills = false;
                bool allCancelledNoFills = true;
                foreach (var entryOrder in entryOrders)
                {
                    if ((entryOrder.Status != OrderStatus.Rejected && entryOrder.Status != OrderStatus.Cancelled) || entryOrder.QuantityFilled > 0.0M)
                    {
                        allCancelledNoFills = false;
                    }

                    if (entryOrder.QuantityFilled > 0.0M)
                    {
                        anyFills = true;
                    }
                }

                if (allCancelledNoFills)
                {
                    position.Status = PositionStatus.Closed;
                    position.CompletedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    statusChanged = true;
                }
                else if (anyFills)
                {
                    position.Status = PositionStatus.Open;
                    statusChanged = true;
                }
            }
            else if (position.Status == PositionStatus.CloseInProgress)
            {
                // Options are:
                // - Move to Closed if all exit orders are complete
                
                Side exitSide = position.Direction == Direction.Long ? Side.Sell : Side.Buy;
                
                var exitOrders = position.Orders
                    .FindAll(o => o.PositionId == position.Id &&
                                  o.Exchange == position.Exchange &&
                                  o.QuoteAsset == position.QuoteAsset &&
                                  o.BaseAsset == position.BaseAsset &&
                                  o.Side == exitSide);
                bool allExitOrdersComplete = true;
                foreach (var exitOrder in exitOrders)
                {
                    if (!exitOrder.IsComplete)
                    {
                        allExitOrdersComplete = false;
                        break;
                    }
                }

                if (allExitOrdersComplete)
                {
                    position.Status = PositionStatus.Closed;
                    position.CompletedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    statusChanged = true;
                }
            }
            else if (position.Status == PositionStatus.StopLossInProgress)
            {
                // Options are:
                // - Move to StopLoss if all exit orders are complete
                
                Side exitSide = position.Direction == Direction.Long ? Side.Sell : Side.Buy;
                
                var exitOrders = position.Orders
                    .FindAll(o => o.PositionId == position.Id &&
                                  o.Exchange == position.Exchange &&
                                  o.QuoteAsset == position.QuoteAsset &&
                                  o.BaseAsset == position.BaseAsset &&
                                  o.Side == exitSide);
                bool allExitOrdersComplete = true;
                foreach (var exitOrder in exitOrders)
                {
                    if (!exitOrder.IsComplete)
                    {
                        allExitOrdersComplete = false;
                        break;
                    }
                }

                if (allExitOrdersComplete)
                {
                    position.Status = PositionStatus.StopLoss;
                    position.CompletedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    statusChanged = true;
                }
            }

            // Update PnL in db.
            _logger.LogDebug("Calculating PnL for {Exchange}:{Base}{Quote}", position.Exchange, position.BaseAsset, position.QuoteAsset);
            var pnl = await CalculateProfitAndLossAsync(position);
            if (pnl.Success)
            {
                position.UnrealisedPnl = pnl.UnrealisedPnl;
                position.UnrealisedPnlPercent = pnl.UnrealisedPnlPercent;
                position.RealisedPnl = pnl.RealisedPnl;
                position.RealisedPnlPercent = pnl.RealisedPnlPercent;
            }
            else
            {
                _logger.LogError("Failed to calculate PnL for {Exchange}:{Base}{Quote}: {Error}", position.Exchange, position.BaseAsset, position.QuoteAsset, pnl.Message);
            }

            position.UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _signalTraderDbContext.SaveChangesAsync();

            if (statusChanged)
            {
                await _mediator.Publish(new PositionStatusChangedNotification { Position = position });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in UpdatePositionAsync");
        }
        finally
        {
            _updatePositionSemaphoreSlim.Release();
        }
    }

    public async Task<ProfitAndLossResult> CalculateProfitAndLossAsync(Position position)
    {
        const int percentDecimalPlaces = 4;
        decimal quantityFilled = 0.0M;
        decimal unrealisedPnl = 0.0M;
        decimal unrealisedPnlPercent = 0.0M;
        decimal realisedPnl = 0.0M;
        decimal realisedPnlPercent = 0.0M;
        
        // Get Exchange instance.
        var exchange = _exchangeProvider.GetExchange(position.Exchange);
        if (exchange == null)
        {
            throw new ApplicationException($"Failed to get exchange {position.Exchange}");
        }

        // Get symbol ticker from exchange.
        var symbolTicker = await exchange.GetTickerAsync(position.QuoteAsset, position.BaseAsset);
        if (symbolTicker == null)
        {
            throw new ApplicationException($"Failed to get ticker for {position.Exchange}:{position.BaseAsset}{position.QuoteAsset}");
        }

        // Calculate PnL using average cost method.

        Side entrySide = position.Direction == Direction.Long ? Side.Buy : Side.Sell;
        Side exitSide = position.Direction == Direction.Long ? Side.Sell : Side.Buy;
        decimal positionSign = position.Direction == Direction.Long ? 1.0M : -1.0M;
        
        // STEP 1: Calculate cost basis.
        decimal entryQuantity = 0.0M;
        decimal entryCost = 0.0M;
        var averageEntryPrice = 0.0M;
        var entryOrders = await _signalTraderDbContext.Orders
            .Include(o => o.Account)
            .Include(o => o.Position)
            .Where(o => o.PositionId == position.Id &&
                        o.Exchange == position.Exchange &&
                        o.QuoteAsset == position.QuoteAsset &&
                        o.BaseAsset == position.BaseAsset &&
                        o.Side == entrySide).ToListAsync();

        if (entryOrders.Count == 0)
        {
            return new ProfitAndLossResult("No entry orders found");
        }
        
        foreach (var entryOrder in entryOrders)
        {
            if (entryOrder.Price.HasValue && entryOrder.Price > 0.0M)
            {
                quantityFilled += entryOrder.QuantityFilled;
                entryQuantity += (positionSign * entryOrder.QuantityFilled);
                entryCost += (entryQuantity * entryOrder.Price.Value);
            }
            else
            {
                return new ProfitAndLossResult("Found entry order with missing Price");
            }
        }

        // Only need to calculate PnL if we actually had some entry fills.
        if (quantityFilled > 0.0M && entryQuantity != 0.0M)
        {
            averageEntryPrice = entryCost / entryQuantity;
            
            // STEP 2: Calculate realised PnL.
            decimal exitQuantity = 0.0M;
            decimal exitAmount = 0.0M;
            var averageRealisedExitPrice = 0.0M;
            var exitOrders = await _signalTraderDbContext.Orders
                .Include(o => o.Account)
                .Include(o => o.Position)
                .Where(o => o.PositionId == position.Id &&
                            o.Exchange == position.Exchange &&
                            o.QuoteAsset == position.QuoteAsset &&
                            o.BaseAsset == position.BaseAsset &&
                            o.Side == exitSide).ToListAsync();
            foreach (var exitOrder in exitOrders)
            {
                if (exitOrder.Price.HasValue && exitOrder.Price > 0.0M)
                {
                    exitQuantity += (positionSign * exitOrder.QuantityFilled);
                    exitAmount += (exitQuantity * exitOrder.Price.Value);
                }
                else
                {
                    return new ProfitAndLossResult("Found exit order with missing Price");
                }
            }
        
            if (exitQuantity != 0.0M)
            {
                averageRealisedExitPrice = exitAmount / exitQuantity;
            }

            realisedPnl = (averageRealisedExitPrice - averageEntryPrice) * exitQuantity;
            if (exitQuantity != 0.0M)
            {
                realisedPnlPercent = positionSign * ((averageRealisedExitPrice / averageEntryPrice) - 1.0M) * 100.0M;
            }
        
        
            // STEP 3: Calculate unrealised PnL.
            var remainingQuantity = entryQuantity - exitQuantity;
            unrealisedPnl = (symbolTicker.LastPrice - averageEntryPrice) * remainingQuantity;
            if (remainingQuantity != 0.0M)
            {
                unrealisedPnlPercent = positionSign * ((symbolTicker.LastPrice / averageEntryPrice) - 1.0M) * 100.0M;
            }
        }
        
        return new ProfitAndLossResult(true)
        {
            Exchange = position.Exchange,
            QuoteAsset = position.QuoteAsset,
            BaseAsset = position.BaseAsset,
            QuantityFilled = quantityFilled,
            UnrealisedPnl = unrealisedPnl,
            UnrealisedPnlPercent = unrealisedPnlPercent.TruncateToDecimalPlaces(percentDecimalPlaces),
            RealisedPnl = realisedPnl,
            RealisedPnlPercent = realisedPnlPercent.TruncateToDecimalPlaces(percentDecimalPlaces)
        };
    }

    public async Task<PositionsResult> GetPositionsAsync(long? accountId = null, SupportedExchange? exchange = null, string? quoteAsset = null, string? baseAsset = null, Direction? direction = null, PositionStatus? status = null)
    {
        var positions = await _signalTraderDbContext.Positions
            .Include(p => p.Account)
            .Where(p => (!accountId.HasValue || p.AccountId == accountId) &&
                        (!exchange.HasValue || p.Exchange == exchange) &&
                        (string.IsNullOrWhiteSpace(quoteAsset) || p.QuoteAsset.Equals(quoteAsset)) &&
                        (string.IsNullOrWhiteSpace(baseAsset) || p.BaseAsset.Equals(baseAsset)) &&
                        (!direction.HasValue || p.Direction == direction) &&
                        (!status.HasValue || p.Status == status))
            .OrderBy(p => p.Id)
            .ToListAsync();

        return new PositionsResult(true)
        {
            Positions = positions.Select(p => p.ToPositionResource()).ToList()
        };
    }

    public async Task<PositionResult> GetPositionAsync(long positionId)
    {
        Guard.Against.NegativeOrZero(positionId, nameof(positionId));
        
        var position = await _signalTraderDbContext.Positions
            .Include(p => p.Account)
            .Where(p => p.Id == positionId)
            .SingleOrDefaultAsync();

        if (position != null)
        {
            return new PositionResult(true)
            {
                Position = position.ToPositionResource()
            };
        }

        return new PositionResult($"Position {positionId} not found");
    }

    #endregion
    
    #region Private

    private decimal? DeterminePrice(Ticker symbolTicker, OrderType orderType, ValueWrapper? priceValue, ValueWrapper? offsetValue)
    {
        decimal? price = null;
        
        if (orderType == OrderType.Limit)
        {
            if (priceValue!.Type == ValueWrapper.ValueType.Price)
            {
                var priceType = priceValue.GetPriceValue();
                price = priceType switch
                {
                    Price.Ask => symbolTicker.AskPrice,
                    Price.Bid => symbolTicker.BidPrice,
                    Price.Last => symbolTicker.LastPrice,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            else
            {
                price = priceValue.GetDecimalValue();
            }
                
            // Apply optional offset to price.
            if (price.HasValue && offsetValue != null)
            {
                decimal offset = 0M;
                if (offsetValue.Type == ValueWrapper.ValueType.Float || offsetValue.Type == ValueWrapper.ValueType.Int)
                {
                    offset = offsetValue.GetDecimalValue();
                }
                else if (offsetValue.Type == ValueWrapper.ValueType.FloatPercent || offsetValue.Type == ValueWrapper.ValueType.IntPercent)
                {
                    decimal offsetPercent = offsetValue.GetDecimalValue();
                    offset = (price.Value / 100M) * offsetPercent;
                }

                price += offset;
            }
        }

        return price;
    }

    private decimal DetermineQuantity(Ticker symbolTicker, decimal? quantity, ValueWrapper? costValue, decimal? price, decimal? leverageMultiplier, decimal availableQuote)
    {
        decimal quantityBase = 0.0M;
        
        if (quantity.HasValue)
        {
            quantityBase = quantity.Value;
        }
        else if (costValue != null)
        {
            decimal entryPrice = price ?? symbolTicker.LastPrice;
            if (costValue.Type == ValueWrapper.ValueType.FloatPercent || costValue.Type == ValueWrapper.ValueType.IntPercent)
            {
                decimal costPercent = costValue.GetDecimalValue();
                leverageMultiplier ??= 1.0M;
                decimal maxPercentage = 100M * leverageMultiplier.Value;
                if (costPercent <= 0M || costPercent > maxPercentage)
                {
                    throw new ArgumentException($"Cost percentage must be greater than 0% and less than {maxPercentage:G}%");
                }
                decimal costQuote = availableQuote * (costPercent / 100.0M);
                // Calculate quantity (BaseAsset) from cost (QuoteAsset).
                quantityBase = costQuote / entryPrice;
            }
            else if (costValue.Type == ValueWrapper.ValueType.Float || costValue.Type == ValueWrapper.ValueType.Int)
            {
                decimal costQuote = costValue.GetDecimalValue();
                // Calculate quantity (BaseAsset) from cost (QuoteAsset).
                quantityBase = costQuote / entryPrice;
            }
        }

        return quantityBase;
    }

    private decimal? DetermineStopLoss(Ticker symbolTicker, Direction direction, decimal? price, ValueWrapper? stopLossValue)
    {
        decimal? stopLoss = null;
        
        if (stopLossValue != null)
        {
            if (stopLossValue.Type == ValueWrapper.ValueType.Float || stopLossValue.Type == ValueWrapper.ValueType.Int)
            {
                stopLoss = stopLossValue.GetDecimalValue();
            }
            else if (stopLossValue.Type == ValueWrapper.ValueType.FloatPercent || stopLossValue.Type == ValueWrapper.ValueType.IntPercent)
            {
                decimal entryPrice = price ?? symbolTicker.LastPrice;
                decimal stopLossPercent = stopLossValue.GetDecimalValue();
                if (direction == Direction.Long)
                {
                    // Calculate long stopLoss.
                    stopLoss = entryPrice * ((100M + stopLossPercent)/100M);
                }
                else
                {
                    // Calculate short stopLoss.
                    stopLoss = entryPrice * ((100M - stopLossPercent)/100M);
                }
            }
        }

        return stopLoss;
    }

    #endregion
}
