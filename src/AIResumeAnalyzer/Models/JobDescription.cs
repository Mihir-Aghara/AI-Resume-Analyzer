namespace AIResumeAnalyzer.Models
{
    public class JobDescription
    {
        public int JobDescriptionId { get; set; }
        public int UserId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string RawText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
