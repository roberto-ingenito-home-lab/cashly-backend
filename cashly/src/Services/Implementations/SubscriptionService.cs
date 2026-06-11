using System.Net;
using cashly.src.Data;
using cashly.src.Data.Entities;
using cashly.src.DTOs;
using cashly.src.Exceptions;
using cashly.src.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace cashly.src.Services.Implementations;

public class SubscriptionService(AppDbContext context) : ISubscriptionService
{
    public async Task<Subscription> CreateSubscriptionAsync(SubscriptionCreateDto dto, int userId)
    {
        // 1. Verifica esistenza utente
        if (!await context.Users.AnyAsync(u => u.UserId == userId))
            throw new AppException("user-not-found", HttpStatusCode.NotFound);

        // 2. Verifica categoria se inserita
        if (
            dto.CategoryId is not null
            && !await context.Categories.AnyAsync(c =>
                c.CategoryId == dto.CategoryId && c.UserId == userId
            )
        )
        {
            throw new AppException("category-not-found", HttpStatusCode.NotFound);
        }

        Subscription newSubscription = new()
        {
            Name = dto.Name,
            Amount = dto.Amount,
            Type = dto.Type,
            Frequency = dto.Frequency,
            StartDate = dto.StartDate?.ToUniversalTime(),
            EndDate = dto.EndDate?.ToUniversalTime(),
            CategoryId = dto.CategoryId,
            UserId = userId,
        };

        context.Subscriptions.Add(newSubscription);
        await context.SaveChangesAsync();

        // Ricarica la categoria se presente
        if (newSubscription.CategoryId is not null)
        {
            await context.Entry(newSubscription).Reference(s => s.Category).LoadAsync();
        }

        return newSubscription;
    }

    public async Task<Subscription> UpdateSubscriptionAsync(
        int subscriptionId,
        SubscriptionUpdateDto dto,
        int userId
    )
    {
        var subscription =
            await context.Subscriptions.FirstOrDefaultAsync(s =>
                s.SubscriptionId == subscriptionId && s.UserId == userId
            ) ?? throw new AppException("subscription-not-found", HttpStatusCode.NotFound);

        // Verifica categoria se inserita
        if (
            dto.CategoryId is not null
            && !await context.Categories.AnyAsync(c =>
                c.CategoryId == dto.CategoryId && c.UserId == userId
            )
        )
        {
            throw new AppException("category-not-found", HttpStatusCode.NotFound);
        }

        subscription.Name = dto.Name;
        subscription.Amount = dto.Amount;
        subscription.Type = dto.Type;
        subscription.Frequency = dto.Frequency;
        subscription.StartDate = dto.StartDate?.ToUniversalTime();
        subscription.EndDate = dto.EndDate?.ToUniversalTime();
        subscription.CategoryId = dto.CategoryId;

        await context.SaveChangesAsync();

        if (subscription.CategoryId is not null)
        {
            await context.Entry(subscription).Reference(s => s.Category).LoadAsync();
        }

        return subscription;
    }

    public async Task DeleteSubscriptionAsync(int subscriptionId, int userId)
    {
        var subscription =
            await context.Subscriptions.FirstOrDefaultAsync(s =>
                s.SubscriptionId == subscriptionId && s.UserId == userId
            ) ?? throw new AppException("subscription-not-found", HttpStatusCode.NotFound);

        context.Subscriptions.Remove(subscription);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsByUserIdAsync(int userId)
    {
        return await context
            .Subscriptions.Include(s => s.Category)
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Transaction> PostTransactionFromSubscriptionAsync(
        int subscriptionId,
        int userId
    )
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction = await context.Database.BeginTransactionAsync();
            try
            {
                // 1. Trova l'abbonamento
                var subscription =
                    await context.Subscriptions.FirstOrDefaultAsync(s =>
                        s.SubscriptionId == subscriptionId && s.UserId == userId
                    ) ?? throw new AppException("subscription-not-found", HttpStatusCode.NotFound);

                // 2. Crea la transazione finanziaria
                Transaction transaction = new()
                {
                    Amount = subscription.Amount,
                    Type = subscription.Type,
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Pagamento abbonamento: {subscription.Name}",
                    CategoryId = subscription.CategoryId,
                    UserId = userId,
                };
                context.Transactions.Add(transaction);

                // 3. Aggiorna saldo utente
                decimal adjustment =
                    transaction.Type == TransactionType.income
                        ? transaction.Amount
                        : -transaction.Amount;
                await context
                    .Users.Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + adjustment)
                    );

                // 4. Aggiorna data di ultimo pagamento
                subscription.LastPaymentDate = DateTime.UtcNow;

                await context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                // Carica categoria se presente
                if (transaction.CategoryId is not null)
                {
                    await context.Entry(transaction).Reference(t => t.Category).LoadAsync();
                }

                return transaction;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        });
    }
}
