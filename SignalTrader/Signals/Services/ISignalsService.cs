using SignalTrader.Signals.Resources;

namespace SignalTrader.Signals.Services;

public interface ISignalsService
{
    public Task ProcessTradingViewSignalsAsync(IList<TradingViewSignalResource> signals);
}
