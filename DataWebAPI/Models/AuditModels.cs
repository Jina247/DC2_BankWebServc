using System.ComponentModel.DataAnnotations;

namespace DataWebAPI.Models
{
    public class CreateAuditRequest{
        [Range(1, int.MaxValue, ErrorMessage = "AdminUserId is required.")]
        public int AdminUserId { get; set; }

        [Required, StringLength(50)]
        public string Action { get; set; } = "";

        [StringLength(30)]
        public string? TargetType { get; set; }

        [StringLength(50)]
        public string? TargetId { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }
    }

    public class AuditEntryDto{
        public long AuditId { get; set; }
        public int AdminUserId { get; set; }
        public string AdminUsername { get; set; } = "";
        public string Action { get; set; } = "";
        public string? TargetType { get; set; }
        public string? TargetId { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AuditQuery
    {
        public int? AdminUserId { get; set; }
        public string? Action { get; set; }

        public string? Search { get; set; }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public string SortDir { get; set; } = "desc";
    }
}
