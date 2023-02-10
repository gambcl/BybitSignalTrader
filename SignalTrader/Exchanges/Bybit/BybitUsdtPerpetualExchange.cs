using System.Globalization;
using System.Text.Json;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models;
using SignalTrader.Common.Enums;
using SignalTrader.Common.Extensions;
using SignalTrader.Data.Entities;
using SignalTrader.Exchanges.Exceptions;
using SignalTrader.Exchanges.Models;
using SignalTrader.Positions.Models;
using OrderStatus = SignalTrader.Common.Enums.OrderStatus;
using OrderType = SignalTrader.Common.Enums.OrderType;

namespace SignalTrader.Exchanges.Bybit;

public class BybitUsdtPerpetualExchange : BybitFuturesExchange, IBybitUsdtPerpetualExchange
{
    #region Members

    private readonly IBybitUsdtPerpetualExchangeListener _bybitUsdtPerpetualExchangeListener;

    #endregion
    
    #region Constructors

    public BybitUsdtPerpetualExchange(ILogger<BybitUsdtPerpetualExchange> logger, IConfiguration configuration, IBybitUsdtPerpetualExchangeListener bybitUsdtPerpetualExchangeListener) : base(logger, configuration)
    {
        _bybitUsdtPerpetualExchangeListener = bybitUsdtPerpetualExchangeListener;
    }

    #endregion

    #region IExchange

    public async Task<OrderResult> PlaceOrderAsync(Account account, string quoteAsset, string baseAsset, Side side, OrderType orderType, decimal? price, decimal quantity, decimal? stopLoss, decimal? leverageMultiplier, LeverageType? leverageType, bool closing)
    {
        try
        {
            var symbol = $"{baseAsset}{quoteAsset}";
            leverageMultiplier ??= 1.0M;
            
            // Get symbol info to use for validation.
            if (_bybitSymbols.TryGetValue(symbol, out var symbolInfo))
            {
                if (!closing)
                {
                    // Truncate and clamp leverage multiplier.
                    leverageMultiplier = leverageMultiplier.Value.TruncateToStepSize(symbolInfo.LeverageFilter.LeverageStep.ToString(CultureInfo.InvariantCulture));
                    if (leverageMultiplier < symbolInfo.LeverageFilter.MinLeverage || leverageMultiplier > symbolInfo.LeverageFilter.MaxLeverage)
                    {
                        throw new ArgumentOutOfRangeException($"{symbol} leverage must be between {symbolInfo.LeverageFilter.MinLeverage} and {symbolInfo.LeverageFilter.MaxLeverage}", nameof(leverageMultiplier));
                    }
                }
                
                // Truncate and clamp quantity.
                quantity = quantity.TruncateToStepSize(symbolInfo.LotSizeFilter.QuantityStep.ToString(CultureInfo.InvariantCulture));
                quantity = Math.Min(quantity, symbolInfo.LotSizeFilter.MaxQuantity);
                if (quantity < symbolInfo.LotSizeFilter.MinQuantity)
                {
                    throw new ArgumentOutOfRangeException($"{symbol} quantity must be between {symbolInfo.LotSizeFilter.MinQuantity} and {symbolInfo.LotSizeFilter.MaxQuantity}", nameof(quantity));
                }

                // Truncate and clamp price.
                if (price != null)
                {
                    price = price.Value.TruncateToStepSize(symbolInfo.PriceFilter.TickSize.ToString(CultureInfo.InvariantCulture));
                    if (price < symbolInfo.PriceFilter.MinPrice || price > symbolInfo.PriceFilter.MaxPrice)
                    {
                        throw new ArgumentOutOfRangeException($"{symbol} price must be between {symbolInfo.PriceFilter.MinPrice} and {symbolInfo.PriceFilter.MaxPrice}", nameof(price));
                    }
                }
            }
            
            var bybitClient = new BybitClient(BuildBybitClientOptions(account, _configuration));

            if (!closing)
            {
                // Set the position mode on the account.
                var pmResult = await bybitClient.UsdPerpetualApi.Account.SetPositionModeAsync(symbol, String.Empty, false, ReceiveWindow);
                _logger.LogDebug("SetPositionModeAsync exchange response: {Response}", JsonSerializer.Serialize(pmResult));
                if (!pmResult.Success && pmResult.Error!.Code != 30083)
                {
                    throw new ExchangeException($"Failed to set position mode to One-Way");
                }

                // Set the TP/SL mode to Full.
                var tpslResult = await bybitClient.UsdPerpetualApi.Account.SetFullPartialPositionModeAsync(symbol, StopLossTakeProfitMode.Full, ReceiveWindow);
                _logger.LogDebug("SetFullPartialPositionModeAsync exchange response: {Response}", JsonSerializer.Serialize(tpslResult));
                if (!tpslResult.Success && tpslResult.Error!.Code != 130150)
                {
                    throw new ExchangeException($"Failed to set TP/SL mode to Full");
                }
                
                // Set the leverage multiplier/type on the account.
                bool isIsolated = (leverageType == null) || (leverageType == LeverageType.Isolated);
                decimal buyLeverageMultiplier = leverageMultiplier.Value;
                decimal sellLeverageMultiplier = buyLeverageMultiplier;
                var isoResult = await bybitClient.UsdPerpetualApi.Account.SetIsolatedPositionModeAsync(symbol, isIsolated, buyLeverageMultiplier, sellLeverageMultiplier, ReceiveWindow);
                _logger.LogDebug("SetIsolatedPositionModeAsync exchange response: {Response}", JsonSerializer.Serialize(isoResult));
                if (!isoResult.Success && isoResult.Error!.Code != 130056)
                {
                    throw new ExchangeException($"Failed to set leverage type to {(isIsolated ? LeverageType.Isolated : LeverageType.Cross)}");
                }

                // Set the leverage multiplier values.
                var levResult = await bybitClient.UsdPerpetualApi.Account.SetLeverageAsync(symbol, buyLeverageMultiplier, sellLeverageMultiplier, ReceiveWindow);
                _logger.LogDebug("SetLeverageAsync exchange response: {Response}", JsonSerializer.Serialize(levResult));
                if (!levResult.Success && levResult.Error!.Code != 34036)
                {
                    throw new ExchangeException($"Failed to set leverage to {buyLeverageMultiplier}x/{sellLeverageMultiplier}x");
                }
            }
            
            // Finally, place the trade.
            OrderSide bybitOrderSide = side switch
            {
                Side.Buy => OrderSide.Buy,
                Side.Sell => OrderSide.Sell,
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
            };
            global::Bybit.Net.Enums.OrderType bybitOrderType = orderType switch
            {
                OrderType.Market => global::Bybit.Net.Enums.OrderType.Market,
                OrderType.Limit => global::Bybit.Net.Enums.OrderType.Limit,
                _ => throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null)
            };
            _logger.LogInformation("Placing order [symbol:{Symbol}, side:{Side}, orderType:{OrderType}, price:{Price}, quantity:{Quantity}, stopLoss:{StopLoss}, leverageMultiplier:{LeverageMultiplier}, leverageType:{LeverageType}, closing:{Closing}]", symbol, bybitOrderSide, bybitOrderType, price, quantity, stopLoss, leverageMultiplier, leverageType, closing);
            var orderResult = await bybitClient.UsdPerpetualApi.Trading.PlaceOrderAsync(
                symbol,
                bybitOrderSide,
                bybitOrderType,
                quantity,
                TimeInForce.GoodTillCanceled,
                closing,
                closing,
                price,
                null,
                null,
                stopLoss,
                null,
                null,
                PositionMode.OneWay,
                ReceiveWindow);
            _logger.LogDebug("PlaceOrderAsync exchange response: {Response}", JsonSerializer.Serialize(orderResult));
            if (!orderResult.Success)
            {
                _logger.LogError("Failed to place order [symbol:{Symbol}, side:{Side}, orderType:{OrderType}, price:{Price}, quantity:{Quantity}, stopLoss:{StopLoss}, leverageMultiplier:{LeverageMultiplier}, leverageType:{LeverageType}, closing:{Closing}]", symbol, bybitOrderSide, bybitOrderType, price, quantity, stopLoss, leverageMultiplier, leverageType, closing);
                throw new ExchangeException($"Failed to place {bybitOrderType} {bybitOrderSide} order for {quantity} {symbol}:\n{orderResult.Error!.ToString()}");
            }
            _logger.LogInformation("Successfully placed order [symbol:{Symbol}, side:{Side}, orderType:{OrderType}, price:{Price}, quantity:{Quantity}, stopLoss:{StopLoss}, leverageMultiplier:{LeverageMultiplier}, leverageType:{LeverageType}, closing:{Closing}]",
                orderResult.Data.Symbol,
                orderResult.Data.Side,
                orderResult.Data.Type,
                orderResult.Data.Price,
                orderResult.Data.Quantity,
                orderResult.Data.StopLoss,
                leverageMultiplier,
                leverageType,
                closing);

            return new OrderResult(orderResult.Success)
            {
                Status = OrderStatus.Created,
                Exchange = SupportedExchange.BybitUSDTPerpetual,
                Id = orderResult.Data.Id,
                Symbol = orderResult.Data.Symbol,
                Side = side,
                Type = orderType,
                Quantity = orderResult.Data.Quantity,
                Price = orderResult.Data.Price,
                TakeProfit = orderResult.Data.TakeProfit,
                StopLoss = orderResult.Data.StopLoss,
                ReduceOnly = orderResult.Data.ReduceOnly,
                CloseOnTrigger = orderResult.Data.CloseOnTrigger
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in PlaceOrderAsync");
            return new OrderResult(e.Message);
        }
    }

    public async Task<PositionInfoResult> GetPositionInfoAsync(Account account, string quoteAsset, string baseAsset)
    {
        try
        {
            var symbol = $"{baseAsset}{quoteAsset}";
            var bybitClient = new BybitClient(BuildBybitClientOptions(account, _configuration));
            var result = await bybitClient.UsdPerpetualApi.Account.GetPositionAsync(symbol, ReceiveWindow);
            _logger.LogDebug("GetPositionAsync exchange response: {Response}", JsonSerializer.Serialize(result));
            if (!result.Success && result.Data.Any())
            {
                _logger.LogError("Failed to fetch position info for {Symbol}", symbol);
                throw new ExchangeException($"Failed to fetch position info for {symbol}:\n{result.Error!.ToString()}");
            }

            var exchangePosition = result.Data.FirstOrDefault();

            return new PositionInfoResult(result.Success)
            {
                Exchange = SupportedExchange.BybitUSDTPerpetual,
                QuoteAsset = quoteAsset,
                BaseAsset = baseAsset,
                Direction = exchangePosition!.Side switch
                {
                    PositionSide.Buy => Direction.Long,
                    PositionSide.Sell => Direction.Short,
                    PositionSide.None => null,
                    _ => throw new ArgumentOutOfRangeException()
                },
                Quantity = exchangePosition.Quantity,
                EntryPrice = exchangePosition.EntryPrice,
                LeverageMultiplier = exchangePosition.Leverage,
                PositionMargin = exchangePosition.PositionMargin,
                LiquidationPrice = exchangePosition.LiquidationPrice,
                TakeProfit = exchangePosition.TakeProfit,
                StopLoss = exchangePosition.StopLoss
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetPositionInfoAsync");
            return new PositionInfoResult(e.Message);
        }
    }

    public async Task<ExchangeResult> CancelOrderAsync(Account account, string quoteAsset, string baseAsset, string orderId)
    {
        try
        {
            var symbol = $"{baseAsset}{quoteAsset}";
            var bybitClient = new BybitClient(BuildBybitClientOptions(account, _configuration));
            var result = await bybitClient.UsdPerpetualApi.Trading.CancelOrderAsync(symbol, orderId, null, ReceiveWindow);
            _logger.LogDebug("CancelOrderAsync exchange response: {Response}", JsonSerializer.Serialize(result));
            if (!result.Success && result.Error!.Code != 20001)
            {
                _logger.LogError("Failed to cancel order {OrderId} for {Symbol}", orderId, symbol);
                throw new ExchangeException($"Failed to cancel order {orderId} for {symbol}:\n{result.Error!.ToString()}");
            }
            
            return new ExchangeResult(result.Success);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in CancelOrderAsync");
            return new ExchangeResult(e.Message);
        }
    }

    public async Task<OrderResult> GetOrderInfoAsync(Account account, string quoteAsset, string baseAsset, string orderId)
    {
        try
        {
            const int maxAttempts = 5;
            const int intervalSeconds = 2;
            var symbol = $"{baseAsset}{quoteAsset}";
            var bybitClient = new BybitClient(BuildBybitClientOptions(account, _configuration));
            BybitUsdPerpetualOrder? order = null;
            int numAttemptsRemaining = maxAttempts;
            
            // Sometimes we get a successful response, but an empty cursor, retry a few times until there is data.
            do
            {
                var orderResult = await bybitClient.UsdPerpetualApi.Trading.GetOrdersAsync(symbol, orderId, receiveWindow:ReceiveWindow);
                _logger.LogDebug("GetOrdersAsync exchange response: {Response}", JsonSerializer.Serialize(orderResult));
                if (!orderResult.Success)
                {
                    _logger.LogError("Failed to get order {OrderId} for {Symbol}", orderId, symbol);
                    throw new ExchangeException($"Failed to get order {orderId} for {symbol}:\n{orderResult.Error!.ToString()}");
                }

                var cursorPage = orderResult.Data;
                order = cursorPage?.Data?.First();
                numAttemptsRemaining--;
                if (order == null)
                {
                    _logger.LogDebug("Empty cursor received, retrying after {Interval}s", intervalSeconds);
                    await Task.Delay(intervalSeconds * 1000);
                }
            } while (order == null && numAttemptsRemaining > 0);
            
            if (order != null)
            {
                return new OrderResult(true)
                {
                    Id = order.Id,
                    Exchange = SupportedExchange.BybitUSDTPerpetual,
                    Symbol = order.Symbol,
                    Side = order.Side switch
                    {
                        OrderSide.Buy => Side.Buy,
                        OrderSide.Sell => Side.Sell,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    Type = order.Type switch
                    {
                        global::Bybit.Net.Enums.OrderType.Market => OrderType.Market,
                        global::Bybit.Net.Enums.OrderType.Limit => OrderType.Limit,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    Quantity = order.Quantity,
                    Price = order.Price,
                    TakeProfit = order.TakeProfit,
                    StopLoss = order.StopLoss,
                    ReduceOnly = order.ReduceOnly,
                    CloseOnTrigger = order.CloseOnTrigger,
                    QuantityFilled = order.BaseQuantityFilled ?? 0M,
                    Status = order.Status switch
                    {
                        global::Bybit.Net.Enums.OrderStatus.Created => OrderStatus.Created,
                        global::Bybit.Net.Enums.OrderStatus.Rejected => OrderStatus.Rejected,
                        global::Bybit.Net.Enums.OrderStatus.New => OrderStatus.Created,
                        global::Bybit.Net.Enums.OrderStatus.PartiallyFilled => OrderStatus.PartiallyFilled,
                        global::Bybit.Net.Enums.OrderStatus.Filled => OrderStatus.Filled,
                        global::Bybit.Net.Enums.OrderStatus.Canceled => OrderStatus.Cancelled,
                        global::Bybit.Net.Enums.OrderStatus.PendingCancel => OrderStatus.CancelInProgress,
                        global::Bybit.Net.Enums.OrderStatus.PartiallyFilledCanceled => OrderStatus.CancelledPartiallyFilled,
                        global::Bybit.Net.Enums.OrderStatus.UnTriggered => OrderStatus.Created,
                        _ => throw new ArgumentOutOfRangeException()
                    }
                };
            }

            _logger.LogError("Failed to get order {OrderId} for {Symbol}", orderId, symbol);
            throw new ExchangeException($"Failed to get order {orderId} for {symbol}: Empty cursor after {maxAttempts} attempts");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetOrderInfoAsync");
            return new OrderResult(e.Message);
        }
    }

    public async Task<ExchangeSubscriptionResult> SubscribeToUpdatesAsync(Account account)
    {
        try
        {
            List<string> errors = new List<string>();
            var userTradeUpdatesSubResult = await _bybitUsdtPerpetualExchangeListener.SubscribeToUserTradeUpdatesAsync(account);
            if (!userTradeUpdatesSubResult.Success)
            {
                errors.Add(userTradeUpdatesSubResult.Message!);
            }
            
            var stopOrderUpdatesSubResult = await _bybitUsdtPerpetualExchangeListener.SubscribeToStopOrderUpdatesAsync(account);
            if (!stopOrderUpdatesSubResult.Success)
            {
                errors.Add(stopOrderUpdatesSubResult.Message!);
            }
            
            var positionUpdatesSubResult = await _bybitUsdtPerpetualExchangeListener.SubscribeToPositionUpdatesAsync(account);
            if (!positionUpdatesSubResult.Success)
            {
                errors.Add(positionUpdatesSubResult.Message!);
            }
            
            var orderUpdatesSubResult = await _bybitUsdtPerpetualExchangeListener.SubscribeToOrderUpdatesAsync(account);
            if (!orderUpdatesSubResult.Success)
            {
                errors.Add(orderUpdatesSubResult.Message!);
            }

            if (errors.Count > 0)
            {
                return new ExchangeSubscriptionResult(string.Join("\n", errors));
            }

            return new ExchangeSubscriptionResult(true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in SubscribeToUpdatesAsync");
            return new ExchangeSubscriptionResult(e.Message);
        }
    }

    public async Task ProcessPendingUpdatesAsync(string orderId, bool isComplete)
    {
        try
        {
            await _bybitUsdtPerpetualExchangeListener.ProcessUserTradeUpdatesAsync(orderId, isComplete);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in ProcessPendingUpdatesAsync");
        }
    }

    #endregion
}
