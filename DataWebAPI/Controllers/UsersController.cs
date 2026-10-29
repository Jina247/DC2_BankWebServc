using DataWebAPI.Models;
using DataWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataWebAPI.Controllers
{
    [Route("api/[controller]")]   // -> /api/users
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;
        public UsersController(IUserService service) => _service = service;

        // POST /api/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            var user = await _service.CreateUserAsync(req);
            return CreatedAtAction(nameof(Get), new { id = user.UserId }, user);   // 201 + Location
        }

        // GET /api/users/2
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
            => Ok(await _service.GetUserAsync(id));

        // GET /api/users/username/olivia.smith
        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetByUsername(string username)
            => Ok(await _service.GetUserByUsernameAsync(username));

        // GET /api/users/email/olivia.smith@example.com
        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
            => Ok(await _service.GetUserByEmailAsync(email));

        // PUT /api/users/2
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
        {
            await _service.UpdateUserAsync(id, req);
            return NoContent();   // 204
        }

        // DELETE /api/users/2  (soft delete = deactivate)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Deactivate(int id)
        {
            await _service.DeactivateUserAsync(id);
            return NoContent();   // 204
        }

        // POST /api/users/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _service.LoginAsync(req);
            if (user == null)
                return Unauthorized(new
                {
                    error = "INVALID_CREDENTIALS",
                    message = "Username or password is incorrect.",
                    details = (object?)null
                });

            return Ok(user);   // includes the role
        }

        // POST /api/users/2/picture  (form-data, key = file)
        [HttpPost("{id:int}/picture")]
        public async Task<IActionResult> UploadPicture(int id, IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                return BadRequest(new
                {
                    error = "VALIDATION_ERROR",
                    message = "One or more fields are invalid.",
                    details = new { file = new[] { "Only .jpg, .jpeg and .png files are allowed." } }
                });

            if (file.Length == 0 || file.Length > 2 * 1024 * 1024)
                return BadRequest(new
                {
                    error = "VALIDATION_ERROR",
                    message = "One or more fields are invalid.",
                    details = new { file = new[] { "File must not be empty or bigger than 2 MB." } }
                });

            return Ok(await _service.SavePictureAsync(id, file));
        }

        // GET /api/users/2/picture
        [HttpGet("{id:int}/picture")]
        public async Task<IActionResult> GetPicture(int id)
        {
            var path = await _service.GetPictureFileAsync(id);
            var type = path.EndsWith(".png") ? "image/png" : "image/jpeg";
            return PhysicalFile(path, type);
        }
    }
}
