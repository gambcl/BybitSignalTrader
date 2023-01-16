using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SignalTrader.Signals.Services;
using SignalTrader.Telegram.Services;
using SignalTrader.Telegram.Workers;

const string consoleOutputTemplate = "{Timestamp:HH:mm:ss} <{ThreadId}> {Level:u3} {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:consoleOutputTemplate)
    .CreateBootstrapLogger();

try
{
    var appName = Assembly.GetEntryAssembly()!.GetName().Name;
    var appVersion = Assembly.GetEntryAssembly()!.GetName().Version;
    Log.Information($"{appName} v{appVersion!.ToString()} starting");
    
    Log.Information("Starting web application");
    var builder = WebApplication.CreateBuilder(args);

    // Resolve paths for data, logs, reports.
    var homePath = builder.Configuration["SignalTraderHome"];
    var dataPath = Path.Combine(homePath, "data");
    if (!Directory.Exists(dataPath))
    {
        Directory.CreateDirectory(dataPath);
    }
    var logsPath = Path.Combine(homePath, "logs");
    if (!Directory.Exists(logsPath))
    {
        Directory.CreateDirectory(logsPath);
    }
    var reportsPath = Path.Combine(homePath, "reports");
    if (!Directory.Exists(reportsPath))
    {
        Directory.CreateDirectory(reportsPath);
    }
    Environment.SetEnvironmentVariable("SignalTraderHome", homePath);

    // Configure logging using Serilog.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate:consoleOutputTemplate));

    // Configure JSON serialization.
    builder.Services.Configure<JsonOptions>(options =>
    {
        // Serialize enums as strings.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    // Add services to the container.
    builder.Services.AddScoped<ISignalsService, SignalsService>();
    builder.Services.AddSingleton<ITelegramService, TelegramService>();
    builder.Services.AddHostedService<TelegramWorker>();

    builder.Services.AddControllers();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
