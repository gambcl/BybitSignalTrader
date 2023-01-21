using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SignalTrader.Common.Docker;
using SignalTrader.Data;

namespace SignalTrader;

public class Program
{
    private const string ConsoleOutputTemplate = "{Timestamp:HH:mm:ss} <{ThreadId}> {Level:u3} {Message:lj}{NewLine}{Exception}";
    
    public static int Main(string[] args)
    {
        try
        {
            // Bootstrap some Configuration so we can resolve paths.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .AddInMemoryCollection(GetApplicationProperties())
                .Build();
            ResolvePaths(configuration);
            
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.Console(outputTemplate:ConsoleOutputTemplate)
                .CreateBootstrapLogger();

            var appName = configuration["ApplicationProduct"];
            var appVersion = configuration["ApplicationVersion"];
            Log.Information($"{appName} v{appVersion} starting");
            
            Log.Information("Starting web host");
            var host = CreateHostBuilder(args).Build();
            RunDbMigrations(host);
            host.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.Sources.Clear();
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
                config.AddDockerSecrets();
                if (hostingContext.HostingEnvironment.IsDevelopment())
                {
                    config.AddUserSecrets<Startup>();
                }
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
                config.AddInMemoryCollection(GetApplicationProperties());
            })
            .UseSerilog((hostingContext, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .WriteTo.Console(outputTemplate: ConsoleOutputTemplate);
            }) 
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((context, options) =>
                {
                    var port = context.Configuration.GetValue<Int32>("Server:Port");
                    options.ListenAnyIP(port);
                });
                webBuilder.UseStartup<Startup>();
            });

    #region Private

    /// <summary>
    /// Retrieves various application properties, such as name, version.
    /// </summary>
    /// <returns>Dictionary containing application properties.</returns>
    /// <exception cref="ApplicationException">Failed to read a required application property.</exception>
    private static IDictionary<string, string> GetApplicationProperties()
    {
        IDictionary<string, string> result = new Dictionary<string, string>();

        string? product = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        if (string.IsNullOrEmpty(product))
        {
            throw new ApplicationException("Failed to read AssemblyProductAttribute");
        }
        result["ApplicationProduct"] = product ?? String.Empty;

        string? version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(version))
        {
            throw new ApplicationException("Failed to read AssemblyInformationalVersionAttribute");
        }
        result["ApplicationVersion"] = version ?? String.Empty;
            
        return result;
    }

    private static void ResolvePaths(IConfiguration configuration)
    {
        // Resolve paths for data, logs, reports.
        var homePath = configuration["SignalTraderHome"];
        if (string.IsNullOrWhiteSpace(homePath))
        {
            throw new ApplicationException("Undefined configuration value SignalTraderHome");
        }
        
        var keysPath = Path.Combine(homePath, "keys");
        if (!Directory.Exists(keysPath))
        {
            Directory.CreateDirectory(keysPath);
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
    }

    private static void RunDbMigrations(IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<SignalTraderDbContext>();
                    
                // Run migrations.
                context.Database.Migrate();

                // Seed data.
                EnsureSeedData(context);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"An error occurred running DB migrations: {ex.Message}");
            }
        }
    }
        
    /// <summary>
    /// Insert seed data into database.
    /// NOTE: This method should be re-runnable, to allow for new defaults to be added in the future without
    /// destroying existing values.
    /// </summary>
    private static void EnsureSeedData(SignalTraderDbContext context)
    {
    }

    #endregion
}
