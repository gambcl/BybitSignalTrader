using System.Globalization;
using System.Threading.Channels;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Ardalis.GuardClauses;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using SignalTrader.Accounts.Services;
using SignalTrader.Common.Enums;
using SignalTrader.Signals.Extensions;
using SignalTrader.Signals.SignalScript;
using SignalTrader.Signals.SignalScript.Exceptions;
using SignalTrader.Signals.SignalScript.Generated;
using SignalTrader.Telegram.Services;

namespace SignalTrader.Signals.Services;

public class SignalScriptService : ISignalScriptService
{
    #region Classes

    private class SignalRecord
    {
        public string? SignalTime { get; set; }
        public string? StrategyName{ get; set; }
        public string? SignalName { get; set; }
        public string? Exchange { get; set; }
        public string? Ticker { get; set; }
        public string? QuoteAsset { get; set; }
        public string? BaseAsset { get; set; }
        public string? Interval { get; set; }
        public string? BarTime { get; set; }
        public decimal? Open { get; set; }
        public decimal? High { get; set; }
        public decimal? Low { get; set; }
        public decimal? Close { get; set; }
        public decimal? Volume { get; set; }
        public bool? LongEnabled { get; set; }
        public bool? ShortEnabled { get; set; }
    }
    
    private class QuoteStringConverter : StringConverter
    {
        public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value == null)
                return "\"\"";

            return "\"" + ((string)value).Replace("\"", "\"\"") + "\"";
        }
    }

    #endregion

    #region Members

    private readonly ILogger<SignalScriptService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAccountsService _accountsService;
    private readonly ITelegramService _telegramService;
    private readonly ChannelWriter<IParseTree> _channelWriter;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion

    #region Constructors

    public SignalScriptService(ILogger<SignalScriptService> logger, IConfiguration configuration, IAccountsService accountsService, ITelegramService telegramService, ChannelWriter<IParseTree> channelWriter, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _accountsService = accountsService;
        _telegramService = telegramService;
        _channelWriter = channelWriter;
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion
    
    #region ISignalScriptService

    public async Task ProcessSignalScriptAsync(string script)
    {
        // Lexical analysis and build parser for SignalScript.
        ICharStream stream = CharStreams.fromString(script);
        ITokenSource lexer = new SignalScriptLexer(stream);
        ITokenStream tokens = new CommonTokenStream(lexer);
        SignalScriptParser parser = new SignalScriptParser(tokens);
        parser.BuildParseTree = true;
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new VerboseErrorListener());
        // Perform SignalScript parsing.
        IParseTree tree = parser.signal();
            
        // Validate parser tree
        var validator = new ValidationVisitor(_serviceScopeFactory);
        await validator.Visit(tree);
        
        // Hand off validated tree to the Channel for background execution.
        if (!_channelWriter.TryWrite(tree))
        {
            _logger.LogError("Failed writing to Channel");
            throw new SignalScriptExecutionException("Failed writing to Channel");
        }
    }

    public async Task<long?> ValidateAccountNameAsync(string? accountName)
    {
        if (!string.IsNullOrWhiteSpace(accountName))
        {
            var accounts = await _accountsService.GetAccountsAsync();
            var account = accounts.SingleOrDefault(a => a.Name == accountName);
            return account?.Id;
        }

        return null;
    }

    public bool ValidatePassphrase(string? passphrase)
    {
        var expectedPassphrase = _configuration["User:SignalsPassphrase"];
        if (!string.IsNullOrWhiteSpace(passphrase) && passphrase.Equals(expectedPassphrase))
        {
            return true;
        }

        return false;
    }

    public async Task SignalReceivedAsync(string? signalTime, string? strategyName, string? signalName, string? exchange, string? ticker, string? quoteAsset, string? baseAsset, string? interval, string? barTime, decimal? open, decimal? high, decimal? low, decimal? close, decimal? volume, string? passphrase, bool? longEnabled, bool? shortEnabled)
    {
        try
        {
            Guard.Against.NullOrWhiteSpace(signalTime, nameof(signalTime));
            Guard.Against.NullOrWhiteSpace(strategyName, nameof(strategyName));
            Guard.Against.NullOrWhiteSpace(signalName, nameof(signalName));
            Guard.Against.NullOrWhiteSpace(exchange, nameof(exchange));
            Guard.Against.NullOrWhiteSpace(ticker, nameof(ticker));
            Guard.Against.NullOrWhiteSpace(quoteAsset, nameof(quoteAsset));
            Guard.Against.NullOrWhiteSpace(baseAsset, nameof(baseAsset));
            Guard.Against.NullOrWhiteSpace(interval, nameof(interval));
            Guard.Against.NullOrWhiteSpace(barTime, nameof(barTime));
            Guard.Against.Null(open, nameof(open));
            Guard.Against.Null(high, nameof(high));
            Guard.Against.Null(low, nameof(low));
            Guard.Against.Null(close, nameof(close));
            Guard.Against.Null(volume, nameof(volume));
            Guard.Against.NullOrWhiteSpace(passphrase, nameof(passphrase));
            Guard.Against.Null(longEnabled, nameof(longEnabled));
            Guard.Against.Null(shortEnabled, nameof(shortEnabled));
            
            // Send Telegram notification to let user know we have received signal.
            await _telegramService.SendSignalReceivedNotificationAsync(strategyName, signalName, exchange, ticker, interval.ToTradingViewTimeframe(), Telegram.Constants.Emojis.SatelliteAntenna);
        
            // Archive signal to CSV file.
            var records = new List<SignalRecord>
            {
                new()
                {
                    SignalTime = signalTime,
                    StrategyName = strategyName,
                    SignalName = signalName,
                    Exchange = exchange,
                    Ticker = ticker,
                    QuoteAsset = quoteAsset,
                    BaseAsset = baseAsset,
                    Interval = interval,
                    BarTime = barTime,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,
                    LongEnabled = longEnabled,
                    ShortEnabled = shortEnabled
                }
            };
            var homePath = _configuration["SignalTraderHome"];
            var filename = $"{strategyName}_{exchange}_{quoteAsset}_{baseAsset}_{interval.ToTradingViewTimeframe()}.csv";
            var filePath = Path.Combine(homePath, "signals", filename);
            var fileExists = File.Exists(filePath);
            // Append to the file.
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header again if the file already exists.
                HasHeaderRecord = !fileExists,
                ShouldQuote = args =>
                {
                    // Quote string types.
                    if (args.FieldType == typeof(string))
                    {
                        return true;
                    }

                    return ConfigurationFunctions.ShouldQuote(args);
                }
            };
            await using var stream = File.Open(filePath, FileMode.Append);
            await using var writer = new StreamWriter(stream);
            await using var csv = new CsvWriter(writer, config);
            await csv.WriteRecordsAsync(records);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in SignalReceivedAsync");
        }
    }

    public Task CancelOrdersAsync(long accountId, string quoteAsset, string baseAsset, Side side)
    {
        // TODO: CancelOrders

        return Task.CompletedTask;
    }

    public Task OpenPositionAsync(long accountId, string quoteAsset, string baseAsset)
    {
        // TODO: OpenPosition

        return Task.CompletedTask;
    }

    public Task ClosePositionAsync(long accountId, string quoteAsset, string baseAsset)
    {
        // TODO: ClosePosition

        return Task.CompletedTask;
    }

    #endregion
}
