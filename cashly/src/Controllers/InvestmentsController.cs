using cashly.src.DTOs;
using cashly.src.Extensions;
using cashly.src.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cashly.src.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class InvestmentsController(IInvestmentService investmentService) : ControllerBase
{
    [HttpGet("accounts")]
    public async Task<ActionResult<List<InvestmentAccountDto>>> GetAccounts()
    {
        var userId = User.GetUserId();
        var accounts = await investmentService.GetAccountsAsync(userId);
        return Ok(accounts);
    }

    [HttpGet("accounts/{accountId}/logs")]
    public async Task<ActionResult<PagedInvestmentLogsDto>> GetAccountLogs(
        int accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var userId = User.GetUserId();
        var logs = await investmentService.GetAccountLogsAsync(accountId, userId, page, pageSize);
        return Ok(logs);
    }

    [HttpPost("accounts")]
    public async Task<ActionResult<InvestmentAccountDto>> CreateAccount(
        [FromBody] CreateInvestmentAccountDto dto
    )
    {
        var userId = User.GetUserId();
        var account = await investmentService.CreateAccountAsync(dto, userId);
        return Ok(account);
    }

    [HttpPost("operations")]
    public async Task<ActionResult<InvestmentAccountDto>> RecordOperation(
        [FromBody] InvestmentCashOperationDto dto
    )
    {
        var userId = User.GetUserId();
        var account = await investmentService.RecordOperationAsync(dto, userId);
        return Ok(account);
    }

    [HttpPatch("accounts/{id}/value")]
    public async Task<ActionResult<InvestmentAccountDto>> UpdateAccountValue(
        int id,
        [FromBody] UpdateInvestmentAccountValueDto dto
    )
    {
        var userId = User.GetUserId();
        var account = await investmentService.UpdateAccountValueAsync(id, dto, userId);
        return Ok(account);
    }

    [HttpPut("logs/{id}")]
    public async Task<ActionResult<InvestmentAccountDto>> UpdateLog(
        int id,
        [FromBody] UpdateInvestmentLogDto dto
    )
    {
        var userId = User.GetUserId();
        var account = await investmentService.UpdateLogAsync(id, dto, userId);
        return Ok(account);
    }

    [HttpDelete("logs/{id}")]
    public async Task<IActionResult> DeleteLog(int id)
    {
        var userId = User.GetUserId();
        await investmentService.DeleteLogAsync(id, userId);
        return NoContent();
    }

    [HttpDelete("accounts/{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var userId = User.GetUserId();
        await investmentService.DeleteAccountAsync(id, userId);
        return NoContent();
    }
}
