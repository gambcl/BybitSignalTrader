using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Common.Enums;

namespace SignalTrader.Data.Entities;

[Index(nameof(AccountId))]
[Index(nameof(Exchange))]
[Index(nameof(QuoteAsset))]
[Index(nameof(BaseAsset))]
[Index(nameof(Direction))]
[Index(nameof(Status))]
public class Position
{
    public Position()
    {
        CreatedUtcMillis = UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        LeverageType = LeverageType.Unspecified;
        Quantity = 0.0M;
        UnrealisedPnl = 0.0M;
        UnrealisedPnl = 0.0M;
        RealisedPnl = 0.0M;
        RealisedPnl = 0.0M;
        Status = PositionStatus.Created;
    }
    
    [Key]
    public long Id { get; set; }
    
    public long AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    [MinLength(1), MaxLength(50)]
    public SupportedExchange Exchange { get; set; }
    
    [MinLength(1), MaxLength(50)]
    public string QuoteAsset { get; set; } = null!;
        
    [MinLength(1), MaxLength(50)]
    public string BaseAsset { get; set; } = null!;
    
    public Direction Direction { get; set; }
    
    public decimal? LeverageMultiplier { get; set; }

    public LeverageType LeverageType { get; set; }
    
    public decimal Quantity { get; set; }
    
    public decimal UnrealisedPnl { get; set; }

    public decimal UnrealisedPnlPercent { get; set; }
    
    public decimal RealisedPnl { get; set; }

    public decimal RealisedPnlPercent { get; set; }
    
    public decimal? StopLoss { get; set; }
    
    public decimal? LiquidationPrice { get; set; }
    
    public PositionStatus Status { get; set; }

    public long CreatedUtcMillis { get; set; }

    [NotMapped]
    public string CreatedUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(CreatedUtcMillis).ToString("O");
    
    public long UpdatedUtcMillis { get; set; }

    [NotMapped]
    public string UpdatedUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(UpdatedUtcMillis).ToString("O");

    public long CompletedUtcMillis { get; set; }

    [NotMapped]
    public string CompletedUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(CompletedUtcMillis).ToString("O");

    [NotMapped]
    public bool IsComplete => (Status == PositionStatus.Closed) || (Status == PositionStatus.Liquidated) || (Status == PositionStatus.StopLoss);

    public List<Order> Orders { get; set; } = new();
}
