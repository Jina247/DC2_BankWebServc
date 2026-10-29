using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace DataWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly string _connStr;

        public HealthController(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection") ?? "Data Source=bank.db;Foreign Keys=True";
        }

        [HttpGet]
        public IActionResult Get()
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users";
            cmd.ExecuteScalar(); 

            return Ok(new { status = "ok", time = DateTime.UtcNow });
        }
    }
}
