using System.ComponentModel.DataAnnotations;

namespace DataWebAPI.Models
{
    // request object for creating an account
    public class CreateAccountRequest
    {
        // request for the user ID that owns the account
        // validate the user ID is a real positive number (1 or more)
        [Range(1, int.MaxValue, ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }

        // request for the account type, it is required & max 30 characters
        [Required, StringLength(30)]
        public string AccountType { get; set; } = "";   // e.g. Savings / Checking
    }

    // request object for updating an account
    public class UpdateAccountRequest
    {
        // request for the new account type, it is required & max 30 characters
        [Required, StringLength(30)]
        public string AccountType { get; set; } = "";
    }

    // object to send account details back to the user (response)
    public class AccountDto
    {
        // the account number
        public string AccountNumber { get; set; } = "";

        // the user ID that owns the account
        public int UserId { get; set; }

        // the account type, e.g. Savings / Checking
        public string AccountType { get; set; } = "";

        // the current balance of the account
        public decimal Balance { get; set; }

        // the account status, false means the account is deactivated
        public bool IsActive { get; set; }
    }
}