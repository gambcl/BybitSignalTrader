using Ardalis.GuardClauses;

namespace SignalTrader.Signals.Services;

public class SignalsService : ISignalsService
{
    #region Members

    private readonly ILogger<SignalsService> _logger;
    private readonly ISignalScriptService _signalScriptService;

    #endregion

    #region Constructors

    public SignalsService(ILogger<SignalsService> logger, ISignalScriptService signalScriptService)
    {
        _logger = logger;
        _signalScriptService = signalScriptService;
    }

    #endregion

    #region ISignalsService

    public async Task ProcessTradingViewSignalAsync(string? body)
    {
        Guard.Against.NullOrWhiteSpace(body, nameof(body));
        
        _logger.LogInformation("Received TradingView webhook:\n{Body}", body);
        await _signalScriptService.ProcessSignalScriptAsync(body);
    }

    #endregion
}
