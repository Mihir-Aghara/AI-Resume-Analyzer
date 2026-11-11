namespace AIResumeAnalyzer.Models
{
    public class ResumeAnalysis
    {
        public int AnalysisId { get; set; }
        public int ResumeId { get; set; }
        public int JobDescriptionId { get; set; }
        public int UserId { get; set; }

        public decimal OverallScore { get; set; }
        public decimal SkillsScore { get; set; }
        public decimal KeywordsScore { get; set; }
        public decimal ExperienceScore { get; set; }
        public decimal EducationScore { get; set; }

        public int MatchedSkillsCount { get; set; }
        public int MissingSkillsCount { get; set; }
        public int TotalSkillsIdentified { get; set; }

        public string CalculationExplanation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Joined view properties
        public string? ResumeFileName { get; set; }
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? UserFullName { get; set; }

        // Children
        public List<AnalysisSkill> Skills { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
    }
}
