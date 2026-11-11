namespace AIResumeAnalyzer.Models
{
    public class Resume
    {
        public int ResumeId { get; set; }
        public int UserId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // ".pdf" or ".docx"
        public long FileSizeBytes { get; set; }
        public string ExtractedText { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation reference for display
        public string? UserFullName { get; set; }
    }
}
