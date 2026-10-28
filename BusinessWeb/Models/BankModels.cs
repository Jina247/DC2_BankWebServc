using System.ComponentModel.DataAnnotations;

namespace BankMvc.Models
{
    // object to hold the account details we get back from the service
    public class AccountDto
    {
        // the account number, used to show & to pick the account
        public string AccountNumber { get; set; } = "";

        // the user ID that owns the account
        public int UserId { get; set; }

        // the account type, e.g. Savings / Checking
        public string AccountType { get; set; } = "";

        // the current balance of the account
        public decimal Balance { get; set; }

        // the account status, false means the account is deactivated
        public bool IsActive { get; set; } = true;
    }

    // object to hold one transaction we get back from the service
    public class TransactionDto
    {
        public long TransactionId { get; set; }

        // the account number this transaction belongs to
        public string AccountNumber { get; set; } = "";

        // Deposit, Withdrawal, TransferOut or TransferIn
        public string Type { get; set; } = "";

        // money in is positive, money out is negative
        // 1001 TransferOut -100.00
        public decimal Amount { get; set; }

        // balance of the account right after this transaction
        public decimal BalanceAfter { get; set; }

        // saved as UTC by the system itself for storing data accurately
        public DateTime Timestamp { get; set; }
    }

    // object class for paged results
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    // filters the user picks on the page, used by one account and all users
    public class TransactionQuery
    {
        // account number, empty = all accounts
        public string? AccountNumber { get; set; }

        // what is being searched (only matches transaction type or account number)
        public string? Search { get; set; }

        // from & to date, both are optional
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // sort by what? only "timestamp" and "amount" work, anything else = newest first
        public string SortBy { get; set; } = "timestamp";

        // asc or desc
        public string SortDir { get; set; } = "desc";

        // which page to display (starts from 1)
        public int Page { get; set; } = 1;

        // how many results in one page
        public int PageSize { get; set; } = 10;
    }

    // data for the transfer form, validation here = clear error messages before we even call the service
    public class TransferViewModel
    {
        // sender account number, must be one of the logged in customer's own accounts
        [Required(ErrorMessage = "Pick an account to transfer from.")]
        public string FromAccountNumber { get; set; } = "";

        // receiver account number, can be own account or another user's account
        [Required(ErrorMessage = "Recipient account number is required.")]
        public string ToAccountNumber { get; set; } = "";

        // validate the amount to transfer is greater than 0
        [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        // optional description for transfer
        [StringLength(200)]
        public string? Description { get; set; }

        // customer's own accounts, only used to fill the dropdown (not sent back)
        public List<AccountDto> MyAccounts { get; set; } = new();
    }
}