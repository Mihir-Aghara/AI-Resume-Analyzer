using System.ComponentModel.DataAnnotations;
using AIResumeAnalyzer.Models;
using Microsoft.AspNetCore.Http;

namespace AIResumeAnalyzer.ViewModels
{
    public class ResumeUploadViewModel
    {
        [Required(ErrorMessage = "Please select a resume file (PDF or DOCX).")]
        [Display(Name = "Resume Document")]
        public IFormFile? ResumeFile { get; set; }

        public string? Notes { get; set; }
    }

    public class ResumeListViewModel
    {
        public List<Resume> Resumes { get; set; } = new();
        public int TotalCount => Resumes.Count;
    }

    public class ResumeDetailsViewModel
    {
        public Resume Resume { get; set; } = new();
        public int AnalysisCount { get; set; }
    }
}
