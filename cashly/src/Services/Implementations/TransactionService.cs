using System.Net;
using cashly.src.Data;
using cashly.src.Data.Entities;
using cashly.src.DTOs;
using cashly.src.Exceptions;
using cashly.src.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace cashly.src.Services.Implementations;

public class TransactionService(AppDbContext context) : ITransactionService
{
    public async Task<Transaction> CreateTransactionAsync(TransactionCreateDto transactionDto, int userId)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 1. Verifica esistenza utente (Senza caricarlo tutto in memoria/tracking)
                if (!await context.Users.AnyAsync(u => u.UserId == userId))
                    throw new AppException("user-not-found", HttpStatusCode.NotFound);

                // 2. Verifica categoria
                if (
                    transactionDto.CategoryId is not null
                    && !await context.Categories.AnyAsync(c => c.CategoryId == transactionDto.CategoryId && c.UserId == userId)
                )
                    throw new AppException("category-not-found", HttpStatusCode.NotFound);

                // 3. Crea la transazione
                Transaction newTransaction = new()
                {
                    Amount = transactionDto.Amount,
                    Type = transactionDto.Type,
                    TransactionDate = transactionDto.TransactionDate.ToUniversalTime(),
                    Description = transactionDto.Description,
                    CategoryId = transactionDto.CategoryId,
                    UserId = userId,
                };

                // 4. Aggiorna il saldo in modo ATOMICO sul Database
                decimal balanceAdjustment = newTransaction.Type == TransactionType.income ? newTransaction.Amount : -newTransaction.Amount;

                await context
                    .Users.Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + balanceAdjustment));

                // 5. Salva la transazione
                context.Transactions.Add(newTransaction);
                await context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                return newTransaction;
            }
            catch (Exception)
            {
                // Se qualcosa va storto, annulla tutte le modifiche
                await dbTransaction.RollbackAsync();
                throw; // Rilancia l'eccezione per essere gestita a un livello superiore
            }
        });
    }

    public async Task<Transaction> UpdateTransactionAsync(int transactionId, TransactionUpdateDto transactionDto, int userId)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 1. Trova la transazione (Senza caricare l'utente per evitare conflitti di tracking sul saldo)
                var transaction =
                    await context.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == userId)
                    ?? throw new AppException("transaction-not-found", HttpStatusCode.NotFound);

                if (
                    transactionDto.CategoryId is not null
                    && !await context.Categories.AnyAsync(c => c.CategoryId == transactionDto.CategoryId && c.UserId == userId)
                )
                    throw new AppException("category-not-found", HttpStatusCode.NotFound);

                // 2. Calcola il DELTA per l'aggiornamento atomico
                // Es: Vecchia = -10 (Uscita), Nuova = -15 (Uscita) -> Delta = -5
                // Es: Vecchia = -10 (Uscita), Nuova = +20 (Entrata) -> Delta = +30
                decimal oldAdjustment = transaction.Type == TransactionType.income ? transaction.Amount : -transaction.Amount;
                decimal newAdjustment = transactionDto.Type == TransactionType.income ? transactionDto.Amount : -transactionDto.Amount;
                decimal delta = newAdjustment - oldAdjustment;

                // 3. Aggiorna i campi della transazione
                transaction.Amount = transactionDto.Amount;
                transaction.Type = transactionDto.Type;
                transaction.TransactionDate = transactionDto.TransactionDate.ToUniversalTime();
                transaction.Description = transactionDto.Description;
                transaction.CategoryId = transactionDto.CategoryId;

                // 4. Applica il delta al saldo in modo atomico
                if (delta != 0)
                {
                    await context
                        .Users.Where(u => u.UserId == userId)
                        .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + delta));
                }

                await context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                // Ricarica per restituire l'oggetto completo
                await context.Entry(transaction).Reference(t => t.Category).LoadAsync();
                return transaction;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByUserIdAsync(int userId)
    {
        return await context.Transactions.Where(t => t.UserId == userId).OrderByDescending(t => t.TransactionDate).ToListAsync();
    }

    public async Task DeleteTransaction(int id, int userId)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction = await context.Database.BeginTransactionAsync();

            try
            {
                var transaction =
                    await context.Transactions.FirstOrDefaultAsync(t => t.TransactionId == id && t.UserId == userId)
                    ?? throw new AppException("transaction-not-found", HttpStatusCode.NotFound);

                // 1. Revert del saldo in modo atomico
                decimal balanceRevert = transaction.Type == TransactionType.income ? -transaction.Amount : transaction.Amount;

                await context
                    .Users.Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + balanceRevert));

                // 2. Elimina la transazione
                context.Transactions.Remove(transaction);

                await context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        });
    }
}
