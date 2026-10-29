using System.Diagnostics;
using BankMvc.Models;
using BankMvc.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly BankApiClient _api;
        private readonly ILogger<HomeController> _logger;

        public HomeController(BankApiClient api, ILogger<HomeController> logger)
        {
            _api = api;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "AdminDashboard");
            if (User.IsInRole("Customer"))
                return RedirectToAction("Index", "Accounts");

            return View(new HomeViewModel { ServiceUp = await _api.IsServiceUpAsync() });
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error != null)
                _logger.LogError(feature.Error, "Unhandled exception on {Path}", feature.Path);

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("Home/Status/{code:int}")]
        public IActionResult Status(int code)
        {
            Response.StatusCode = code;  

            var vm = code switch
            {
                403 => new StatusViewModel { Code = 403, Title = "Access denied", Message = "You don't have permission to view that page." },
                404 => new StatusViewModel { Code = 404, Title = "Page not found", Message = "The page or record you asked for doesn't exist." },
                401 => new StatusViewModel { Code = 401, Title = "Please sign in", Message = "You need to sign in to view that page." },
                503 => new StatusViewModel { Code = 503, Title = "Service unavailable", Message = "The bank service is unavailable. Please try again later." },
                _ => new StatusViewModel { Code = code, Title = "Something went wrong", Message = "We couldn't complete your request. Please try again." }
            };
            return View(vm);
        }
    }
}
