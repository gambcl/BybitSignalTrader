using SignalTrader.Common.Enums;
using SignalTrader.Orders.Services;
using SignalTrader.Positions.Services;
using SignalTrader.Signals.Services;

namespace SignalTrader.Signals.SignalScript;

public class ExecutionVisitor : ValidationVisitor
{
    #region Constructors

    public ExecutionVisitor(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
    {
    }

    #endregion
    
    #region Protected

    protected override async Task ExecuteSignalFunctionAsync(SymbolScope symbolScope)
    {
        using var serviceScope = _serviceScopeFactory.CreateScope();
        var signalScriptService = serviceScope.ServiceProvider.GetRequiredService<ISignalScriptService>();
        
        var strategyName = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.StrategyName)?.Value.GetStringValue();
        var signalName = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.SignalName)?.Value.GetStringValue();
        var exchange = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.Exchange)?.Value.GetStringValue();
        var ticker = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.Ticker)?.Value.GetStringValue();
        var baseAsset = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.BaseAsset)?.Value.GetStringValue();
        var quoteAsset = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.QuoteAsset)?.Value.GetStringValue();
        var interval = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.Interval)?.Value.GetStringValue();
        var signalTime = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.SignalTime)?.Value.GetStringValue();
        var barTime = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.BarTime)?.Value.GetStringValue();
        var open = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.Open)?.Value.GetDecimalValue();
        var high = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.High)?.Value.GetDecimalValue();
        var low = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.Low)?.Value.GetDecimalValue();
        var close = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.Close)?.Value.GetDecimalValue();
        var volume = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.Volume)?.Value.GetDecimalValue();
        var passphrase = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.Passphrase)?.Value.GetStringValue();
        var longEnabled = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.LongEnabled)?.Value.GetBooleanValue();
        var shortEnabled = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.ShortEnabled)?.Value.GetBooleanValue();

        await signalScriptService.SignalReceivedAsync(signalTime,
            strategyName,
            signalName,
            exchange,
            ticker,
            quoteAsset,
            baseAsset,
            interval,
            barTime,
            open,
            high,
            low,
            close,
            volume,
            passphrase,
            longEnabled,
            shortEnabled);
    }

    protected override async Task ExecuteAccountFunctionAsync(string functionName, SymbolScope symbolScope)
    {
        using var serviceScope = _serviceScopeFactory.CreateScope();
        var positionsService = serviceScope.ServiceProvider.GetRequiredService<IPositionsService>();
        var ordersService = serviceScope.ServiceProvider.GetRequiredService<IOrdersService>();
        
        switch (functionName)
        {
            case Constants.FunctionNames.OpenPosition:
            {
                var accountId = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.AccountId)?.Value.GetIntValue();
                var baseAsset = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.BaseAsset)?.Value.GetStringValue();
                var quoteAsset = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.QuoteAsset)?.Value.GetStringValue();
                var leverageMultiplier = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Leverage)?.Value.GetDecimalValue();
                var leverageType = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.LeverageType)?.Value.GetLeverageValue();
                var direction = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Direction)?.Value.GetDirectionValue();
                var order = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Order)?.Value.GetOrderValue();
                var quantity = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Quantity)?.Value.GetDecimalValue();
                var costValue = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Cost)?.Value;
                var priceValue = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Price)?.Value;
                var offsetValue = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.Offset)?.Value;
                var stopLossValue = symbolScope.Resolve(Constants.OpenPositionFunction.ParameterNames.StopLoss)?.Value;

                var longEnabled = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.LongEnabled)?.Value;
                var shortEnabled = symbolScope.Resolve(Constants.SignalFunction.ParameterNames.ShortEnabled)?.Value;
                bool openPosition = true;
                if (direction.HasValue && direction == Direction.Long && longEnabled != null)
                {
                    openPosition = longEnabled.GetBooleanValue();
                }
                else if (direction.HasValue && direction == Direction.Short && shortEnabled != null)
                {
                    openPosition = shortEnabled.GetBooleanValue();
                }

                if (openPosition)
                {
                    await positionsService.OpenPositionAsync(accountId, quoteAsset, baseAsset, leverageMultiplier, leverageType, direction, order, quantity, costValue, priceValue, offsetValue, stopLossValue);
                }
                break;
            }
            
            case Constants.FunctionNames.ClosePosition:
            {
                var accountId = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.AccountId)?.Value.GetIntValue();
                var baseAsset = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.BaseAsset)?.Value.GetStringValue();
                var quoteAsset = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.QuoteAsset)?.Value.GetStringValue();

                var direction = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.Direction)?.Value.GetDirectionValue();
                var order = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.Order)?.Value.GetOrderValue();
                var priceValue = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.Price)?.Value;
                var offsetValue = symbolScope.Resolve(Constants.ClosePositionFunction.ParameterNames.Offset)?.Value;
                
                await positionsService.ClosePositionAsync(accountId, quoteAsset, baseAsset, direction, order, priceValue, offsetValue);
                break;
            }

            case Constants.FunctionNames.CancelOrders:
            {
                var accountId = symbolScope.Resolve(Constants.CancelOrdersFunction.ParameterNames.AccountId)?.Value.GetIntValue();
                var baseAsset = symbolScope.Resolve(Constants.CancelOrdersFunction.ParameterNames.BaseAsset)?.Value.GetStringValue();
                var quoteAsset = symbolScope.Resolve(Constants.CancelOrdersFunction.ParameterNames.QuoteAsset)?.Value.GetStringValue();
                var side = symbolScope.Resolve(Constants.CancelOrdersFunction.ParameterNames.Side)?.Value.GetSideValue();
                
                await ordersService.CancelOrdersAsync(accountId, quoteAsset, baseAsset, side);
                break;
            }
        }
    }

    #endregion
}
