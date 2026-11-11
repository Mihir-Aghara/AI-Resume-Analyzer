using System.ComponentModel.DataAnnotations;
using AIResumeAnalyzer.Models;

namespace AIResumeAnalyzer.ViewModels
{
    public class AnalysisRequestViewModel
    {
        [Required(ErrorMessage = "Please select a resume to analyze.")]
        [Display(Name = "Select Resume")]
        public int ResumeId { get; set; }

        [Required(ErrorMessage = "Job Title is required.")]
        [StringLength(150, ErrorMessage = "Job Title cannot exceed 150 characters.")]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company / Organization Name (Optional)")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Please enter or paste the Job Description text.")]
        [MinLength(30, ErrorMessage = "Please paste a realistic job description (at least 30 characters).")]
        [Display(Name = "Job Description Text")]
        public string JobDescriptionText { get; set; } = string.Empty;

        // Available resumes for selection dropdown
        public List<Resume> AvailableResumes { get; set; } = new();
    }

    public class AnalysisHistoryViewModel
    {
        public List<ResumeAnalysis> Analyses { get; set; } = new();
        public string? SearchQuery { get; set; }
        public int TotalAnalyses => Analyses.Count;
        public decimal AverageScore => Analyses.Count > 0 ? Math.Round(Analyses.Average(a => a.OverallScore), 1) : 0m;
        public decimal HighestScore => Analyses.Count > 0 ? Analyses.Max(a => a.OverallScore) : 0m;
    }
}
