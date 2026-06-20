using cashly.src.DTOs;

namespace cashly.src.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId, int year, int month);
    Task<List<CategoryExpenseDto>> GetCategoryBreakdownAsync(int userId, string filter);
}
