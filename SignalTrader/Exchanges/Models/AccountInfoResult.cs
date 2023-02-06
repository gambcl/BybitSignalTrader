using SignalTrader.Common.Enums;

namespace SignalTrader.Exchanges.Models;

public class AccountInfoResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public string? ExchangeAccountId { get; set; }
    public ExchangeType ExchangeType { get; set; }
}
