using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SignalTrader.Telegram.Services;

public class TelegramService : ITelegramService, IDisposable
{
    #region Members

    private readonly ILogger<TelegramService> _logger;
    private readonly IConfiguration _configuration;
    private readonly CancellationTokenSource _cancellationTokenSource = new ();
    private readonly ITelegramBotClient? _telegramBotClient = null;
    private readonly long _chatId;

    #endregion

    #region Constructors

    public TelegramService(ILogger<TelegramService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var botToken = _configuration["Notifications:Telegram:BotToken"];
        if (!string.IsNullOrWhiteSpace(botToken) && long.TryParse(_configuration["Notifications:Telegram:ChatId"], out _chatId))
        {
            _telegramBotClient = new TelegramBotClient(botToken);
        }
    }

    #endregion

    #region ITelegramService

    public void StartBot()
    {
        if (_telegramBotClient != null)
        {
            _logger.LogDebug("Starting Telegram bot");
            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            _telegramBotClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: new ReceiverOptions()
                {
                    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
                },
                cancellationToken: _cancellationTokenSource.Token
            );
        }
    }

    public void StopBot()
    {
        if (_telegramBotClient != null)
        {
            _logger.LogDebug("Stopping Telegram bot");
            // Send cancellation request to stop bot.
            _cancellationTokenSource.Cancel();
        }
    }

    public async Task SendMessageNotificationAsync(string message)
    {
        if (_telegramBotClient != null)
        {
            await _telegramBotClient.SendTextMessageAsync(_chatId, message, ParseMode.MarkdownV2);
        }
    }

    public async Task SendStartupNotificationAsync()
    {
        if (_telegramBotClient != null)
        {
            var appName = Assembly.GetEntryAssembly()!.GetName().Name;
            var appVersion = Assembly.GetEntryAssembly()!.GetName().Version;
            await _telegramBotClient.SendTextMessageAsync(_chatId, $"*{appName} v{appVersion!.Major}\\.{appVersion!.Minor}\\.{appVersion.Build}* started", ParseMode.MarkdownV2);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    #endregion

    #region Private

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is null) return;

        // Security check - make sure message is from ourselves.
        if (update.Message.Chat.Id == _chatId)
        {
            if (update.Message.Type == MessageType.Text)
            {
                var messageText = update.Message.Text;
                if (!string.IsNullOrWhiteSpace(messageText))
                {
                    switch (messageText)
                    {
                        case "/help":
                            await HandleHelpCommand(botClient, update, cancellationToken);
                            break;
                        case "/balances":
                            await HandleBalancesCommand(botClient, update, cancellationToken);
                            break;
                        case "/bots":
                            await HandleBotsCommand(botClient, update, cancellationToken);
                            break;
                    }
                }
            }
        }
        else
        {
            await _telegramBotClient!.SendTextMessageAsync(update.Message.Chat.Id, "Unauthorized");
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(exception, errorMessage);
        return Task.CompletedTask;
    }

    private async Task HandleHelpCommand(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = "The following commands are supported:\n" +
                      "/help \\- Show this message\n" +
                      "/balances \\- Show account balances\n" +
                      "/bots \\- Show bots\n";
        await _telegramBotClient!.SendTextMessageAsync(_chatId, message, ParseMode.MarkdownV2);
    }

    private async Task HandleBalancesCommand(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // TODO: Show account balances
        var message = "TODO: Show account balances\n";
        await _telegramBotClient!.SendTextMessageAsync(_chatId, message, ParseMode.MarkdownV2);
    }

    private async Task HandleBotsCommand(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // TODO: Show bots
        var message = "TODO: Show bots\n";
        await _telegramBotClient!.SendTextMessageAsync(_chatId, message, ParseMode.MarkdownV2);
    }

    #endregion
}