using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataWebAPI.Models
{
    public class AuditLog
    {
        [Key]
        public long AuditId { get; set; }

        [ForeignKey(nameof(Admin))]
        public int AdminUserId { get; set; }

        [Required, StringLength(50)]
        public string Action { get; set; } = "";

        [StringLength(30)]
        public string? TargetType { get; set; }

        [StringLength(50)]
        public string? TargetId { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; }

        public User? Admin { get; set; }
    }
}
