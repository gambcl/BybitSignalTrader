using SignalTrader.Common.Enums;

namespace SignalTrader.Exchanges;

public interface IExchangeProvider
{
    IExchange? GetExchange(SupportedExchange exchange);
}
