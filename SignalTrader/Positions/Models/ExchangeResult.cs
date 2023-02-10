using SignalTrader.Common.Models;

namespace SignalTrader.Positions.Models;

public class ExchangeResult : ServiceResult
{
    public ExchangeResult(bool success) : base(success)
    {
    }

    public ExchangeResult(string message) : base(message)
    {
    }
}
