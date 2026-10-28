using DataWebAPI.Models;

namespace DataWebAPI.Services
{
    public interface IAccountService
    {
        // method to deposit money into an account
        Task<TransactionResult> DepositAsync(DepositRequest req);

        // method to withdraw money from an account
        Task<TransactionResult> WithdrawAsync(WithdrawRequest req);

        // method to transfer money from one account to another
        Task<TransactionResult> TransferAsync(TransferRequest req);

        // function to create an account
        Task<AccountDto> CreateAccountAsync(CreateAccountRequest req);

        // function to fetch account details
        Task<AccountDto> GetAccountAsync(string accountNumber);

        // function to update account details
        Task UpdateAccountAsync(string accountNumber, UpdateAccountRequest req);

        // function to deactivate an account
        Task DeactivateAccountAsync(string accountNumber);

        // method to search specific transactions (filter, sort & paging)
        Task<PagedResult<TransactionResult>> SearchTransactionsAsync(TransactionQuery q);

        // method to fetch all the accounts for a specific user ID
        Task<List<AccountDto>> GetAccountsByUserAsync(int userId);
    }
}