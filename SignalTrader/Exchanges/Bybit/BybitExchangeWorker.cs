namespace SignalTrader.Exchanges.Bybit;

public class BybitExchangeWorker : IHostedService, IDisposable
{
    #region Members

    private readonly ILogger<BybitExchangeWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private Timer? _timerUpdateSymbolInfo;

    #endregion

    #region Constructors

    public BybitExchangeWorker(ILogger<BybitExchangeWorker> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
    }

    #endregion
    
    #region IDisposable

    public void Dispose()
    {
        _timerUpdateSymbolInfo?.Dispose();
    }

    #endregion

    #region IHostedService

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"BybitExchangeWorker starting");

        var updateSymbolInfoIntervalSeconds = _configuration.GetValue<int>("Exchanges:Bybit:UpdateSymbolInfoIntervalSeconds");
        if (updateSymbolInfoIntervalSeconds > 0)
        {
            _timerUpdateSymbolInfo = new Timer(DoUpdateSymbolInfoWorkAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(updateSymbolInfoIntervalSeconds));
        }
            
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("BybitExchangeWorker stopping");
        _timerUpdateSymbolInfo?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    #endregion
    
    #region Private

    private async void DoUpdateSymbolInfoWorkAsync(object? state)
    {
        try
        {
            using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                var bybitExchange = scope.ServiceProvider.GetRequiredService<IBybitUsdtPerpetualExchange>();
                await bybitExchange.UpdateSymbolInfoAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught exception in BybitExchangeWorker");
        }
    }

    #endregion
}
