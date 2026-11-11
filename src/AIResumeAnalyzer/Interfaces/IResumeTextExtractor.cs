namespace AIResumeAnalyzer.Interfaces
{
    public interface IResumeTextExtractor
    {
        Task<string> ExtractTextAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default);
        bool IsValidFileSignature(Stream stream, string fileExtension);
    }
}
