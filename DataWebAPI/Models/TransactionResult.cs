namespace DataWebAPI.Models
{
    // declare object to hold the result of a transaction
    // <transID> 1001 Withdrawal 500.00 250.00 2023-03-15T10:30:00Z
    public class TransactionResult
    {
        public long TransactionId { get; set; }
        public string AccountNumber { get; set; } = "";
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime Timestamp { get; set; }
    }
}