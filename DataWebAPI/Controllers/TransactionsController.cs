using DataWebAPI.Models;
using DataWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataWebAPI.Controllers
{
    [Route("api/[controller]")]   // -> /api/transactions
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly IAccountService _service;
        public TransactionsController(IAccountService service) => _service = service;

        // POST /api/transactions/deposit
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequest req)
        {
            var result = await _service.DepositAsync(req);
            return Created($"/api/transactions/{result.TransactionId}", result);
        }

        // POST /api/transactions/withdraw
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest req)
        {
            var result = await _service.WithdrawAsync(req);
            return Created($"/api/transactions/{result.TransactionId}", result);
        }

        // POST /api/transactions/transfer
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest req)
        {
            var result = await _service.TransferAsync(req);
            return Created($"/api/transactions/{result.TransactionId}", result);
        }

        // GET /api/transactions?page=1&pageSize=5&sortBy=amount&sortDir=asc
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] TransactionQuery query)
            => Ok(await _service.SearchTransactionsAsync(query));
    }
}