using AIResumeAnalyzer.Models;

namespace AIResumeAnalyzer.Interfaces
{
    public interface IResumeRepository
    {
        Task<int> CreateAsync(Resume resume);
        Task<Resume?> GetByIdAsync(int resumeId);
        Task<Resume?> GetByIdAndUserAsync(int resumeId, int userId);
        Task<List<Resume>> GetByUserIdAsync(int userId);
        Task<List<Resume>> GetAllResumesAsync();
        Task<int> GetCountByUserIdAsync(int userId);
        Task<bool> DeleteAsync(int resumeId, int userId);
    }
}
