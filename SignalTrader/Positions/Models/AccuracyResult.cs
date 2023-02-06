using SignalTrader.Common.Enums;

namespace SignalTrader.Positions.Models;

public class AccuracyResult
{
    public SupportedExchange Exchange { get; set; }
    public string QuoteAsset { get; set; } = null!;
    public string BaseAsset { get; set; } = null!;
    
    public decimal Accuracy { get; set; }
    public int NumberPositions { get; set; }
    public int NumberWinners { get; set; }
    public int NumberLosers { get; set; }
}
