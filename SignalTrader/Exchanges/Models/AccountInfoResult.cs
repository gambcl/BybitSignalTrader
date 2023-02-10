using SignalTrader.Common.Enums;
using SignalTrader.Common.Models;

namespace SignalTrader.Exchanges.Models;

public class AccountInfoResult : ServiceResult
{
    public AccountInfoResult(bool success) : base(success)
    {
    }

    public AccountInfoResult(string message) : base(message)
    {
    }

    public string? ExchangeAccountId { get; set; }
    public ExchangeType ExchangeType { get; set; }
}
