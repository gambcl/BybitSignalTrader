using SignalTrader.Signals.Resources;
using SignalTrader.Telegram.Services;

namespace SignalTrader.Signals.Services;

public class SignalsService : ISignalsService
{
    #region Members

    private readonly ILogger<SignalsService> _logger;
    private readonly ITelegramService _telegramService;

    #endregion

    #region Constructors

    public SignalsService(ILogger<SignalsService> logger, ITelegramService telegramService)
    {
        _logger = logger;
        _telegramService = telegramService;
    }

    #endregion

    #region ISignalsService

    public async Task ProcessTradingViewSignalAsync(string? body)
    {
        _logger.LogInformation("Received TradingView webhook:\n{Body}", body);
        await _telegramService.SendMessageNotificationAsync("Received TradingView webhook");
    }

    #endregion
}
