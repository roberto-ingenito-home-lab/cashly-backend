using cashly.src.Data.Entities;
using cashly.src.DTOs;

namespace cashly.src.Services.Interfaces;

public interface ISubscriptionService
{
    Task<Subscription> CreateSubscriptionAsync(SubscriptionCreateDto dto, int userId);
    Task<Subscription> UpdateSubscriptionAsync(
        int subscriptionId,
        SubscriptionUpdateDto dto,
        int userId
    );
    Task DeleteSubscriptionAsync(int subscriptionId, int userId);
    Task<IEnumerable<Subscription>> GetSubscriptionsByUserIdAsync(int userId);
    Task<Transaction> PostTransactionFromSubscriptionAsync(int subscriptionId, int userId);
}
