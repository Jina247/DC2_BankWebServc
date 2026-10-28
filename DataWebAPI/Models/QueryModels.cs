using System.ComponentModel.DataAnnotations;

namespace DataWebAPI.Models
{
    // request object for querying transactions
    public class TransactionQuery
    {
        // account number
        public string? AccountNumber { get; set; }

        // from & to date
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // what is being searched
        public string? Search { get; set; }

        // sort out how many pages to display (< 100 results)
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        // sort out how many pages to display (for 100 results)
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        // sort by what?
        public string SortBy { get; set; } = "timestamp";
        public string SortDir { get; set; } = "desc";
    }

    // object class for paged results
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}