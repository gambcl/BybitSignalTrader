using SignalTrader.Common.Enums;
using SignalTrader.Exchanges.Bybit;

namespace SignalTrader.Exchanges;

public class ExchangeProvider : IExchangeProvider
{
    #region Members

    private readonly ILogger<ExchangeProvider> _logger;
    private readonly IServiceProvider _serviceProvider;

    #endregion

    #region Constructors

    public ExchangeProvider(ILogger<ExchangeProvider> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    #endregion
    
    #region IExchangeProvider

    public IExchange? GetExchange(SupportedExchange exchange)
    {
        switch (exchange)
        {
            case SupportedExchange.BybitUSDTPerpetual:
                return _serviceProvider.GetService<IBybitUsdtPerpetualExchange>();
        }

        return null;
    }

    #endregion
}
