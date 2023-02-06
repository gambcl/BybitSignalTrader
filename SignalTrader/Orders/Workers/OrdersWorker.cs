using SignalTrader.Orders.Services;

namespace SignalTrader.Orders.Workers;

public class OrdersWorker : IHostedService, IDisposable
{
    #region Members

    private readonly ILogger<OrdersWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private Timer? _timerUpdateOrders;

    #endregion

    #region Constructors

    public OrdersWorker(ILogger<OrdersWorker> logger, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _timerUpdateOrders?.Dispose();
    }

    #endregion

    #region IHostedService

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"OrdersWorker starting");

        var updateOrdersIntervalSeconds = _configuration.GetValue<int>("Orders:UpdateOrdersIntervalSeconds");
        if (updateOrdersIntervalSeconds > 0)
        {
            _timerUpdateOrders = new Timer(DoUpdateOrdersWorkAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(updateOrdersIntervalSeconds));
        }
            
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("OrdersWorker stopping");
        _timerUpdateOrders?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    #endregion
    
    #region Private

    private async void DoUpdateOrdersWorkAsync(object? state)
    {
        try
        {
            using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                var ordersService = scope.ServiceProvider.GetRequiredService<IOrdersService>();
                await ordersService.UpdateOrdersAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught exception in OrdersWorker");
        }
    }
    
    #endregion
}
