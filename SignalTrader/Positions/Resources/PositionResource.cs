using SignalTrader.Common.Enums;

namespace SignalTrader.Positions.Resources;

public class PositionResource
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string AccountName { get; set; } = null!;
    public SupportedExchange Exchange { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public string BaseAsset { get; set; } = null!;
    public Direction Direction { get; set; }
    public decimal? LeverageMultiplier { get; set; }
    public LeverageType? LeverageType { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnrealisedPnl { get; set; }
    public decimal? UnrealisedPnlPercent { get; set; }
    public decimal? RealisedPnl { get; set; }
    public decimal? RealisedPnlPercent { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? LiquidationPrice { get; set; }
    public PositionStatus Status { get; set; }
    public decimal? CreatedUtcMillis { get; set; }
    public decimal? UpdatedUtcMillis { get; set; }
    public decimal? CompletedUtcMillis { get; set; }
}
