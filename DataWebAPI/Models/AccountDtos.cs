using System.ComponentModel.DataAnnotations;

namespace DataWebAPI.Models
{
    public class CreateAccountRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }

        [Required, StringLength(30)]
        public string AccountType { get; set; } = "";   // e.g. Savings / Checking
    }

    public class UpdateAccountRequest
    {
        [Required, StringLength(30)]
        public string AccountType { get; set; } = "";
    }

    public class AccountDto
    {
        public string AccountNumber { get; set; } = "";
        public int UserId { get; set; }
        public string AccountType { get; set; } = "";
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
    }
}