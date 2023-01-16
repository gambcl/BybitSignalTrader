namespace SignalTrader.Telegram.Services;

public interface ITelegramService
{
    public void StartBot();
    public void StopBot();
    
    public Task SendMessageNotificationAsync(string message);
    public Task SendStartupNotificationAsync();
}
