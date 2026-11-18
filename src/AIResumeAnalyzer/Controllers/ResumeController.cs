using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using AIResumeAnalyzer.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers
{
    [SessionAuthorize]
    public class ResumeController : Controller
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IResumeTextExtractor _textExtractor;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResumeController> _logger;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public ResumeController(
            IResumeRepository resumeRepository,
            IResumeTextExtractor textExtractor,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<ResumeController> logger)
        {
            _resumeRepository = resumeRepository;
            _textExtractor = textExtractor;
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: /Resume/Upload
        [HttpGet]
        public IActionResult Upload()
        {
            return View(new ResumeUploadViewModel());
        }

        // POST: /Resume/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ResumeUploadViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (model.ResumeFile == null || model.ResumeFile.Length == 0)
            {
                ModelState.AddModelError("ResumeFile", "Please choose a valid resume file to upload.");
                return View(model);
            }

            if (model.ResumeFile.Length > MaxFileSizeBytes)
            {
                ModelState.AddModelError("ResumeFile", $"File size exceeds the 5 MB limit (Current size: {model.ResumeFile.Length / 1024 / 1024.0:F2} MB).");
                return View(model);
            }

            string originalFileName = Path.GetFileName(model.ResumeFile.FileName);
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (extension != ".pdf" && extension != ".docx")
            {
                ModelState.AddModelError("ResumeFile", "Only PDF (.pdf) and Microsoft Word (.docx) files are supported.");
                return View(model);
            }

            // Verify binary signature
            using (var memoryStream = new MemoryStream())
            {
                await model.ResumeFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                if (!_textExtractor.IsValidFileSignature(memoryStream, extension))
                {
                    ModelState.AddModelError("ResumeFile", $"Security Check Failed: The file does not match a valid {extension.ToUpperInvariant()} binary signature. Renamed or executable files are rejected.");
                    return View(model);
                }

                // Extract text from resume
                string extractedText;
                try
                {
                    memoryStream.Position = 0;
                    extractedText = await _textExtractor.ExtractTextAsync(memoryStream, extension);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Text extraction failed for {FileName}", originalFileName);
                    ModelState.AddModelError("ResumeFile", $"Text extraction failed: {ex.Message}");
                    return View(model);
                }

                // Secure physical storage in App_Data/Resumes (Non-static directory)
                string appDataPath = Path.Combine(_env.ContentRootPath, "App_Data", "Resumes");
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                string storedFileName = $"{Guid.NewGuid():N}{extension}";
                string physicalPath = Path.Combine(appDataPath, storedFileName);

                try
                {
                    await using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
                    {
                        memoryStream.Position = 0;
                        await memoryStream.CopyToAsync(fileStream);
                    }

                    // Relative path for database record
                    string relativePath = Path.Combine("App_Data", "Resumes", storedFileName).Replace('\\', '/');

                    var resumeRecord = new Resume
                    {
                        UserId = userId.Value,
                        OriginalFileName = originalFileName,
                        StoredFileName = storedFileName,
                        FilePath = relativePath,
                        FileType = extension,
                        FileSizeBytes = model.ResumeFile.Length,
                        ExtractedText = extractedText,
                        UploadedAt = DateTime.UtcNow
                    };

                    int newResumeId = await _resumeRepository.CreateAsync(resumeRecord);
                    if (newResumeId <= 0)
                    {
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                        ModelState.AddModelError(string.Empty, "Failed to save the resume metadata to the database. Please try again.");
                        return View(model);
                    }

                    TempData["SuccessMessage"] = $"Resume \"{originalFileName}\" uploaded and text extracted successfully! You can now analyze it against a job description.";
                    return RedirectToAction("Create", "Analysis", new { resumeId = newResumeId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store uploaded resume {FileName} for user {UserId}", originalFileName, userId.Value);
                    if (System.IO.File.Exists(physicalPath))
                    {
                        try { System.IO.File.Delete(physicalPath); } catch { /* ignore cleanup error */ }
                    }
                    ModelState.AddModelError(string.Empty, "We couldn't save your resume right now. Please try uploading again.");
                    return View(model);
                }
            }
        }

        // GET: /Resume/MyResumes
        [HttpGet]
        public async Task<IActionResult> MyResumes()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var resumes = await _resumeRepository.GetByUserIdAsync(userId.Value);
            return View(new ResumeListViewModel { Resumes = resumes });
        }

        // GET: /Resume/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var resume = await _resumeRepository.GetByIdAndUserAsync(id, userId.Value);
            if (resume == null)
            {
                TempData["ErrorMessage"] = "The requested resume was not found or you do not have permission to access it.";
                return RedirectToAction("MyResumes");
            }

            return View(new ResumeDetailsViewModel { Resume = resume });
        }

        // POST: /Resume/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var resume = await _resumeRepository.GetByIdAndUserAsync(id, userId.Value);
                if (resume != null)
                {
                    bool deleted = await _resumeRepository.DeleteAsync(id, userId.Value);
                    if (deleted)
                    {
                        string physicalPath = Path.Combine(_env.ContentRootPath, resume.FilePath.Replace('/', Path.DirectorySeparatorChar));
                        if (System.IO.File.Exists(physicalPath))
                        {
                            try
                            {
                                System.IO.File.Delete(physicalPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete file from disk: {Path}", physicalPath);
                            }
                        }

                        TempData["SuccessMessage"] = $"Resume \"{resume.OriginalFileName}\" and its associated reviews were deleted successfully.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "The resume could not be deleted. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resume {ResumeId} for user {UserId}", id, userId.Value);
                TempData["ErrorMessage"] = "An error occurred while deleting the resume. Please try again.";
            }

            return RedirectToAction("MyResumes");
        }
    }
}
