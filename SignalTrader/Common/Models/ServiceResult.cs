namespace SignalTrader.Common.Models;

public class ServiceResult
{
    public ServiceResult(bool success)
    {
        Success = success;
    }
    
    public ServiceResult(string message)
    {
        Success = false;
        Message = message;
    }
    
    public bool Success { get; }
    public string? Message { get; }
}
