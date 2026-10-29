using DataWebAPI.Models;

namespace DataWebAPI.Services
{
    public interface IUserService
    {
        // function to create a user
        Task<UserDto> CreateUserAsync(CreateUserRequest req);

        // functions to fetch user details
        Task<UserDto> GetUserAsync(int userId);
        Task<UserDto> GetUserByUsernameAsync(string username);
        Task<UserDto> GetUserByEmailAsync(string email);

        // function to update user details
        Task UpdateUserAsync(int userId, UpdateUserRequest req);

        // function to deactivate a user
        Task DeactivateUserAsync(int userId);

        // function to check a username + password, returns null if they are wrong
        Task<UserDto?> LoginAsync(LoginRequest req);

        // function to save a user's profile picture as a file
        Task<UserDto> SavePictureAsync(int userId, IFormFile file);

        // function to get the full file path of a user's profile picture
        Task<string> GetPictureFileAsync(int userId);
    }
}
