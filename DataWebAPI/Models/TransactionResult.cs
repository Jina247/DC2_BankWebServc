namespace DataWebAPI.Models
{
    public class TransactionResult
    {
        public long TransactionId { get; set; }
        public string AccountNumber { get; set; } = "";
        public string Type { get; set; } = "";      // Deposit / Withdrawal / TransferOut
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime Timestamp { get; set; }
    }
}