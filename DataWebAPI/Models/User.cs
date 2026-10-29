using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataWebAPI.Models
{
    public static class UserRoles
    {
        public const string Customer = "Customer";
        public const string Admin = "Admin";
    }

    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = "";

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = "";

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        public string? PicturePath { get; set; }

        [JsonIgnore]
        public string PasswordHash { get; set; } = "";

        public string Role { get; set; } = UserRoles.Customer;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public List<Account> Accounts { get; set; } = new();
    }
}
