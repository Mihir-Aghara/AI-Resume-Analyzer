using System.Text;
using AIResumeAnalyzer.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace AIResumeAnalyzer.Services
{
    public class ResumeTextExtractor : IResumeTextExtractor
    {
        private static readonly byte[] PdfMagicBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
        private static readonly byte[] ZipMagicBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // "PK\x03\x04" (DOCX)

        public bool IsValidFileSignature(Stream stream, string fileExtension)
        {
            if (stream == null || stream.Length < 4)
            {
                return false;
            }

            long originalPosition = stream.Position;
            try
            {
                stream.Position = 0;
                byte[] header = new byte[4];
                int bytesRead = stream.Read(header, 0, 4);
                if (bytesRead < 4)
                {
                    return false;
                }

                string ext = fileExtension.ToLowerInvariant();
                if (ext == ".pdf")
                {
                    return header[0] == PdfMagicBytes[0] &&
                           header[1] == PdfMagicBytes[1] &&
                           header[2] == PdfMagicBytes[2] &&
                           header[3] == PdfMagicBytes[3];
                }
                else if (ext == ".docx")
                {
                    return header[0] == ZipMagicBytes[0] &&
                           header[1] == ZipMagicBytes[1] &&
                           header[2] == ZipMagicBytes[2] &&
                           header[3] == ZipMagicBytes[3];
                }

                return false;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        public async Task<string> ExtractTextAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default)
        {
            if (fileStream == null || fileStream.Length == 0)
            {
                throw new ArgumentException("The uploaded file stream is empty.", nameof(fileStream));
            }

            string ext = fileExtension.ToLowerInvariant().Trim();
            if (ext != ".pdf" && ext != ".docx")
            {
                throw new NotSupportedException($"File format '{fileExtension}' is not supported. Please upload a .pdf or .docx file.");
            }

            if (!IsValidFileSignature(fileStream, ext))
            {
                throw new InvalidOperationException($"The file content does not match the expected {ext.ToUpperInvariant()} binary signature. Executable or renamed files are rejected.");
            }

            fileStream.Position = 0;

            if (ext == ".pdf")
            {
                return await ExtractFromPdfAsync(fileStream, cancellationToken);
            }
            else
            {
                return await ExtractFromDocxAsync(fileStream, cancellationToken);
            }
        }

        private static Task<string> ExtractFromPdfAsync(Stream stream, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var sb = new StringBuilder();
                using (var document = PdfDocument.Open(stream))
                {
                    if (document.NumberOfPages == 0)
                    {
                        throw new InvalidDataException("The PDF document does not contain any readable pages.");
                    }

                    for (int i = 1; i <= document.NumberOfPages; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var page = document.GetPage(i);
                        string pageText = page.Text;

                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            sb.AppendLine(pageText);
                        }
                    }
                }

                string result = NormalizeExtractedText(sb.ToString());
                if (string.IsNullOrWhiteSpace(result))
                {
                    throw new InvalidDataException("Unable to extract text from the PDF. It may be an image-only scan or password-protected.");
                }

                return result;
            }, cancellationToken);
        }

        private static Task<string> ExtractFromDocxAsync(Stream stream, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var sb = new StringBuilder();
                using (var wordDoc = WordprocessingDocument.Open(stream, false))
                {
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    if (body == null)
                    {
                        throw new InvalidDataException("The Word document body could not be accessed.");
                    }

                    foreach (var paragraph in body.Descendants<Paragraph>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string text = paragraph.InnerText;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            sb.AppendLine(text);
                        }
                    }
                }

                string result = NormalizeExtractedText(sb.ToString());
                if (string.IsNullOrWhiteSpace(result))
                {
                    throw new InvalidDataException("The Word document contains no readable text content.");
                }

                return result;
            }, cancellationToken);
        }

        private static string NormalizeExtractedText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return string.Empty;
            }

            // Normalize line endings and collapse excessive spaces
            var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var cleanLines = new List<string>();

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0)
                {
                    cleanLines.Add(trimmed);
                }
            }

            return string.Join(Environment.NewLine, cleanLines);
        }
    }
}
