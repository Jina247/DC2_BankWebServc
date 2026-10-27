// DataWebAPI/Models/Requests.cs
using System.ComponentModel.DataAnnotations;

namespace DataWebAPI.Models
{
    public class DepositRequest
    {
        [Required]
        public string AccountNumber { get; set; } = "";

        [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }
    }

    public class WithdrawRequest
    {
        [Required]
        public string AccountNumber { get; set; } = "";

        [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }
    }

    public class TransferRequest
    {
        [Required]
        public string FromAccountNumber { get; set; } = "";

        [Required]
        public string ToAccountNumber { get; set; } = "";

        [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }
    }
}