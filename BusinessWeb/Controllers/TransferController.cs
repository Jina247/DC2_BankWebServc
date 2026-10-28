using System.Security.Claims;
using BankMvc.Models;
using BankMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMvc.Controllers
{
    // only logged in customers can transfer money (checked on the server)
    [Authorize(Roles = "Customer")]
    public class TransferController : Controller
    {
        // the client used to talk to the part A service
        private readonly BankApiClient _api;

        public TransferController(BankApiClient api)
        {
            _api = api;
        }

        // helper method to get the logged in user's id from the login cookie
        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        // method to show the empty transfer form
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!TryGetUserId(out var userId))
                return Challenge();

            var vm = new TransferViewModel();

            // fill the "transfer from" dropdown with this customer's own accounts only
            var accounts = await _api.GetAccountsAsync(userId);
            if (accounts.Ok)
                vm.MyAccounts = accounts.Data ?? new();
            else
                ModelState.AddModelError("", accounts.Message);

            return View(vm);
        }

        // method that runs when the customer presses the transfer button
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TransferViewModel m)
        {
            if (!TryGetUserId(out var userId))
                return Challenge();

            // get the customer's own accounts again (to fill the dropdown & to check who owns the account)
            var accounts = await _api.GetAccountsAsync(userId);
            if (!accounts.Ok)
            {
                ModelState.AddModelError("", accounts.Message);
                return View(m);
            }
            m.MyAccounts = accounts.Data ?? new();

            // block if the sender account is not one of the customer's own accounts
            // so nobody can transfer out of another customer's account by changing the form
            if (!m.MyAccounts.Any(a => a.AccountNumber == m.FromAccountNumber))
                return Forbid();

            // remove accidental spaces from the receiver account number
            m.ToAccountNumber = (m.ToAccountNumber ?? "").Trim();

            // can't transfer to the same account
            if (m.FromAccountNumber == m.ToAccountNumber)
                ModelState.AddModelError(nameof(m.ToAccountNumber), "You can't transfer to the same account.");

            // stop here if the form has errors (required fields, amount > 0, description too long)
            if (!ModelState.IsValid)
                return View(m);

            // ask the service to do the transfer
            var result = await _api.TransferAsync(m);

            if (!result.Ok)
            {
                // show the error on the form, ex: "Balance is 45.20; requested 100.00."
                // this also covers receiver not found, deactivated account & service down, so no crashed page
                ModelState.AddModelError("", result.Message);
                foreach (var d in result.Details)
                    ModelState.AddModelError("", d);
                return View(m);
            }

            // if successful, go to the accounts page with a message so they can see the new balance
            TempData["Success"] = $"Transferred ${m.Amount:N2} to account {m.ToAccountNumber}.";
            return RedirectToAction("Index", "Accounts");
        }
    }
}