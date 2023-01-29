namespace SignalTrader.Telegram.Services;

public interface ITelegramService
{
    public void StartBot();
    public void StopBot();
    
    public Task SendMessageNotificationAsync(string message, string? emoji = null);
    public Task SendStartupNotificationAsync(string? emoji = null);
    public Task SendErrorNotificationAsync(string message, string? detail, string? emoji = null);
    public Task SendSignalReceivedNotificationAsync(string strategyName, string signalName, string exchange, string ticker, string interval, string? emoji = null);
}
