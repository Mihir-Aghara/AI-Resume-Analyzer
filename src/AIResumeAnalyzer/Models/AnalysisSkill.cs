namespace AIResumeAnalyzer.Models
{
    public class AnalysisSkill
    {
        public int SkillId { get; set; }
        public int AnalysisId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsMatched { get; set; }
        public string? ProficiencyLevel { get; set; }
    }
}
