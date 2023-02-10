using SignalTrader.Common.Models;

namespace SignalTrader.Exchanges.Models;

public class ExchangeSubscriptionResult : ServiceResult
{
    public ExchangeSubscriptionResult(bool success) : base(success)
    {
    }

    public ExchangeSubscriptionResult(string message) : base(message)
    {
    }
}
