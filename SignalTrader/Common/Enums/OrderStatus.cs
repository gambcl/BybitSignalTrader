namespace SignalTrader.Common.Enums;

public enum OrderStatus
{
    Created,
    Rejected,
    PartiallyFilled,
    Filled,
    CancelInProgress,
    Cancelled,
    CancelledPartiallyFilled
}
