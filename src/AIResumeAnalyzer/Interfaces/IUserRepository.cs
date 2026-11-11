using AIResumeAnalyzer.Models;

namespace AIResumeAnalyzer.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByEmailAsync(string email);
        Task<int> CreateAsync(User user);
        Task<bool> UpdateProfileAsync(int userId, string fullName, string email);
        Task<bool> UpdatePasswordAsync(int userId, string passwordHash, string passwordSalt);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> ToggleUserStatusAsync(int userId, bool isActive);
    }
}
