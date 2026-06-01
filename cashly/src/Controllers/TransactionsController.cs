using cashly.src.Data.Entities;
using cashly.src.DTOs;
using cashly.src.Extensions;
using cashly.src.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cashly.src.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> CreateTransaction([FromBody] TransactionCreateDto createTransactionDto)
    {
        var userId = User.GetUserId();
        var newTransaction = await transactionService.CreateTransactionAsync(createTransactionDto, userId);

        return Ok(newTransaction.ToDto());
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactions()
    {
        var userId = User.GetUserId();
        var transactions = await transactionService.GetTransactionsByUserIdAsync(userId);
        var transactionResponse = transactions.Select(t => t.ToDto());

        return Ok(transactionResponse);
    }

    [HttpPut("{transactionId}")]
    public async Task<ActionResult<TransactionResponseDto>> UpdateTransaction(int transactionId, [FromBody] TransactionUpdateDto updateTransactionDto)
    {
        var userId = User.GetUserId();
        var updatedTransaction = await transactionService.UpdateTransactionAsync(transactionId, updateTransactionDto, userId);

        return Ok(updatedTransaction.ToDto()); // 200
    }

    [HttpDelete("{transactionId}")]
    public async Task<IActionResult> DeleteTransactions(int transactionId)
    {
        var userId = User.GetUserId();
        await transactionService.DeleteTransaction(transactionId, userId);

        return NoContent(); // 204 - Eliminazione avvenuta con successo
    }
}
