using GreenSquirrelDev.Shared.Models;

namespace GreenSquirrelDev.Functions.Services;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(string id);
    Task<User?> GetUserByGoogleIdAsync(string googleUserId);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task DeleteUserAsync(string id);
}
