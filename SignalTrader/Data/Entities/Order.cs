using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SignalTrader.Common.Enums;

namespace SignalTrader.Data.Entities;

[Index(nameof(AccountId))]
[Index(nameof(Exchange))]
[Index(nameof(QuoteAsset))]
[Index(nameof(BaseAsset))]
[Index(nameof(ExchangeOrderId))]
[Index(nameof(Side))]
[Index(nameof(Status))]
[Index(nameof(PositionId))]
public class Order
{
    public Order()
    {
        Status = OrderStatus.Created;
        QuantityFilled = 0M;
        CreatedUtcMillis = UpdatedUtcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
    
    public string? ExchangeOrderId { get; set; }
    
    public Side Side { get; set; }
    
    public OrderType Type { get; set; }
    
    public decimal? Price { get; set; }
    
    public decimal Quantity { get; set; }
    
    public decimal QuantityFilled { get; set; }
    
    public OrderStatus Status { get; set; }
    
    public decimal? TakeProfit { get; set; }
    
    public decimal? StopLoss { get; set; }
    
    public bool? ReduceOnly { get; set; }

    public long PositionId { get; set; }
    public Position Position { get; set; } = null!;
    
    public long CreatedUtcMillis { get; set; }

    [NotMapped]
    public string CreatedUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(CreatedUtcMillis).ToString("O");
    
    public long UpdatedUtcMillis { get; set; }

    [NotMapped]
    public string UpdatedUtcIso8601 => DateTimeOffset.FromUnixTimeMilliseconds(UpdatedUtcMillis).ToString("O");

    [NotMapped]
    public bool IsComplete => (Status == OrderStatus.Rejected) || (Status == OrderStatus.Filled) || (Status == OrderStatus.CancelledPartiallyFilled) || (Status == OrderStatus.Cancelled);
}
