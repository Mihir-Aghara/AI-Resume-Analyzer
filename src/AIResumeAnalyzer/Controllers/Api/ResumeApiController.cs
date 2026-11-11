using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers.Api
{
    [ApiController]
    [Route("api/resumes")]
    [Produces("application/json")]
    public class ResumeApiController : ControllerBase
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IResumeTextExtractor _textExtractor;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ResumeApiController> _logger;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public ResumeApiController(
            IResumeRepository resumeRepository,
            IResumeTextExtractor textExtractor,
            IWebHostEnvironment env,
            ILogger<ResumeApiController> logger)
        {
            _resumeRepository = resumeRepository;
            _textExtractor = textExtractor;
            _env = env;
            _logger = logger;
        }

        // GET: /api/resumes
        [HttpGet]
        public async Task<IActionResult> GetMyResumes()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.Fail("User is not authenticated. Please log in."));
            }

            var resumes = await _resumeRepository.GetByUserIdAsync(userId.Value);
            return Ok(ApiResponse<List<Resume>>.Ok(resumes, "Resumes retrieved successfully."));
        }

        // GET: /api/resumes/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetResumeById(int id)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.Fail("User is not authenticated. Please log in."));
            }

            var resume = await _resumeRepository.GetByIdAndUserAsync(id, userId.Value);
            if (resume == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Resume with ID {id} was not found."));
            }

            return Ok(ApiResponse<Resume>.Ok(resume, "Resume details retrieved."));
        }

        // POST: /api/resumes/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadResume([FromForm] IFormFile? file)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.Fail("User is not authenticated. Please log in."));
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("No file was uploaded or file is empty."));
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest(ApiResponse<object>.Fail($"File size exceeds maximum 5 MB limit."));
            }

            string originalFileName = Path.GetFileName(file.FileName);
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (extension != ".pdf" && extension != ".docx")
            {
                return BadRequest(ApiResponse<object>.Fail("Only .pdf and .docx documents are supported."));
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            if (!_textExtractor.IsValidFileSignature(memoryStream, extension))
            {
                return BadRequest(ApiResponse<object>.Fail("Binary signature mismatch: uploaded file is not a valid PDF or Word document."));
            }

            string extractedText;
            try
            {
                memoryStream.Position = 0;
                extractedText = await _textExtractor.ExtractTextAsync(memoryStream, extension);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API extraction failed for {FileName}", originalFileName);
                return BadRequest(ApiResponse<object>.Fail($"Text extraction failed: {ex.Message}"));
            }

            string appDataPath = Path.Combine(_env.ContentRootPath, "App_Data", "Resumes");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            string storedFileName = $"{Guid.NewGuid():N}{extension}";
            string physicalPath = Path.Combine(appDataPath, storedFileName);

            await using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
            {
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(fileStream);
            }

            string relativePath = Path.Combine("App_Data", "Resumes", storedFileName).Replace('\\', '/');

            var resume = new Resume
            {
                UserId = userId.Value,
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                FilePath = relativePath,
                FileType = extension,
                FileSizeBytes = file.Length,
                ExtractedText = extractedText,
                UploadedAt = DateTime.UtcNow
            };

            int newId = await _resumeRepository.CreateAsync(resume);
            resume.ResumeId = newId;

            return CreatedAtAction(nameof(GetResumeById), new { id = newId }, ApiResponse<Resume>.Ok(resume, "Resume uploaded and text extracted successfully."));
        }
    }
}
