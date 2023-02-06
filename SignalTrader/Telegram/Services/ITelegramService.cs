namespace SignalTrader.Telegram.Services;

public interface ITelegramService
{
    public void StartBot();
    public void StopBot();
    
    public Task SendMessageNotificationAsync(string? prefixEmoji, string? title, string? message, string? detail = null, string? suffixEmoji = null);
    public Task SendStartupNotificationAsync();
    public Task SendSignalReceivedNotificationAsync(string strategyName, string signalName, string exchange, string ticker, string interval);
}
