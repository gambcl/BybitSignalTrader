using System.Threading.Channels;
using Antlr4.Runtime.Tree;
using SignalTrader.Signals.SignalScript;

namespace SignalTrader.Signals.Workers;

public class SignalScriptWorker : BackgroundService
{
    #region Members

    private readonly ILogger<SignalScriptWorker> _logger;
    private readonly ChannelReader<IParseTree> _channelReader;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion

    #region Constructors

    public SignalScriptWorker(ILogger<SignalScriptWorker> logger, ChannelReader<IParseTree> channelReader, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _channelReader = channelReader;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion
    
    #region BackgroundService

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SignalScriptWorker starting");
        while (await _channelReader.WaitToReadAsync(stoppingToken))
        {
            if (_channelReader.TryRead(out var tree))
            {
                try
                {
                    _logger.LogInformation("Received SignalScript from Channel");
                    // Execute parser tree
                    _logger.LogDebug("Executing SignalScript");
                    var executor = new ExecutionVisitor(_serviceScopeFactory);
                    await executor.Visit(tree);
                    _logger.LogDebug("Finished executing SignalScript");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Caught Exception whilst executing SignalScript");
                }
            }
        }
        _logger.LogInformation("SignalScriptWorker stopping");
    }

    #endregion
}
