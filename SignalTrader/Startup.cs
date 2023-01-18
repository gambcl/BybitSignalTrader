using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SignalTrader.Authentication.Services;
using SignalTrader.Signals.Services;
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
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Configure singleton services.
        services.AddSingleton<ITelegramService, TelegramService>();

        // Configure background workers (effectively singletons).
        services.AddHostedService<TelegramWorker>();
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
}
