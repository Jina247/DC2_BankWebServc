using DataWebAPI.Models;

namespace DataWebAPI.Services
{
    public interface IAccountService
    {
        Task<TransactionResult> DepositAsync(DepositRequest req);
        Task<TransactionResult> WithdrawAsync(WithdrawRequest req);
        Task<TransactionResult> TransferAsync(TransferRequest req);
        Task<AccountDto> CreateAccountAsync(CreateAccountRequest req);
        Task<AccountDto> GetAccountAsync(string accountNumber);
        Task UpdateAccountAsync(string accountNumber, UpdateAccountRequest req);
        Task DeactivateAccountAsync(string accountNumber);
        Task<PagedResult<TransactionResult>> SearchTransactionsAsync(TransactionQuery q);
        Task<List<AccountDto>> GetAccountsByUserAsync(int userId);
    }
}