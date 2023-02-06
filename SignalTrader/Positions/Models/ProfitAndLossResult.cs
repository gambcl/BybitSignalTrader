using SignalTrader.Common.Enums;

namespace SignalTrader.Positions.Models;

public class ProfitAndLossResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public SupportedExchange Exchange { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public string BaseAsset { get; set; } = null!;
    
    public decimal QuantityFilled { get; set; }
    public decimal UnrealisedPnl { get; set; }
    public decimal UnrealisedPnlPercent { get; set; }
    public decimal RealisedPnl { get; set; }
    public decimal RealisedPnlPercent { get; set; }
}
