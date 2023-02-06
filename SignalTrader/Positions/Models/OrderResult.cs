using SignalTrader.Common.Enums;

namespace SignalTrader.Positions.Models;

public class OrderResult : ExchangeResult
{
    public SupportedExchange Exchange { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public Side Side { get; set; }
    public OrderType Type { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityFilled { get; set; }
    public OrderStatus Status { get; set; }
    public decimal? TakeProfit { get; set; }
    public decimal? StopLoss { get; set; }
    public bool? ReduceOnly { get; set; }
    public bool? CloseOnTrigger { get; set; }
}
