using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalTrader.Common.Resources;
using SignalTrader.Signals.Services;

namespace SignalTrader.Signals.Controllers;

[AllowAnonymous]
[ApiController]
[Route("/signals")]
public class SignalsController : ControllerBase
{
    #region Members

    private readonly ILogger<SignalsController> _logger;
    private readonly ISignalsService _signalsService;

    #endregion

    #region Constructors

    public SignalsController(ILogger<SignalsController> logger, ISignalsService signalsService)
    {
        _logger = logger;
        _signalsService = signalsService;
    }

    #endregion

    #region Signals API

    [HttpPost("tradingview")]
    [Consumes("text/plain")]
    [Produces("application/json")]
    public async Task<IActionResult> TradingViewSignalAsync()
    {
        try
        {
            _logger.LogInformation("Received {Path} request from {IPAddress}", Request.Path, Request.HttpContext.Connection.RemoteIpAddress!.ToString() ?? "<unknown>");

            string? bodyContent;
            using (var reader = new StreamReader(Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
            {
                bodyContent = await reader.ReadToEndAsync();
            }

            await _signalsService.ProcessTradingViewSignalAsync(bodyContent);
            return Ok();
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in TradingViewSignalAsync");
            return BadRequest(new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in TradingViewSignalAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }

    #endregion
}
