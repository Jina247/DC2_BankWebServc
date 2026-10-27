using DataWebAPI.Models;
using DataWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataWebAPI.Controllers
{
    [Route("api/[controller]")]   // -> /api/accounts
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _service;
        public AccountsController(IAccountService service) => _service = service;

        // POST /api/accounts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAccountRequest req)
        {
            var acc = await _service.CreateAccountAsync(req);
            return CreatedAtAction(nameof(Get), new { accountNumber = acc.AccountNumber }, acc);   // 201 + Location
        }

        // GET /api/accounts/1001
        [HttpGet("{accountNumber}")]
        public async Task<IActionResult> Get(string accountNumber)
            => Ok(await _service.GetAccountAsync(accountNumber));

        // PUT /api/accounts/1001
        [HttpPut("{accountNumber}")]
        public async Task<IActionResult> Update(string accountNumber, [FromBody] UpdateAccountRequest req)
        {
            await _service.UpdateAccountAsync(accountNumber, req);
            return NoContent();   // 204
        }

        // DELETE /api/accounts/1001  (soft delete = deactivate)
        [HttpDelete("{accountNumber}")]
        public async Task<IActionResult> Deactivate(string accountNumber)
        {
            await _service.DeactivateAccountAsync(accountNumber);
            return NoContent();   // 204
        }

        // GET /api/accounts?userId=1
        [HttpGet]
        public async Task<IActionResult> GetByUser([FromQuery] int userId)
            => Ok(await _service.GetAccountsByUserAsync(userId));
    }
}