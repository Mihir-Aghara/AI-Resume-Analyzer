namespace AIResumeAnalyzer.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string? ErrorTitle { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FailedPath { get; set; }
    public string? DeveloperDetail { get; set; }
}
