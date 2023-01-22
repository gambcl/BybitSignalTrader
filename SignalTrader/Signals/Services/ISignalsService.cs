namespace SignalTrader.Signals.Services;

public interface ISignalsService
{
    public Task ProcessTradingViewSignalAsync(string? body);
}
