using System.Reflection;
using SignalTrader.Accounts.Services;
using SignalTrader.Common.Enums;
using SignalTrader.Common.Extensions;
using SignalTrader.Positions.Services;
using SignalTrader.Telegram.Extensions;
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
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _cancellationTokenSource = new ();
    private readonly ITelegramBotClient? _telegramBotClient;
    private readonly long _chatId;

    #endregion

    #region Constructors

    public TelegramService(ILogger<TelegramService> logger, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;

        var botToken = _configuration["User:Notifications:Telegram:BotToken"];
        if (!string.IsNullOrWhiteSpace(botToken) && long.TryParse(_configuration["User:Notifications:Telegram:ChatId"], out _chatId))
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

    public async Task SendMessageNotificationAsync(string? prefixEmoji, string? title, string? message, string? detail, string? suffixEmoji = null)
    {
        if (_telegramBotClient != null)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Title in *bold*
                parts.Add($"*{title.ToTelegramSafeString()}*");
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                // Plain message
                parts.Add(message.ToTelegramSafeString());
            }

            if (!string.IsNullOrWhiteSpace(detail))
            {
                // Detail in _italic_
                parts.Add($"_{detail.ToTelegramSafeString()}_");
            }

            // Add prefix/suffix emojis.
            prefixEmoji = string.IsNullOrWhiteSpace(prefixEmoji) ? string.Empty : prefixEmoji + " ";
            suffixEmoji = string.IsNullOrWhiteSpace(suffixEmoji) ? string.Empty : " " + suffixEmoji;
            string fullMessage = prefixEmoji + string.Join("\n", parts) + suffixEmoji;
            
            if (!string.IsNullOrWhiteSpace(fullMessage))
            {
                await _telegramBotClient.SendTextMessageAsync(_chatId, fullMessage, ParseMode.MarkdownV2);
            }
        }
    }

    public async Task SendStartupNotificationAsync()
    {
        if (_telegramBotClient != null)
        {
            var appName = Assembly.GetEntryAssembly()!.GetName().Name;
            var appVersion = Assembly.GetEntryAssembly()!.GetName().Version;

            await _telegramBotClient.SendTextMessageAsync(_chatId, $"{Constants.Emojis.TrafficLight} *{appName} v{appVersion!.Major}\\.{appVersion.Minor}\\.{appVersion.Build}* started", ParseMode.MarkdownV2);
        }
    }

    public async Task SendSignalReceivedNotificationAsync(string strategyName, string signalName, string exchange, string ticker, string interval)
    {
        if (_telegramBotClient != null)
        {
            await _telegramBotClient.SendTextMessageAsync(_chatId, $"{Constants.Emojis.SatelliteAntenna} Received *{strategyName.ToTelegramSafeString()} '{signalName.ToTelegramSafeString()}'* signal for *{exchange.ToTelegramSafeString()}:{ticker.ToTelegramSafeString()} {interval.ToTelegramSafeString()}*", ParseMode.MarkdownV2);
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
                        case "/positions":
                            await HandlePositionsCommand(botClient, update, cancellationToken);
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
                      "/help - Show this message\n" +
                      "/balances - Show account balances\n" +
                      "/positions - Show open positions\n";
        await botClient.SendTextMessageAsync(_chatId, message.ToTelegramSafeString(), ParseMode.MarkdownV2, cancellationToken:cancellationToken);
    }

    private async Task HandleBalancesCommand(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
        {
            var accountsService = scope.ServiceProvider.GetRequiredService<IAccountsService>();
            var message = string.Empty;
            var accounts = await accountsService.GetAccountsAsync();
            if (accounts.Count > 0)
            {
                var orderedAccounts = accounts
                    .OrderBy(a => a.Name)
                    .ThenBy(a => a.CreatedUtcMillis)
                    .ToList();
                
                foreach (var account in orderedAccounts)
                {
                    message += $"{Constants.Emojis.MoneyBag} *{account.Name.ToTelegramSafeString()}*\n";
                    var balances = accountsService.GetBalances(account.Id);
                    var balancesShown = false;
                    foreach (var kv in balances)
                    {
                        if (kv.Value.WalletBalance > 0 || kv.Value.AvailableBalance > 0)
                        {
                            message += $"_{kv.Value.Asset.ToTelegramSafeString()}:_\n";
                            message += "```\n";
                            message += $"Available: {kv.Value.AvailableBalance}\n".ToTelegramSafeString();
                            message += $"Wallet:    {kv.Value.WalletBalance}\n".ToTelegramSafeString();
                            message += "```\n";
                            balancesShown = true;
                        }
                    }
                    if (!balancesShown)
                    {
                        message += "No balances found!\n".ToTelegramSafeString();
                    }
                    message += "\n";
                }
            }
            else
            {
                message = "No accounts found!\n".ToTelegramSafeString();
            }
        
            await botClient.SendTextMessageAsync(_chatId, message, ParseMode.MarkdownV2, cancellationToken:cancellationToken);
        }
    }

    private async Task HandlePositionsCommand(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
        {
            var positionsService = scope.ServiceProvider.GetRequiredService<IPositionsService>();
            var message = string.Empty;
            var positionsResult = await positionsService.GetPositionsAsync(status:PositionStatus.Open);
            if (positionsResult.Success)
            {
                if (positionsResult.Positions.Count > 0)
                {
                    var orderedPositions = positionsResult.Positions
                        .OrderBy(p => p.AccountName)
                        .ThenBy(p => p.QuoteAsset)
                        .ThenBy(p => p.BaseAsset)
                        .ThenBy(p => p.CreatedUtcMillis)
                        .ToList();
                    
                    foreach (var position in orderedPositions)
                    {
                        var realisedSign = position.RealisedPnl >= 0.0M ? "+" : "-";
                        var realisedPnlEmoji = PositionsService.DeterminePnlEmoji(position.Status, position.RealisedPnlPercent);
                        var unrealisedSign = position.UnrealisedPnl >= 0.0M ? "+" : "-";
                        var unrealisedPnlEmoji = PositionsService.DeterminePnlEmoji(position.Status, position.UnrealisedPnlPercent);
                        message += $"{position.Direction.ToEmoji()} *{position.AccountName.ToTelegramSafeString()}*\n";
                        message += $"{position.Direction} position of {position.Quantity} {position.BaseAsset}{position.QuoteAsset} at {position.LeverageMultiplier:N2}x leverage\n".ToTelegramSafeString();
                        message += ("_" + $"Realised P&L: {realisedSign}{Math.Abs(position.RealisedPnl)} {position.QuoteAsset} ({realisedSign}{Math.Abs(position.RealisedPnlPercent)/100.0M:P2}) {realisedPnlEmoji}".ToTelegramSafeString() + "_\n");
                        message += ("_" + $"Unrealised P&L: {unrealisedSign}{Math.Abs(position.UnrealisedPnl)} {position.QuoteAsset} ({unrealisedSign}{Math.Abs(position.UnrealisedPnlPercent)/100.0M:P2}) {unrealisedPnlEmoji}".ToTelegramSafeString() + "_\n");
                        message += ("_" + $"Duration: {PositionsService.FormatDuration(position.CreatedUtcMillis ?? 0L, (position.CompletedUtcMillis.HasValue && position.CompletedUtcMillis > 0.0M ? position.CompletedUtcMillis.Value : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))}".ToTelegramSafeString() + "_\n");
                        message += "\n";
                    }
                }
                else
                {
                    message = "No open positions found!\n".ToTelegramSafeString();
                }
            }
            else
            {
                message = "Failed to fetch open positions!\n".ToTelegramSafeString();
            }
        
            await botClient.SendTextMessageAsync(_chatId, message, ParseMode.MarkdownV2, cancellationToken:cancellationToken);
        }
    }

    #endregion
}
