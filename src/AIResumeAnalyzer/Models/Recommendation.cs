namespace AIResumeAnalyzer.Models
{
    public class Recommendation
    {
        public int RecommendationId { get; set; }
        public int AnalysisId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // "High", "Medium", "Low"
    }
}
