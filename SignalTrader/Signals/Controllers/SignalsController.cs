using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalTrader.Common.Resources;
using SignalTrader.Signals.Resources;
using SignalTrader.Signals.Services;
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
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> TradingViewSignalAsync()
    {
        try
        {
            _logger.LogInformation("Received {Path} request from {IPAddress}", Request.Path, Request.HttpContext.Connection.RemoteIpAddress!.ToString() ?? "<unknown>");

            List<TradingViewSignalResource> signals = new ();

            // Read raw body content and parse manually because we want to accept EITHER:
            // - list of TradingViewSignalResource
            // - a single TradingViewSignalResource
            var rawContent = string.Empty;
            using (var reader = new StreamReader(Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
            {
                rawContent = await reader.ReadToEndAsync();
            }

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _logger.LogDebug("Received TradingView webhook:\n{Body}", rawContent);
            await _telegramService.SendMessageNotificationAsync("Received TradingView webhook");

            // First try to parse as list of TradingViewSignalResource.
            try
            {
                var bodySignals = JsonSerializer.Deserialize<List<TradingViewSignalResource>>(rawContent, jsonSerializerOptions);
                if (bodySignals != null && bodySignals.Count > 0)
                {
                    signals.AddRange(bodySignals);
                }
            }
            catch (JsonException)
            {
                // Swallow.
            }

            // Then try to parse as a single TradingViewSignalResource.
            try
            {
                var bodySignal = JsonSerializer.Deserialize<TradingViewSignalResource>(rawContent, jsonSerializerOptions);
                if (bodySignal != null)
                {
                    signals.Add(bodySignal);
                }
            }
            catch (JsonException)
            {
                // Swallow.
            }

            if (signals.Count == 0)
            {
                throw new ArgumentException("Invalid JSON body");
            }
            
            await _signalsService.ProcessTradingViewSignalsAsync(signals);
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
