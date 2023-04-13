namespace SignalTrader.Common.Enums;

public enum PositionStatus
{
    Created,
    Open,
    CloseInProgress,
    Closed,
    StopLossInProgress,
    StopLoss,
    LiquidatedInProgress,
    Liquidated
}
