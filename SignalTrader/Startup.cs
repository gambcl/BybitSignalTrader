using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Antlr4.Runtime.Tree;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SignalTrader.Accounts.Services;
using SignalTrader.Accounts.Workers;
using SignalTrader.Authentication.Services;
using SignalTrader.Data;
using SignalTrader.Exchanges;
using SignalTrader.Exchanges.Bybit;
using SignalTrader.Signals.Services;
using SignalTrader.Signals.Workers;
using SignalTrader.Telegram.Services;
using SignalTrader.Telegram.Workers;

namespace SignalTrader;

public class Startup
{
    #region Constructors

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    #endregion

    #region Properties

    public IConfiguration Configuration { get; }

    #endregion

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Create the keyring directory if one doesn't exist.
        var keyRingDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%SignalTraderHome%"), "keys", "data-protection");
        Directory.CreateDirectory(keyRingDirectory);
        // Configure DataProtection.
        services.AddDataProtection()
            .SetApplicationName(Configuration["ApplicationProduct"])
            .PersistKeysToFileSystem(new DirectoryInfo(keyRingDirectory));

        // Configure EF Core.
        services.AddDbContext<SignalTraderDbContext>(options =>
        {
            options.UseNpgsql(BuildPostgreSqlConnectionString(), builder =>
                builder.MigrationsHistoryTable("__EFMigrationsHistory", SignalTraderDbContext.DefaultSchema));
        });
        
        // Configure authentication (using JWT).
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                RequireAudience = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = Configuration["Authentication:JwtIssuer"],
                ValidAudience = Configuration["Authentication:JwtAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Authentication:JwtSigningKey"]))
            };
        });

        // Configure MVC controllers.
        services.AddControllers(options =>
        {
            // Apply JWT authentication to all controllers.
            var policy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(new [] { JwtBearerDefaults.AuthenticationScheme })
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new AuthorizeFilter(policy));
        }).AddJsonOptions(options =>
        {
            // Configure JSON serialization.

            // Serialize enums as strings.
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Injecting service into another with different lifetime:
        // * Never inject Scoped & Transient services into Singleton services.
        //   (This effectively converts the transient or scoped service into the singleton.)
        // * Never inject Transient services into Scoped services.
        //   (This converts the transient service into the scoped.)
        
        // Configure scoped services.
        services.AddScoped<ISignalsService, SignalsService>();
        services.AddScoped<ISignalScriptService, SignalScriptService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAccountsService, AccountsService>();
        services.AddScoped<IExchangeProvider, ExchangeProvider>();
        services.AddScoped<IBybitUsdtPerpetualExchange, BybitUsdtPerpetualExchange>();

        // Configure singleton services.
        services.AddSingleton<ITelegramService, TelegramService>();
        services.AddSingleton<Channel<IParseTree>>(Channel.CreateUnbounded<IParseTree>());
        services.AddSingleton<ChannelWriter<IParseTree>>(svc => svc.GetRequiredService<Channel<IParseTree>>().Writer);
        services.AddSingleton<ChannelReader<IParseTree>>(svc => svc.GetRequiredService<Channel<IParseTree>>().Reader);

        // Configure background workers (effectively singletons).
        services.AddHostedService<TelegramWorker>();
        services.AddHostedService<AccountsWorker>();
        services.AddHostedService<SignalScriptWorker>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseForwardedHeaders();

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            // Map API controllers.
            endpoints.MapControllers();
        });
    }
    
    #region Private

    private string BuildPostgreSqlConnectionString()
    {
        var host = Configuration["Database:PostgreSQL:Host"];
        var port = Configuration["Database:PostgreSQL:Port"];
        var database = Configuration["Database:PostgreSQL:Database"];
        var user = Configuration["Database:PostgreSQL:User"];
        var password = Configuration["Database:PostgreSQL:Password"];
        var result = $"Host={host};Port={port};Database={database};Username={user};Password={password}";
        return result;
    }
        
    #endregion
}
