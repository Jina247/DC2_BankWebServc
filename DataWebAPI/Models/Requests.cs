using System.ComponentModel.DataAnnotations;

namespace DataWebAPI.Models
{
    // request objects for deposit
    public class DepositRequest
    {
        // request for account number
        [Required]
        public string AccountNumber { get; set; } = "";

        // validate the amount to deposit is greater than 0
        [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than 0.")]

        // request for amount to deposit
        public decimal Amount { get; set; }

        // optional description for deposit
        [StringLength(200)]
        public string? Description { get; set; }
    }

    // request object for withdraw
    public class WithdrawRequest
    {
        // request for account number
        [Required]
        public string AccountNumber { get; set; } = "";

        // validate the amount to deposit is greater than 0
        [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than 0.")]

        // request for amount to withdraw
        public decimal Amount { get; set; }

        // optional description for withdraw
        [StringLength(200)]
        public string? Description { get; set; }
    }

    // request object for transfer
    public class TransferRequest
    {
        // request for sender account number
        [Required]
        public string FromAccountNumber { get; set; } = "";

        // request for receiver account number
        [Required]
        public string ToAccountNumber { get; set; } = "";

        // validate the amount to transfer is greater than 0
        [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than 0.")]

        // request for amount to transfer
        public decimal Amount { get; set; }


        // optional description for transfer
        [StringLength(200)]
        public string? Description { get; set; }
    }
}