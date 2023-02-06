using SignalTrader.Accounts.Services;

namespace SignalTrader.Accounts.Workers;

public class AccountsWorker : IHostedService, IDisposable
{
    #region Members

    private readonly ILogger<AccountsWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private Timer? _timerUpdateBalances;

    #endregion

    #region Constructors

    public AccountsWorker(ILogger<AccountsWorker> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _timerUpdateBalances?.Dispose();
    }

    #endregion
    
    #region IHostedService

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"AccountsWorker starting");

        var updateBalancesIntervalSeconds = _configuration.GetValue<int>("Accounts:UpdateBalancesIntervalSeconds");
        if (updateBalancesIntervalSeconds > 0)
        {
            _timerUpdateBalances = new Timer(DoUpdateBalancesWorkAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(updateBalancesIntervalSeconds));
        }
            
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("AccountsWorker stopping");
        _timerUpdateBalances?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    #endregion
    
    #region Private

    private async void DoUpdateBalancesWorkAsync(object? state)
    {
        try
        {
            using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                var accountsService = scope.ServiceProvider.GetRequiredService<IAccountsService>();
                await accountsService.UpdateAccountsAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught exception in AccountsWorker");
        }
    }

    #endregion
}
