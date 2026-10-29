using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataWebAPI.Models
{
    public class Account
    {
        [Key, StringLength(20)]
        public string AccountNumber { get; set; } = "";

        [ForeignKey(nameof(Owner))]
        public int UserId { get; set; }

        [Required, StringLength(30)]
        public string AccountType { get; set; } = "";

        [Range(0, double.MaxValue)]
        public decimal Balance { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public User? Owner { get; set; }

        public List<Transaction> Transactions { get; set; } = new();
    }
}
