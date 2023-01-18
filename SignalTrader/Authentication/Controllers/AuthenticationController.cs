using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalTrader.Authentication.Resources;
using SignalTrader.Authentication.Services;
using SignalTrader.Common.Resources;

namespace SignalTrader.Authentication.Controllers;

[AllowAnonymous]
[ApiController]
[Route("/authenticate")]
public class AuthenticationController : ControllerBase
{
    #region Members

    private readonly ILogger<AuthenticationController> _logger;
    private readonly IAuthenticationService _authenticationService;

    #endregion

    #region Constructors

    public AuthenticationController(ILogger<AuthenticationController> logger, IAuthenticationService authenticationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
    }

    #endregion

    #region Authentication API

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public IActionResult Authenticate([FromBody] AuthenticateResource resource)
    {
        try
        {
            var token = _authenticationService.Authenticate(resource);
            if (!string.IsNullOrWhiteSpace(token))
            {
                _logger.LogInformation("Successful login");
                return Ok(new TokenResource(token));
            }

            _logger.LogWarning("Invalid password");
            return Unauthorized(new ErrorResource("Invalid password"));
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in Authenticate");
            return StatusCode(StatusCodes.Status400BadRequest, new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in Authenticate");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }

    #endregion
}
