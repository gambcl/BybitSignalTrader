using SignalTrader.Common.Enums;

namespace SignalTrader.Positions.Models;

public class PositionInfoResult : ExchangeResult
{
    public SupportedExchange Exchange { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public string BaseAsset { get; set; } = null!;
    public Direction? Direction { get; set; }
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal LeverageMultiplier { get; set; }
    public decimal? PositionMargin { get; set; }
    public decimal? LiquidationPrice { get; set; }
    public decimal? TakeProfit { get; set; }
    public decimal? StopLoss { get; set; }
    public string? Status { get; set; }
}
