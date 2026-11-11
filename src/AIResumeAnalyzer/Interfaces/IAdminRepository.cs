using AIResumeAnalyzer.Models;

namespace AIResumeAnalyzer.Interfaces
{
    public interface IAdminRepository
    {
        Task<AdminDashboardSummary> GetDashboardSummaryAsync();
        Task<List<User>> GetAllUsersAsync();
        Task<bool> UpdateUserRoleAsync(int userId, string newRole);
    }
}
