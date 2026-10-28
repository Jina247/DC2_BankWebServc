namespace BankMvc.Models
{
    // custom exception for cannot view results due to empty / rule violation
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
