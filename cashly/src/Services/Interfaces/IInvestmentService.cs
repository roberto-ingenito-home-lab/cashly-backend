using cashly.src.DTOs;

namespace cashly.src.Services.Interfaces;

public interface IInvestmentService
{
    Task<List<InvestmentAccountDto>> GetAccountsAsync(int userId);
    Task<PagedInvestmentLogsDto> GetAccountLogsAsync(int accountId, int userId, int page = 1, int pageSize = 10);
    Task<InvestmentAccountDto> CreateAccountAsync(CreateInvestmentAccountDto dto, int userId);
    Task<InvestmentAccountDto> UpdateAccountAsync(int accountId, UpdateInvestmentAccountDto dto, int userId);
    Task<InvestmentAccountDto> RecordOperationAsync(InvestmentCashOperationDto dto, int userId);
    Task<InvestmentAccountDto> UpdateAccountValueAsync(int accountId, UpdateInvestmentAccountValueDto dto, int userId);
    Task<InvestmentAccountDto> UpdateLogAsync(int logId, UpdateInvestmentLogDto dto, int userId);
    Task DeleteLogAsync(int logId, int userId);
    Task DeleteAccountAsync(int accountId, int userId);
}
