using MediatR;
using SignalTrader.Data.Entities;

namespace SignalTrader.Positions.Notifications;

public class PositionStatusChangedNotification : INotification
{
    public Position Position { get; set; } = null!;
}
