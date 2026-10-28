using BankMvc.Models;
using BankMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMvc.Controllers
{
    // only administrators can open this page, customers get blocked on the server
    [Authorize(Roles = "Admin")]
    public class AdminTransController : Controller
    {
        // the client used to talk to the part A service
        private readonly BankApiClient _api;

        public AdminTransController(BankApiClient api)
        {
            _api = api;
        }

        // page that shows the transactions of all users, with search, filters, sorting and paging
        // everything comes from the query string, ex: ?Search=Deposit&From=2026-10-01&SortBy=amount
        public async Task<IActionResult> Index(TransactionQuery query)
        {
            // fix the page, page size & sorting in case the browser sends wrong values
            query.Page = Math.Max(1, query.Page);
            query.PageSize = new[] { 5, 10, 25, 50 }.Contains(query.PageSize) ? query.PageSize : 10;
            query.SortBy = query.SortBy == "amount" ? "amount" : "timestamp";
            query.SortDir = query.SortDir == "asc" ? "asc" : "desc";

            // check the date range, 'from' date can't be after 'to' date
            if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            {
                return View(new AdminTransactionsViewModel
                {
                    Query = query,
                    Error = "The 'from' date can't be after the 'to' date."
                });
            }

            // ask the service for that page of transactions
            // (account number is left as typed, empty = all accounts)
            var result = await _api.GetTransactionsAsync(query);

            // if the service is down or gave an error, show a clear message instead of crashing
            return View(new AdminTransactionsViewModel
            {
                Query = query,
                Result = result.Ok ? result.Data : null,
                Error = result.Ok ? null : result.Message
            });
        }
    }

    // data for the admin transactions page
    public class AdminTransactionsViewModel
    {
        // the filters currently picked, so the form keeps its values
        public TransactionQuery Query { get; set; } = new();

        // the page of transactions (null if something went wrong)
        public PagedResult<TransactionDto>? Result { get; set; }

        public string? Error { get; set; }
    }
}