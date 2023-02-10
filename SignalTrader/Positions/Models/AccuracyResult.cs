using SignalTrader.Common.Enums;
using SignalTrader.Common.Models;

namespace SignalTrader.Positions.Models;

public class AccuracyResult : ServiceResult
{
    public AccuracyResult(bool success) : base(success)
    {
    }

    public AccuracyResult(string message) : base(message)
    {
    }

    public SupportedExchange Exchange { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public string BaseAsset { get; set; } = null!;
    
    public decimal Accuracy { get; set; }
    public int NumberPositions { get; set; }
    public int NumberWinners { get; set; }
    public int NumberLosers { get; set; }
}
