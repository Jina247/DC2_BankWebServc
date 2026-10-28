using System.Security.Claims;
using BankMvc.Models;
using BankMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMvc.Controllers
{
    // only logged in customers can open these pages (checked on the server)
    [Authorize(Roles = "Customer")]
    public class AccountsController : Controller
    {
        // the client used to talk to the part A service
        private readonly BankApiClient _api;

        public AccountsController(BankApiClient api)
        {
            _api = api;
        }

        // helper method to get the logged in user's ID from the login cookie
        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        // page that shows the summary of the customer's account number + balance
        public async Task<IActionResult> Index()
        {
            // cookie has no valid user ID = not really logged in, send back to login
            if (!TryGetUserId(out var userId))
                return Challenge();

            // get the accounts of this user only
            var result = await _api.GetAccountsAsync(userId);

            // if the service is down or gave an error, show a clear message instead of crashing
            var vm = new AccountsIndexViewModel
            {
                Accounts = result.Ok ? result.Data ?? new() : new(),
                Error = result.Ok ? null : result.Message
            };

            // show the page with the accounts
            return View(vm);
        }

        // page that shows the transaction history of one account, ID = account number
        // the filters (From, To), sorting and paging come from the query string
        public async Task<IActionResult> History(string id, TransactionQuery query)
        {
            if (!TryGetUserId(out var userId))
                return Challenge();

            // get the customer's own accounts first (to check who owns the account)
            var accounts = await _api.GetAccountsAsync(userId);
            if (!accounts.Ok)
                return View(new HistoryViewModel { AccountNumber = id, Query = query, Error = accounts.Message });

            // block if the account number in URL is not one of this customer's accounts
            // so changing the URL to someone else's account number will not work
            if (!(accounts.Data ?? new()).Any(a => a.AccountNumber == id))
                return Forbid();

            // use the account number from URL only, so it can't be changed through query
            query.AccountNumber = id;

            // fix the page, page size & sorting in case the browser sends wrong values
            query.Page = Math.Max(1, query.Page);
            query.PageSize = new[] { 5, 10, 25, 50 }.Contains(query.PageSize) ? query.PageSize : 10;
            query.SortBy = query.SortBy == "amount" ? "amount" : "timestamp";
            query.SortDir = query.SortDir == "asc" ? "asc" : "desc";

            // check the date range, 'from' date can't be after 'to' date
            if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            {
                return View(new HistoryViewModel
                {
                    AccountNumber = id,
                    Query = query,
                    Error = "The 'from' date can't be after the 'to' date."
                });
            }

            // ask the service for that page of transactions
            var result = await _api.GetTransactionsAsync(query);

            return View(new HistoryViewModel
            {
                AccountNumber = id,
                Query = query,
                Result = result.Ok ? result.Data : null,
                Error = result.Ok ? null : result.Message
            });
        }
    }

    // data for the accounts summary page
    public class AccountsIndexViewModel
    {
        public List<AccountDto> Accounts { get; set; } = new();

        // not null = show this message on the page
        public string? Error { get; set; }
    }

    // data for the history page
    public class HistoryViewModel
    {
        public string AccountNumber { get; set; } = "";

        // filters currently picked, so the form keeps its values
        public TransactionQuery Query { get; set; } = new();

        // the page of transactions (null if something went wrong)
        public PagedResult<TransactionDto>? Result { get; set; }

        public string? Error { get; set; }
    }
}