using AIResumeAnalyzer.Services;
using Xunit;

namespace AIResumeAnalyzer.Tests
{
    public class TextExtractorTests
    {
        private readonly ResumeTextExtractor _extractor = new();

        [Fact]
        public void IsValidFileSignature_ValidPdfHeader_ShouldReturnTrue()
        {
            byte[] pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
            using var stream = new MemoryStream(pdfBytes);

            bool isValid = _extractor.IsValidFileSignature(stream, ".pdf");

            Assert.True(isValid);
        }

        [Fact]
        public void IsValidFileSignature_ExecutableRenamedToPdf_ShouldReturnFalse()
        {
            // Windows PE executable header: "MZ" (0x4D, 0x5A)
            byte[] exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
            using var stream = new MemoryStream(exeBytes);

            bool isValid = _extractor.IsValidFileSignature(stream, ".pdf");

            Assert.False(isValid);
        }

        [Fact]
        public void IsValidFileSignature_ValidDocxZipHeader_ShouldReturnTrue()
        {
            // PK\x03\x04 header
            byte[] docxBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 };
            using var stream = new MemoryStream(docxBytes);

            bool isValid = _extractor.IsValidFileSignature(stream, ".docx");

            Assert.True(isValid);
        }

        [Fact]
        public void IsValidFileSignature_EmptyStream_ShouldReturnFalse()
        {
            using var stream = new MemoryStream();

            bool isValid = _extractor.IsValidFileSignature(stream, ".pdf");

            Assert.False(isValid);
        }

        [Fact]
        public async Task ExtractTextAsync_UnsupportedExtension_ShouldThrowNotSupportedException()
        {
            using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            await Assert.ThrowsAsync<NotSupportedException>(() => 
                _extractor.ExtractTextAsync(stream, ".exe"));
        }

        [Fact]
        public async Task ExtractTextAsync_EmptyStream_ShouldThrowArgumentException()
        {
            using var stream = new MemoryStream();

            await Assert.ThrowsAsync<ArgumentException>(() => 
                _extractor.ExtractTextAsync(stream, ".pdf"));
        }

        [Fact]
        public async Task ExtractTextAsync_MismatchedSignature_ShouldThrowInvalidOperationException()
        {
            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes("This is plain text pretending to be a PDF");
            using var stream = new MemoryStream(textBytes);

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _extractor.ExtractTextAsync(stream, ".pdf"));
        }
    }
}
