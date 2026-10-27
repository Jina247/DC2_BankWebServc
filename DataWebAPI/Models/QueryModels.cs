using System.ComponentModel.DataAnnotations;

namespace DataWebAPI.Models
{
    public class TransactionQuery
    {
        public string? AccountNumber { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Search { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        public string SortBy { get; set; } = "timestamp";   // timestamp | amount
        public string SortDir { get; set; } = "desc";       // asc | desc
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}