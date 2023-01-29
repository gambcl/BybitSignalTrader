using SignalTrader.Signals.Extensions;
using SignalTrader.Signals.Services;
using SignalTrader.Signals.SignalScript.Exceptions;
using SignalTrader.Telegram.Services;

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

    protected override Task ExecuteAccountFunctionAsync(string functionName, SymbolScope symbolScope)
    {
        using var serviceScope = _serviceScopeFactory.CreateScope();
        var signalScriptService = serviceScope.ServiceProvider.GetRequiredService<ISignalScriptService>();
        
        var accountId = symbolScope.Resolve(Constants.AccountFunction.ParameterNames.AccountId)?.Value.GetIntValue();
        var baseAsset = symbolScope.Resolve(Constants.AccountFunction.ParameterNames.BaseAsset)?.Value.GetStringValue();
        var quoteAsset = symbolScope.Resolve(Constants.AccountFunction.ParameterNames.QuoteAsset)?.Value.GetStringValue();

        Console.Out.WriteLine($"Executing function {functionName}()");
        return Task.CompletedTask;
    }

    #endregion
}
