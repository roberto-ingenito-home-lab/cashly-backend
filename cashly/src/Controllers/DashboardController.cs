using cashly.src.DTOs;
using cashly.src.Services.Interfaces;
using cashly.src.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cashly.src.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary([FromQuery] int year, [FromQuery] int month)
    {
        var userId = User.GetUserId();
        var data = await dashboardService.GetDashboardSummaryAsync(userId, year, month);
        return Ok(data);
    }

    [HttpGet("category-breakdown")]
    public async Task<ActionResult<List<CategoryExpenseDto>>> GetCategoryBreakdown([FromQuery] string filter = "current_month")
    {
        var userId = User.GetUserId();
        var data = await dashboardService.GetCategoryBreakdownAsync(userId, filter);
        return Ok(data);
    }
}
