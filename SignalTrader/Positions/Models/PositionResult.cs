using SignalTrader.Common.Models;
using SignalTrader.Positions.Resources;

namespace SignalTrader.Positions.Models;

public class PositionResult : ServiceResult
{
    public PositionResult(bool success) : base(success)
    {
    }

    public PositionResult(string message) : base(message)
    {
    }

    public PositionResource? Position { get; set; }
}
