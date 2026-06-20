namespace cashly.src.DTOs;

public class DashboardSummaryDto
{
    public DateTime? FirstTransactionDate { get; set; }
    public List<CumulativeBalancePointDto> CumulativeBalance { get; set; } = [];
    public List<CategoryExpenseDto> ExpenseDistribution { get; set; } = [];
    public List<DailyTrendPointDto> DailyTrends { get; set; } = [];
    public List<MonthlyOverviewPointDto> YearlyOverview { get; set; } = [];
}

public class CumulativeBalancePointDto
{
    public long X { get; set; } // timestamp
    public decimal Y { get; set; } // balance
}

public class CategoryExpenseDto
{
    public int? CategoryId { get; set; }
    public decimal Amount { get; set; }
}

public class DailyTrendPointDto
{
    public long Timestamp { get; set; }
    public decimal Incomes { get; set; }
    public decimal Expenses { get; set; }
}

public class MonthlyOverviewPointDto
{
    public int Month { get; set; }
    public decimal Incomes { get; set; }
    public decimal Expenses { get; set; }
}
