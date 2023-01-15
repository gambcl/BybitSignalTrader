using System.Reflection;
using SignalTrader.Telegram.Services;

namespace SignalTrader.Telegram.Workers;

public class TelegramWorker : IHostedService
{
    #region Members

    private readonly ILogger<TelegramWorker> _logger;
    private readonly ITelegramService _telegramService;

    #endregion

    #region Constructors

    public TelegramWorker(ILogger<TelegramWorker> logger, ITelegramService telegramService)
    {
        _logger = logger;
        _telegramService = telegramService;
    }

    #endregion

    #region IHostedService

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TelegramWorker starting");
        _telegramService.StartBot();

        // Send startup message.
        await _telegramService.SendStartupNotificationAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TelegramWorker stopping");
        _telegramService.StopBot();
        return Task.CompletedTask;
    }

    #endregion
}
