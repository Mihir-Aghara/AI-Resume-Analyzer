using AIResumeAnalyzer.Models;

namespace AIResumeAnalyzer.Interfaces
{
    public interface IResumeAnalysisService
    {
        Task<ResumeAnalysis> AnalyzeResumeAsync(
            string resumeText, 
            string jobDescriptionText, 
            int resumeId, 
            int jobDescriptionId, 
            int userId, 
            CancellationToken cancellationToken = default);

        List<string> ExtractSkills(string text);
        HashSet<string> ExtractKeywords(string text);
    }
}
