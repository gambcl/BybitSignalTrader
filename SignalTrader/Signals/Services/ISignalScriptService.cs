using SignalTrader.Common.Enums;

namespace SignalTrader.Signals.Services;

public interface ISignalScriptService
{
    public Task ProcessSignalScriptAsync(string script);
    
    public Task<long?> ValidateAccountNameAsync(string? accountName);
    public bool ValidatePassphrase(string? passphrase);

    public Task SignalReceivedAsync(string? signalTime, string? strategyName, string? signalName, string? exchange, string? ticker, string? quoteAsset, string? baseAsset, string? interval, string? barTime, decimal? open, decimal? high, decimal? low, decimal? close, decimal? volume, string? passphrase, bool? longEnabled, bool? shortEnabled);
    public Task CancelOrdersAsync(long accountId, string quoteAsset, string baseAsset, Side side);
    public Task OpenPositionAsync(long accountId, string quoteAsset, string baseAsset);
    public Task ClosePositionAsync(long accountId, string quoteAsset, string baseAsset);
}
