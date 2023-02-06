using MediatR;
using SignalTrader.Data.Entities;

namespace SignalTrader.Orders.Notifications;

public class OrderStatusChangedNotification : INotification
{
    public Order Order { get; set; } = null!;
}
