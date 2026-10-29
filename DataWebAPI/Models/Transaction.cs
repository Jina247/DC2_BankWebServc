using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataWebAPI.Models
{
    public static class TransactionTypes
    {
        public const string Deposit = "Deposit";
        public const string Withdrawal = "Withdrawal";
        public const string TransferOut = "TransferOut";   
        public const string TransferIn = "TransferIn";     
    }

    public class Transaction
    {
        [Key]
        public long TransactionId { get; set; }

        [ForeignKey(nameof(Account))]
        public string AccountNumber { get; set; } = "";

        [Required]
        public string Type { get; set; } = "";

        public decimal Amount { get; set; }

        public decimal BalanceAfter { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public string? RelatedAccountNumber { get; set; }

        public DateTime Timestamp { get; set; }

        public Account? Account { get; set; }
    }
}
