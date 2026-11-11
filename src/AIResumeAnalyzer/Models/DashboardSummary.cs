namespace AIResumeAnalyzer.Models
{
    public class UserDashboardSummary
    {
        public int TotalResumes { get; set; }
        public int TotalAnalyses { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public List<ResumeAnalysis> RecentAnalyses { get; set; } = new();
        public List<Resume> RecentResumes { get; set; } = new();
    }

    public class AdminDashboardSummary
    {
        public int TotalUsers { get; set; }
        public int TotalResumes { get; set; }
        public int TotalAnalyses { get; set; }
        public decimal SystemAverageScore { get; set; }
        public List<User> RecentUsers { get; set; } = new();
        public List<ResumeAnalysis> RecentAnalyses { get; set; } = new();
    }
}
