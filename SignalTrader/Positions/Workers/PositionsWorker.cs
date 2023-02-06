using SignalTrader.Positions.Services;

namespace SignalTrader.Positions.Workers;

public class PositionsWorker : IHostedService, IDisposable
{
    #region Members

    private readonly ILogger<PositionsWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private Timer? _timerUpdatePositions;

    #endregion

    #region Constructors

    public PositionsWorker(ILogger<PositionsWorker> logger, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _timerUpdatePositions?.Dispose();
    }

    #endregion

    #region IHostedService

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"PositionsWorker starting");

        var updatePositionsIntervalSeconds = _configuration.GetValue<int>("Positions:UpdatePositionsIntervalSeconds");
        if (updatePositionsIntervalSeconds > 0)
        {
            _timerUpdatePositions = new Timer(DoUpdatePositionsWorkAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(updatePositionsIntervalSeconds));
        }
            
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("PositionsWorker stopping");
        _timerUpdatePositions?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    #endregion
    
    #region Private

    private async void DoUpdatePositionsWorkAsync(object? state)
    {
        try
        {
            using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                var positionsService = scope.ServiceProvider.GetRequiredService<IPositionsService>();
                await positionsService.UpdatePositionsAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught exception in PositionsWorker");
        }
    }

    #endregion
}
