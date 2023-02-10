using SignalTrader.Common.Enums;
using SignalTrader.Common.Models;

namespace SignalTrader.Positions.Models;

public class ProfitAndLossResult : ServiceResult
{
    public ProfitAndLossResult(bool success) : base(success)
    {
    }

    public ProfitAndLossResult(string message) : base(message)
    {
    }

    public SupportedExchange Exchange { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public string BaseAsset { get; set; } = null!;
    
    public decimal QuantityFilled { get; set; }
    public decimal UnrealisedPnl { get; set; }
    public decimal UnrealisedPnlPercent { get; set; }
    public decimal RealisedPnl { get; set; }
    public decimal RealisedPnlPercent { get; set; }
}
