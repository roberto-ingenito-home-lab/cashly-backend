using System.Net;
using cashly.src.Data;
using cashly.src.Data.Entities;
using cashly.src.DTOs;
using cashly.src.Exceptions;
using cashly.src.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace cashly.src.Services.Implementations;

public class InvestmentService(AppDbContext context) : IInvestmentService
{
    private static void RecalculateAccountBalances(InvestmentAccount acc)
    {
        decimal transferred = 0;
        decimal currentBalance = 0;
        DateTime? latestDate = null;

        var orderedLogs = acc.Logs
            .OrderBy(l => l.LogDate.Date)
            .ThenBy(l => l.Id)
            .ToList();

        foreach (var log in orderedLogs)
        {
            switch (log.ActionType)
            {
                case InvestmentLogType.DEPOSIT:
                    transferred += log.Amount;
                    currentBalance += log.Amount;
                    break;
                case InvestmentLogType.WITHDRAWAL:
                    transferred = Math.Max(0, transferred - log.Amount);
                    currentBalance = Math.Max(0, currentBalance - log.Amount);
                    break;
                case InvestmentLogType.INTEREST:
                    currentBalance += log.Amount;
                    break;
                case InvestmentLogType.VALUE_SNAPSHOT:
                    currentBalance = log.Amount;
                    break;
            }
            latestDate = log.LogDate;
        }

        acc.CashTransferredNet = transferred;
        acc.CashCurrentBalance = currentBalance;
        acc.UpdatedAt = latestDate ?? DateTime.UtcNow;
    }

    private static InvestmentAccountDto MapToDto(InvestmentAccount acc)
    {
        RecalculateAccountBalances(acc);

        var totalInterest = acc.Logs
            .Where(l => l.ActionType == InvestmentLogType.INTEREST)
            .Sum(l => l.Amount);

        return new InvestmentAccountDto
        {
            Id = acc.Id,
            Name = acc.Name,
            ColorHex = acc.ColorHex,
            IconName = acc.IconName,
            UpdatedAt = acc.UpdatedAt,
            TotalTransferred = acc.CashTransferredNet,
            TotalCurrentValue = acc.CashCurrentBalance,
            TotalInterestCredited = totalInterest,
            TotalLogsCount = acc.Logs.Count,
        };
    }

    public async Task<List<InvestmentAccountDto>> GetAccountsAsync(int userId)
    {
        var accounts = await context.InvestmentAccounts
            .Where(a => a.UserId == userId)
            .Include(a => a.Logs)
            .OrderBy(a => a.Id)
            .ToListAsync();

        return accounts.Select(MapToDto).ToList();
    }

    public async Task<PagedInvestmentLogsDto> GetAccountLogsAsync(
        int accountId,
        int userId,
        int page = 1,
        int pageSize = 10
    )
    {
        var accountExists = await context.InvestmentAccounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId);

        if (!accountExists)
        {
            throw new AppException("Conto non trovato.", HttpStatusCode.NotFound);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = context.InvestmentLogs
            .Where(l => l.AccountId == accountId && l.UserId == userId);

        var totalCount = await baseQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        if (page > totalPages) page = totalPages;

        var items = await baseQuery
            .OrderByDescending(l => l.LogDate)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new InvestmentLogDto
            {
                Id = l.Id,
                AccountId = l.AccountId,
                ActionType = l.ActionType,
                Amount = l.Amount,
                LogDate = l.LogDate,
                Note = l.Note,
            })
            .ToListAsync();

        return new PagedInvestmentLogsDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<InvestmentAccountDto> CreateAccountAsync(CreateInvestmentAccountDto dto, int userId)
    {
        var now = DateTime.UtcNow;
        var currentVal = dto.InitialCurrentValue.HasValue && dto.InitialCurrentValue.Value >= 0
            ? dto.InitialCurrentValue.Value
            : dto.InitialDeposit;

        var acc = new InvestmentAccount
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            CashTransferredNet = dto.InitialDeposit,
            CashCurrentBalance = currentVal,
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#4a7063" : dto.ColorHex,
            IconName = string.IsNullOrWhiteSpace(dto.IconName) ? "account_balance" : dto.IconName,
            UpdatedAt = now,
        };

        context.InvestmentAccounts.Add(acc);
        await context.SaveChangesAsync();

        var initialLogs = new List<InvestmentLog>();
        if (dto.InitialDeposit > 0)
        {
            var depLog = new InvestmentLog
            {
                AccountId = acc.Id,
                UserId = userId,
                ActionType = InvestmentLogType.DEPOSIT,
                Amount = dto.InitialDeposit,
                LogDate = now,
                Note = "Versamento iniziale",
            };
            context.InvestmentLogs.Add(depLog);
            initialLogs.Add(depLog);
        }

        if (dto.InitialCurrentValue.HasValue && dto.InitialCurrentValue.Value != dto.InitialDeposit)
        {
            var snapLog = new InvestmentLog
            {
                AccountId = acc.Id,
                UserId = userId,
                ActionType = InvestmentLogType.VALUE_SNAPSHOT,
                Amount = dto.InitialCurrentValue.Value,
                LogDate = now,
                Note = "Valore iniziale conto",
            };
            context.InvestmentLogs.Add(snapLog);
            initialLogs.Add(snapLog);
        }

        if (initialLogs.Count > 0)
        {
            await context.SaveChangesAsync();
            acc.Logs = initialLogs;
            RecalculateAccountBalances(acc);
            await context.SaveChangesAsync();
        }

        return MapToDto(acc);
    }

    public async Task<InvestmentAccountDto> RecordOperationAsync(InvestmentCashOperationDto dto, int userId)
    {
        var acc = await context.InvestmentAccounts
            .Include(a => a.Logs)
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId)
            ?? throw new AppException("Conto non trovato.", HttpStatusCode.NotFound);

        var now = DateTime.UtcNow;

        var defaultNote = dto.OperationType switch
        {
            InvestmentLogType.DEPOSIT => "Versamento sul conto",
            InvestmentLogType.WITHDRAWAL => "Prelievo dal conto",
            InvestmentLogType.INTEREST => "Accredito interessi",
            InvestmentLogType.VALUE_SNAPSHOT => "Aggiornamento valore attuale",
            _ => "Operazione",
        };

        var log = new InvestmentLog
        {
            AccountId = acc.Id,
            UserId = userId,
            ActionType = dto.OperationType,
            Amount = dto.Amount,
            LogDate = dto.LogDate.HasValue
                ? DateTime.SpecifyKind(dto.LogDate.Value, DateTimeKind.Utc)
                : now,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? defaultNote : dto.Note.Trim(),
        };

        context.InvestmentLogs.Add(log);
        await context.SaveChangesAsync();

        RecalculateAccountBalances(acc);
        await context.SaveChangesAsync();

        return MapToDto(acc);
    }

    public async Task<InvestmentAccountDto> UpdateAccountValueAsync(
        int accountId,
        UpdateInvestmentAccountValueDto dto,
        int userId
    )
    {
        var acc = await context.InvestmentAccounts
            .Include(a => a.Logs)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId)
            ?? throw new AppException("Conto non trovato.", HttpStatusCode.NotFound);

        var now = DateTime.UtcNow;
        var log = new InvestmentLog
        {
            AccountId = acc.Id,
            UserId = userId,
            ActionType = InvestmentLogType.VALUE_SNAPSHOT,
            Amount = dto.CurrentValue,
            LogDate = dto.LogDate.HasValue
                ? DateTime.SpecifyKind(dto.LogDate.Value, DateTimeKind.Utc)
                : now,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? "Aggiornamento valore attuale" : dto.Note.Trim(),
        };

        context.InvestmentLogs.Add(log);
        await context.SaveChangesAsync();

        RecalculateAccountBalances(acc);
        await context.SaveChangesAsync();

        return MapToDto(acc);
    }

    public async Task<InvestmentAccountDto> UpdateLogAsync(
        int logId,
        UpdateInvestmentLogDto dto,
        int userId
    )
    {
        var log = await context.InvestmentLogs
            .FirstOrDefaultAsync(l => l.Id == logId && l.UserId == userId)
            ?? throw new AppException("Movimento non trovato.", HttpStatusCode.NotFound);

        log.ActionType = dto.ActionType;
        log.Amount = dto.Amount;
        if (dto.LogDate.HasValue)
        {
            log.LogDate = DateTime.SpecifyKind(dto.LogDate.Value, DateTimeKind.Utc);
        }
        log.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

        await context.SaveChangesAsync();

        var acc = await context.InvestmentAccounts
            .Include(a => a.Logs)
            .FirstAsync(a => a.Id == log.AccountId && a.UserId == userId);

        RecalculateAccountBalances(acc);
        await context.SaveChangesAsync();

        return MapToDto(acc);
    }

    public async Task DeleteLogAsync(int logId, int userId)
    {
        var log = await context.InvestmentLogs
            .FirstOrDefaultAsync(l => l.Id == logId && l.UserId == userId)
            ?? throw new AppException("Movimento non trovato.", HttpStatusCode.NotFound);

        var accountId = log.AccountId;
        context.InvestmentLogs.Remove(log);
        await context.SaveChangesAsync();

        var acc = await context.InvestmentAccounts
            .Include(a => a.Logs)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (acc != null)
        {
            RecalculateAccountBalances(acc);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteAccountAsync(int accountId, int userId)
    {
        var acc = await context.InvestmentAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId)
            ?? throw new AppException("Conto non trovato.", HttpStatusCode.NotFound);

        context.InvestmentAccounts.Remove(acc);
        await context.SaveChangesAsync();
    }
}
