using cashly.src.Data;
using cashly.src.Data.Entities;
using cashly.src.DTOs;
using cashly.src.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace cashly.src.Services.Implementations;

public class DashboardService(AppDbContext context) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId, int year, int month)
    {
        var data = new DashboardSummaryDto();

        var baseQuery = context.Transactions.Where(t => t.UserId == userId);

        data.FirstTransactionDate = await baseQuery.MinAsync(t => (DateTime?)t.TransactionDate);

        // 1. Cumulative Balance (All time)
        var dailyDeltas = await baseQuery
            .GroupBy(t => t.TransactionDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Delta = g.Sum(x => x.Type == TransactionType.income ? x.Amount : -x.Amount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        decimal running = 0;
        foreach (var d in dailyDeltas)
        {
            if (d.Delta == 0) continue;
            running += d.Delta;
            data.CumulativeBalance.Add(new CumulativeBalancePointDto
            {
                X = ((DateTimeOffset)DateTime.SpecifyKind(d.Date, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
                Y = running
            });
        }

        // 2. Expense Distribution (Selected Month)
        var monthStart = new DateTime(year, month + 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        data.ExpenseDistribution = await baseQuery
            .Where(t => t.Type == TransactionType.expense && t.TransactionDate >= monthStart && t.TransactionDate < monthEnd)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryExpenseDto
            {
                CategoryId = g.Key,
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        // 3. Daily Trends (Selected Month + 6 days before for moving average)
        var trendStart = monthStart.AddDays(-6);
        
        var dailyTrendsRaw = await baseQuery
            .Where(t => t.TransactionDate >= trendStart && t.TransactionDate < monthEnd)
            .GroupBy(t => t.TransactionDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Incomes = g.Sum(x => x.Type == TransactionType.income ? x.Amount : 0),
                Expenses = g.Sum(x => x.Type == TransactionType.expense ? x.Amount : 0)
            })
            .ToListAsync();

        // Riempi tutti i giorni richiesti (trendStart -> monthEnd - 1)
        var currentDay = trendStart;
        while (currentDay < monthEnd)
        {
            var matched = dailyTrendsRaw.FirstOrDefault(d => d.Date == currentDay);
            data.DailyTrends.Add(new DailyTrendPointDto
            {
                Timestamp = ((DateTimeOffset)DateTime.SpecifyKind(currentDay, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
                Incomes = matched?.Incomes ?? 0,
                Expenses = matched?.Expenses ?? 0
            });
            currentDay = currentDay.AddDays(1);
        }
        
        // Wait, returning the Day is not enough because days can wrap around months. Let's return the DayOfYear or just Timestamp, or let's change Day to Timestamp.
        // I will change Day to Timestamp in DTO.
        
        // 4. Yearly Overview
        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);

        var yearlyRaw = await baseQuery
            .Where(t => t.TransactionDate >= yearStart && t.TransactionDate < yearEnd)
            .GroupBy(t => t.TransactionDate.Month)
            .Select(g => new
            {
                Month = g.Key - 1, // 0-indexed per il frontend (Angular/JS month)
                Incomes = g.Sum(x => x.Type == TransactionType.income ? x.Amount : 0),
                Expenses = g.Sum(x => x.Type == TransactionType.expense ? x.Amount : 0)
            })
            .ToListAsync();

        for (int m = 0; m < 12; m++)
        {
            var matched = yearlyRaw.FirstOrDefault(x => x.Month == m);
            data.YearlyOverview.Add(new MonthlyOverviewPointDto
            {
                Month = m,
                Incomes = matched?.Incomes ?? 0,
                Expenses = matched?.Expenses ?? 0
            });
        }

        return data;
    }

    public async Task<List<CategoryExpenseDto>> GetCategoryBreakdownAsync(int userId, string filter)
    {
        var baseQuery = context.Transactions.Where(t => t.UserId == userId && t.Type == TransactionType.expense);
        
        var now = DateTime.UtcNow;
        var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        
        DateTime? startDate = filter switch
        {
            "current_month" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            "current_year" => new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "last_3_months" => new DateTime(now.AddMonths(-3).Year, now.AddMonths(-3).Month, 1, 0, 0, 0, DateTimeKind.Utc),
            "last_6_months" => new DateTime(now.AddMonths(-6).Year, now.AddMonths(-6).Month, 1, 0, 0, 0, DateTimeKind.Utc),
            "last_year" => new DateTime(now.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "all" => null,
            _ => null
        };

        if (startDate.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.TransactionDate >= startDate.Value);
        }

        return await baseQuery
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryExpenseDto
            {
                CategoryId = g.Key,
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync();
    }
}
