using cashly.src.Data.Entities;
using cashly.src.DTOs;

namespace cashly.src.Services.Interfaces;

public interface ITransactionService
{
    Task<Transaction> CreateTransactionAsync(TransactionCreateDto transactionDto, int userId);
    Task<Transaction> UpdateTransactionAsync(int transactionId, TransactionUpdateDto transactionDto, int userId);
    Task<IEnumerable<Transaction>> GetTransactionsByUserIdAsync(int userId);
    Task DeleteTransaction(int id, int userId);
}
