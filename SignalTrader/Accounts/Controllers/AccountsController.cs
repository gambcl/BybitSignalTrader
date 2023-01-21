using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalTrader.Accounts.Resources;
using SignalTrader.Common.Resources;
using SignalTrader.Accounts.Services;

namespace SignalTrader.Accounts.Controllers;

[Authorize]
[ApiController]
[Route("/accounts")]
public class AccountsController : ControllerBase
{
    #region Members

    private readonly ILogger<AccountsController> _logger;
    private readonly IAccountsService _accountsService;

    #endregion

    #region Constructors

    public AccountsController(ILogger<AccountsController> logger, IAccountsService accountsService)
    {
        _logger = logger;
        _accountsService = accountsService;
    }

    #endregion

    #region Accounts API

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> CreateAccountAsync([FromBody] CreateAccountResource resource)
    {
        try
        {
            var account = await _accountsService.CreateAccountAsync(resource);
            var result = account.ToAccountResource();
            return Ok(result);
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in CreateAccountAsync");
            return BadRequest(new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in CreateAccountAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }

    [HttpGet]
    [Produces("application/json")]
    public async Task<IActionResult> GetAccountsAsync(bool includeBalances = false)
    {
        try
        {
            var accounts = await _accountsService.GetAccountsAsync();
            var result = accounts.Select(account => account.ToAccountResource()).ToList();
            if (includeBalances)
            {
                foreach (var accountResource in result)
                {
                    // Fetch balances and add to resource.
                    var balances = _accountsService.GetBalances(accountResource.Id);
                    Dictionary<string, AccountWalletBalanceResource> balancesResources = new();
                    foreach (var kv in balances)
                    {
                        balancesResources[kv.Key] = new AccountWalletBalanceResource
                        {
                            Asset = kv.Value.Asset,
                            WalletAmount = kv.Value.WalletBalance,
                            AvailableAmount = kv.Value.AvailableBalance,
                        };
                    }
                    accountResource.Balances = balancesResources;
                }
            }
            return Ok(result);
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in GetAccountsAsync");
            return BadRequest(new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetAccountsAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }

    [HttpGet("{id}")]
    [Produces("application/json")]
    public async Task<IActionResult> GetAccountAsync(int id, bool includeBalances = false)
    {
        try
        {
            var account = await _accountsService.GetAccountAsync(id);
            if (account != null)
            {
                var result = account.ToAccountResource();
                if (includeBalances)
                {
                    // Fetch balances and add to resource.
                    var balances = _accountsService.GetBalances(id);
                    Dictionary<string, AccountWalletBalanceResource> balancesResources = new();
                    foreach (var kv in balances)
                    {
                        balancesResources[kv.Key] = new AccountWalletBalanceResource
                        {
                            Asset = kv.Value.Asset,
                            WalletAmount = kv.Value.WalletBalance,
                            AvailableAmount = kv.Value.AvailableBalance,
                        };
                    }
                    result.Balances = balancesResources;
                }
                return Ok(result);
            }

            return NotFound(new ErrorResource($"Account {id} not found"));
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in GetAccountAsync");
            return BadRequest(new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in GetAccountAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }
    
    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateAccountAsync([FromBody] UpdateAccountResource resource)
    {
        try
        {
            var account = await _accountsService.UpdateAccountAsync(resource);
            var result = account.ToAccountResource();
            return Ok(result);
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in UpdateAccountAsync");
            return BadRequest(new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in UpdateAccountAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }

    [HttpDelete("{id}")]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteAccountAsync(int id)
    {
        try
        {
            var result = await _accountsService.DeleteAccountAsync(id);
            if (result)
            {
                return Ok();
            }

            return NotFound(new ErrorResource($"Account {id} not found"));
        }
        catch (ArgumentException ae)
        {
            _logger.LogError(ae, "Caught ArgumentException in DeleteAccountAsync");
            return BadRequest(new ErrorResource(ae.Message));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Caught Exception in DeleteAccountAsync");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResource(e.Message));
        }
    }
    
    #endregion
}
