using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SignalTrader.Data.Entities;

[Index(nameof(StrategyName))]
[Index(nameof(SignalName))]
[Index(nameof(QuoteAsset))]
[Index(nameof(BaseAsset))]
[Index(nameof(SignalTimeUtcMillis))]
[Index(nameof(Exchange))]
[Index(nameof(Ticker))]
[Index(nameof(Interval))]
[Index(nameof(BarTimeUtcMillis))]
public class Signal
{
    public Signal()
    {
        CreatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    
    [Key]
    public long Id { get; set; }

    public string StrategyName { get; set; } = null!;
    
    public string SignalName { get; set; } = null!;
    
    public string QuoteAsset { get; set; } = null!;
    
    public string BaseAsset { get; set; } = null!;
    
    public long SignalTimeUtcMillis { get; set; }
    
    [NotMapped]
    public string SignalTimeUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(SignalTimeUtcMillis).ToString("O");

    public string Exchange { get; set; } = null!;
    
    public string Ticker { get; set; } = null!;
    
    public string Interval { get; set; } = null!;
    
    public long BarTimeUtcMillis { get; set; }
    
    [NotMapped]
    public string BarTimeUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(BarTimeUtcMillis).ToString("O");

    public decimal Open { get; set; }
    
    public decimal High { get; set; }
    
    public decimal Low { get; set; }
    
    public decimal Close { get; set; }

    public decimal Volume { get; set; }
    
    public bool LongEnabled { get; set; }
    
    public bool ShortEnabled { get; set; }
    
    public long CreatedUtcMillis { get; set; }

    [NotMapped]
    public string CreatedUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(CreatedUtcMillis).ToString("O");
}
