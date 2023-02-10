using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalTrader.Common.Enums;
using SignalTrader.Common.Resources;
using SignalTrader.Positions.Services;

namespace SignalTrader.Positions.Controllers;

[Authorize]
[ApiController]
[Route("/positions")]
public class PositionsController : ControllerBase
{
    #region Members

    private readonly ILogger<PositionsController> _logger;
    private readonly IPositionsService _positionsService;

    #endregion

    #region Constructors

    public PositionsController(ILogger<PositionsController> logger, IPositionsService positionsService)
    {
        _logger = logger;
        _positionsService = positionsService;
    }

    #endregion

    #region Positions API

    [HttpGet]
    [Produces("application/json")]
    public async Task<IActionResult> GetPositionsAsync(long? accountId = null, SupportedExchange? exchange = null, string? quoteAsset = null, string? baseAsset = null, Direction? direction = null, PositionStatus? status = null)
    {
        try
        {
            var result = await _positionsService.GetPositionsAsync(accountId, exchange, quoteAsset, baseAsset, direction, status);
            if (result.Success)
            {
                return Ok(result.Positions);
            }

            return BadRequest(new ErrorResource(result.Message!));
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in GetPositionsAsync");
            return BadRequest(new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetPositionsAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }

    [HttpGet("{positionId:long}")]
    [Produces("application/json")]
    public async Task<IActionResult> GetPositionAsync(long positionId)
    {
        try
        {
            var result = await _positionsService.GetPositionAsync(positionId);
            if (result.Success)
            {
                return Ok(result.Position);
            }

            return NotFound(new ErrorResource(result.Message!));
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in GetPositionAsync");
            return BadRequest(new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetPositionAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }

    #endregion
}
