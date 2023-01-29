using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalTrader.Common.Resources;
using SignalTrader.Signals.Services;
using SignalTrader.Signals.SignalScript.Exceptions;
using SignalTrader.Telegram.Services;

namespace SignalTrader.Signals.Controllers;

[AllowAnonymous]
[ApiController]
[Route("/signals")]
public class SignalsController : ControllerBase
{
    #region Members

    private readonly ILogger<SignalsController> _logger;
    private readonly ISignalsService _signalsService;
    private readonly ITelegramService _telegramService;

    #endregion

    #region Constructors

    public SignalsController(ILogger<SignalsController> logger, ISignalsService signalsService, ITelegramService telegramService)
    {
        _logger = logger;
        _signalsService = signalsService;
        _telegramService = telegramService;
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
        catch (SignalScriptSecurityException ex)
        {
            _logger.LogError(ex, "Caught SignalScriptSecurityException in TradingViewSignalAsync");
            await _telegramService.SendErrorNotificationAsync("Rejected TradingView webhook", ex.Message, Telegram.Constants.Emojis.MiddleFinger);
            return Unauthorized(new ErrorResource(ex.Message));
        }
        catch (SignalScriptExecutionException ex)
        {
            _logger.LogError(ex, "Caught SignalScriptExecutionException in TradingViewSignalAsync");
            await _telegramService.SendErrorNotificationAsync("Failed to process TradingView webhook", ex.Message, Telegram.Constants.Emojis.DoubleExclamation);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(ex.Message));
        }
        catch (SignalScriptValidationException ex)
        {
            _logger.LogError(ex, "Caught SignalScriptValidationException in TradingViewSignalAsync");
            await _telegramService.SendErrorNotificationAsync("Rejected TradingView webhook", ex.Message, Telegram.Constants.Emojis.Prohibited);
            return BadRequest(new ErrorResource(ex.Message));
        }
        catch (SignalScriptSyntaxException ex)
        {
            _logger.LogError(ex, "Caught SignalScriptSyntaxException in TradingViewSignalAsync");
            await _telegramService.SendErrorNotificationAsync("Rejected TradingView webhook", ex.Message, Telegram.Constants.Emojis.Prohibited);
            return BadRequest(new ErrorResource(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Caught ArgumentException in TradingViewSignalAsync");
            await _telegramService.SendErrorNotificationAsync("Rejected TradingView webhook", ex.Message, Telegram.Constants.Emojis.Prohibited);
            return BadRequest(new ErrorResource(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Caught Exception in TradingViewSignalAsync");
            await _telegramService.SendErrorNotificationAsync("Failed to process TradingView webhook", ex.Message, Telegram.Constants.Emojis.DoubleExclamation);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(ex.Message));
        }
    }

    #endregion
}
