using SignalTrader.Common.Models;
using SignalTrader.Positions.Resources;

namespace SignalTrader.Positions.Models;

public class PositionsResult : ServiceResult
{
    public PositionsResult(bool success) : base(success)
    {
    }

    public PositionsResult(string message) : base(message)
    {
    }

    public List<PositionResource> Positions { get; set; } = new();
}
