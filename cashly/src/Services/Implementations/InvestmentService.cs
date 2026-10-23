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
            var tx = new Transaction
            {
                UserId = userId,
                Amount = dto.InitialDeposit,
                Type = TransactionType.transfer_out,
                TransactionDate = now,
                Description = $"Trasferimento iniziale verso {acc.Name}",
                CategoryId = null
            };
            context.Transactions.Add(tx);
            await context.SaveChangesAsync();

            await context.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance - dto.InitialDeposit));

            var depLog = new InvestmentLog
            {
                AccountId = acc.Id,
                UserId = userId,
                ActionType = InvestmentLogType.DEPOSIT,
                Amount = dto.InitialDeposit,
                LogDate = now,
                Note = "Versamento iniziale",
                TransactionId = tx.TransactionId,
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

    public async Task<InvestmentAccountDto> UpdateAccountAsync(int accountId, UpdateInvestmentAccountDto dto, int userId)
    {
        var acc = await context.InvestmentAccounts
            .Include(a => a.Logs)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId)
            ?? throw new AppException("Conto non trovato.", HttpStatusCode.NotFound);

        acc.Name = dto.Name.Trim();
        acc.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return MapToDto(acc);
    }

    public async Task<InvestmentAccountDto> RecordOperationAsync(InvestmentCashOperationDto dto, int userId)
    {
        var acc = await context.InvestmentAccounts
            .Include(a => a.Logs)
            .FirstOrDefaultAsync(a => a.Id == dto.AccountId && a.UserId == userId)
            ?? throw new AppException("Conto non trovato.", HttpStatusCode.NotFound);

        var now = DateTime.UtcNow;
        var opDate = dto.LogDate.HasValue
            ? DateTime.SpecifyKind(dto.LogDate.Value, DateTimeKind.Utc)
            : now;

        var defaultNote = dto.OperationType switch
        {
            InvestmentLogType.DEPOSIT => "Versamento sul conto",
            InvestmentLogType.WITHDRAWAL => "Prelievo dal conto",
            InvestmentLogType.INTEREST => "Accredito interessi",
            InvestmentLogType.VALUE_SNAPSHOT => "Aggiornamento valore attuale",
            _ => "Operazione",
        };

        var note = string.IsNullOrWhiteSpace(dto.Note) ? defaultNote : dto.Note.Trim();

        int? linkedTransactionId = null;

        // Se l'operazione è DEPOSIT (uscita dal conto corrente verso investimenti)
        // oppure WITHDRAWAL (rientro sul conto corrente da investimenti),
        // creiamo automaticamente la transazione collegata e aggiorniamo il saldo CurrentBalance dell'utente.
        if (dto.OperationType == InvestmentLogType.DEPOSIT)
        {
            var tx = new Transaction
            {
                UserId = userId,
                Amount = dto.Amount,
                Type = TransactionType.transfer_out,
                TransactionDate = opDate,
                Description = $"Trasferimento verso {acc.Name}" + (string.IsNullOrWhiteSpace(dto.Note) ? "" : $": {dto.Note.Trim()}"),
                CategoryId = null
            };
            context.Transactions.Add(tx);
            await context.SaveChangesAsync();
            linkedTransactionId = tx.TransactionId;

            // Riduce il saldo di cassa/conto corrente dell'utente
            await context.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance - dto.Amount));
        }
        else if (dto.OperationType == InvestmentLogType.WITHDRAWAL)
        {
            var tx = new Transaction
            {
                UserId = userId,
                Amount = dto.Amount,
                Type = TransactionType.transfer_in,
                TransactionDate = opDate,
                Description = $"Rientro da {acc.Name}" + (string.IsNullOrWhiteSpace(dto.Note) ? "" : $": {dto.Note.Trim()}"),
                CategoryId = null
            };
            context.Transactions.Add(tx);
            await context.SaveChangesAsync();
            linkedTransactionId = tx.TransactionId;

            // Incrementa il saldo di cassa/conto corrente dell'utente
            await context.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + dto.Amount));
        }

        var log = new InvestmentLog
        {
            AccountId = acc.Id,
            UserId = userId,
            ActionType = dto.OperationType,
            Amount = dto.Amount,
            LogDate = opDate,
            Note = note,
            TransactionId = linkedTransactionId,
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
            .Include(l => l.Account)
            .FirstOrDefaultAsync(l => l.Id == logId && l.UserId == userId)
            ?? throw new AppException("Movimento non trovato.", HttpStatusCode.NotFound);

        var newOpDate = dto.LogDate.HasValue
            ? DateTime.SpecifyKind(dto.LogDate.Value, DateTimeKind.Utc)
            : log.LogDate;

        // Se era collegata una transazione o se il nuovo tipo richiede una transazione
        if (log.TransactionId.HasValue)
        {
            var linkedTx = await context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == log.TransactionId.Value && t.UserId == userId);

            if (linkedTx != null)
            {
                // Revert del vecchio impatto della transazione sul CurrentBalance
                decimal revertDelta = linkedTx.Type switch
                {
                    TransactionType.income => -linkedTx.Amount,
                    TransactionType.transfer_in => -linkedTx.Amount,
                    TransactionType.expense => linkedTx.Amount,
                    TransactionType.transfer_out => linkedTx.Amount,
                    _ => 0
                };
                await context.Users.Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + revertDelta));

                // Se la nuova operazione è ancora DEPOSIT o WITHDRAWAL, aggiorniamo la transazione
                if (dto.ActionType == InvestmentLogType.DEPOSIT)
                {
                    linkedTx.Amount = dto.Amount;
                    linkedTx.Type = TransactionType.transfer_out;
                    linkedTx.TransactionDate = newOpDate;
                    linkedTx.Description = $"Trasferimento verso {log.Account.Name}" + (string.IsNullOrWhiteSpace(dto.Note) ? "" : $": {dto.Note.Trim()}");

                    // Applica nuovo delta (-Amount)
                    await context.Users.Where(u => u.UserId == userId)
                        .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance - dto.Amount));
                }
                else if (dto.ActionType == InvestmentLogType.WITHDRAWAL)
                {
                    linkedTx.Amount = dto.Amount;
                    linkedTx.Type = TransactionType.transfer_in;
                    linkedTx.TransactionDate = newOpDate;
                    linkedTx.Description = $"Rientro da {log.Account.Name}" + (string.IsNullOrWhiteSpace(dto.Note) ? "" : $": {dto.Note.Trim()}");

                    // Applica nuovo delta (+Amount)
                    await context.Users.Where(u => u.UserId == userId)
                        .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + dto.Amount));
                }
                else
                {
                    // Se è diventato INTEREST o VALUE_SNAPSHOT, la transazione non serve più
                    context.Transactions.Remove(linkedTx);
                    log.TransactionId = null;
                }
            }
        }
        else if (dto.ActionType == InvestmentLogType.DEPOSIT || dto.ActionType == InvestmentLogType.WITHDRAWAL)
        {
            // Non c'era transazione ma ora è diventato DEPOSIT o WITHDRAWAL -> creiamola
            var isDep = dto.ActionType == InvestmentLogType.DEPOSIT;
            var tx = new Transaction
            {
                UserId = userId,
                Amount = dto.Amount,
                Type = isDep ? TransactionType.transfer_out : TransactionType.transfer_in,
                TransactionDate = newOpDate,
                Description = (isDep ? $"Trasferimento verso {log.Account.Name}" : $"Rientro da {log.Account.Name}") + (string.IsNullOrWhiteSpace(dto.Note) ? "" : $": {dto.Note.Trim()}"),
                CategoryId = null
            };
            context.Transactions.Add(tx);
            await context.SaveChangesAsync();
            log.TransactionId = tx.TransactionId;

            decimal balChange = isDep ? -dto.Amount : dto.Amount;
            await context.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + balChange));
        }

        log.ActionType = dto.ActionType;
        log.Amount = dto.Amount;
        log.LogDate = newOpDate;
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

        // Se era collegata una transazione automatica, eliminiamola e ripristiniamo il saldo di cassa
        if (log.TransactionId.HasValue)
        {
            var linkedTxId = log.TransactionId.Value;
            // Sgancia prima la foreign key dal log per non violare ON DELETE RESTRICT
            log.TransactionId = null;
            await context.SaveChangesAsync();

            var linkedTx = await context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == linkedTxId && t.UserId == userId);

            if (linkedTx != null)
            {
                decimal revertDelta = linkedTx.Type switch
                {
                    TransactionType.income => -linkedTx.Amount,
                    TransactionType.transfer_in => -linkedTx.Amount,
                    TransactionType.expense => linkedTx.Amount,
                    TransactionType.transfer_out => linkedTx.Amount,
                    _ => 0
                };

                await context.Users.Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + revertDelta));

                context.Transactions.Remove(linkedTx);
                await context.SaveChangesAsync();
            }
        }

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
            .Include(a => a.Logs)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId)
            ?? throw new AppException("Conto non trovato.", HttpStatusCode.NotFound);

        // Se ci sono log con transazioni collegate, eliminiamo le transazioni e ripristiniamo il saldo utente
        var linkedTxIds = acc.Logs.Where(l => l.TransactionId.HasValue).Select(l => l.TransactionId!.Value).ToList();
        if (linkedTxIds.Count > 0)
        {
            var linkedTxs = await context.Transactions
                .Where(t => linkedTxIds.Contains(t.TransactionId) && t.UserId == userId)
                .ToListAsync();

            decimal totalRevert = 0;
            foreach (var tx in linkedTxs)
            {
                totalRevert += tx.Type switch
                {
                    TransactionType.income => -tx.Amount,
                    TransactionType.transfer_in => -tx.Amount,
                    TransactionType.expense => tx.Amount,
                    TransactionType.transfer_out => tx.Amount,
                    _ => 0
                };
            }

            if (totalRevert != 0)
            {
                await context.Users.Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.CurrentBalance, u => u.CurrentBalance + totalRevert));
            }

            // Sgancia i TransactionId dai log prima di rimuovere le transazioni per rispettare ON DELETE RESTRICT
            foreach (var log in acc.Logs.Where(l => l.TransactionId.HasValue))
            {
                log.TransactionId = null;
            }
            await context.SaveChangesAsync();

            context.Transactions.RemoveRange(linkedTxs);
        }

        context.InvestmentAccounts.Remove(acc);
        await context.SaveChangesAsync();
    }
}
