using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SignalTrader.Signals.Resources;

namespace SignalTrader.Ping.Controllers;

[ApiController]
public class PingController : ControllerBase
{
    #region Members

    private readonly ILogger<PingController> _logger;

    #endregion

    #region Constructors

    public PingController(ILogger<PingController> logger)
    {
        _logger = logger;
    }

    #endregion
    
    #region Ping API

    [HttpGet("ping")]
    [Produces("application/json")]
    public IActionResult Ping()
    {
        _logger.LogInformation("Received {Path} request from {IPAddress}", Request.Path, Request.HttpContext.Connection.RemoteIpAddress!.ToString() ?? "<unknown>");
        var serverTime = DateTime.UtcNow.ToString("O");
        var version = Assembly.GetEntryAssembly()!.GetName().Version!.ToString() ?? string.Empty;
        return Ok(new PingResource(serverTime, version));
    }

    #endregion
}
