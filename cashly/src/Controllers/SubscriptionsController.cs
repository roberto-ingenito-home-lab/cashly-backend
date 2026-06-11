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
public class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SubscriptionResponseDto>> CreateSubscription([FromBody] SubscriptionCreateDto createSubscriptionDto)
    {
        var userId = User.GetUserId();
        var newSubscription = await subscriptionService.CreateSubscriptionAsync(createSubscriptionDto, userId);

        return Ok(newSubscription.ToDto());
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubscriptionResponseDto>>> GetSubscriptions()
    {
        var userId = User.GetUserId();
        var subscriptions = await subscriptionService.GetSubscriptionsByUserIdAsync(userId);
        var subscriptionsResponse = subscriptions.Select(s => s.ToDto());

        return Ok(subscriptionsResponse);
    }

    [HttpPut("{subscriptionId}")]
    public async Task<ActionResult<SubscriptionResponseDto>> UpdateSubscription(int subscriptionId, [FromBody] SubscriptionUpdateDto updateSubscriptionDto)
    {
        var userId = User.GetUserId();
        var updatedSubscription = await subscriptionService.UpdateSubscriptionAsync(subscriptionId, updateSubscriptionDto, userId);

        return Ok(updatedSubscription.ToDto());
    }

    [HttpDelete("{subscriptionId}")]
    public async Task<IActionResult> DeleteSubscription(int subscriptionId)
    {
        var userId = User.GetUserId();
        await subscriptionService.DeleteSubscriptionAsync(subscriptionId, userId);

        return NoContent();
    }

    [HttpPost("{subscriptionId}/post-transaction")]
    public async Task<ActionResult<TransactionResponseDto>> PostTransactionFromSubscription(int subscriptionId)
    {
        var userId = User.GetUserId();
        var transaction = await subscriptionService.PostTransactionFromSubscriptionAsync(subscriptionId, userId);

        return Ok(transaction.ToDto());
    }
}
