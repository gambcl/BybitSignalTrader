using SignalTrader.Signals.Resources;

namespace SignalTrader.Signals.Services;

public class SignalsService : ISignalsService
{
    #region Members

    private readonly ILogger<SignalsService> _logger;

    #endregion

    #region Constructors

    public SignalsService(ILogger<SignalsService> logger)
    {
        _logger = logger;
    }

    #endregion

    #region ISignalsService

    public async Task ProcessTradingViewSignalsAsync(IList<TradingViewSignalResource> signals)
    {
    }

    #endregion
}
