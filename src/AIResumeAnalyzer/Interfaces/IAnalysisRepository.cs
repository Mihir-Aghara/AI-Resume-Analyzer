using AIResumeAnalyzer.Models;

namespace AIResumeAnalyzer.Interfaces
{
    public interface IAnalysisRepository
    {
        Task<int> CreateJobDescriptionAsync(JobDescription jobDescription);
        Task<int> SaveAnalysisAsync(ResumeAnalysis analysis);
        Task<ResumeAnalysis?> GetByIdAsync(int analysisId);
        Task<ResumeAnalysis?> GetByIdAndUserAsync(int analysisId, int userId);
        Task<List<ResumeAnalysis>> GetByUserIdAsync(int userId);
        Task<List<ResumeAnalysis>> GetRecentByUserIdAsync(int userId, int limit = 5);
        Task<List<ResumeAnalysis>> GetAllAnalysesAsync(int limit = 50);
        Task<int> GetCountByUserIdAsync(int userId);
        Task<decimal> GetAverageScoreByUserIdAsync(int userId);
        Task<decimal> GetHighestScoreByUserIdAsync(int userId);
    }
}
