using DataWebAPI.Models;
using DataWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataWebAPI.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _service;
        public AuditController(IAuditService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAuditRequest req)
        {
            var entry = await _service.CreateAsync(req);
            return CreatedAtAction(nameof(Get), new { id = entry.AuditId }, entry);  
        }


        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get(long id)
            => Ok(await _service.GetAsync(id));

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AuditQuery query)
            => Ok(await _service.SearchAsync(query));
    }
}
