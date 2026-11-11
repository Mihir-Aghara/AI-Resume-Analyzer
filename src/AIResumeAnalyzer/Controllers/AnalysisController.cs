using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using AIResumeAnalyzer.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers
{
    [SessionAuthorize]
    public class AnalysisController : Controller
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IAnalysisRepository _analysisRepository;
        private readonly IResumeAnalysisService _analysisService;
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(
            IResumeRepository resumeRepository,
            IAnalysisRepository analysisRepository,
            IResumeAnalysisService analysisService,
            ILogger<AnalysisController> logger)
        {
            _resumeRepository = resumeRepository;
            _analysisRepository = analysisRepository;
            _analysisService = analysisService;
            _logger = logger;
        }

        // GET: /Analysis/Create?resumeId=5
        [HttpGet]
        public async Task<IActionResult> Create(int? resumeId)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var resumes = await _resumeRepository.GetByUserIdAsync(userId.Value);
            if (resumes.Count == 0)
            {
                TempData["InfoMessage"] = "Please upload at least one resume document before triggering a compatibility analysis.";
                return RedirectToAction("Upload", "Resume");
            }

            var model = new AnalysisRequestViewModel
            {
                AvailableResumes = resumes,
                ResumeId = resumeId ?? resumes.First().ResumeId
            };

            return View(model);
        }

        // POST: /Analysis/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnalysisRequestViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var resumes = await _resumeRepository.GetByUserIdAsync(userId.Value);
            model.AvailableResumes = resumes;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var selectedResume = resumes.FirstOrDefault(r => r.ResumeId == model.ResumeId);
            if (selectedResume == null)
            {
                ModelState.AddModelError("ResumeId", "The selected resume was not found or belongs to another user.");
                return View(model);
            }

            try
            {
                // 1. Create and persist Job Description record in database
                var jobDescription = new JobDescription
                {
                    UserId = userId.Value,
                    JobTitle = model.JobTitle.Trim(),
                    CompanyName = string.IsNullOrWhiteSpace(model.CompanyName) ? null : model.CompanyName.Trim(),
                    RawText = model.JobDescriptionText.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                int jdId = await _analysisRepository.CreateJobDescriptionAsync(jobDescription);
                if (jdId <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Failed to record the job description in the database.");
                    return View(model);
                }

                // 2. Perform Intelligent 4-Pillar Resume Analysis
                var analysisResult = await _analysisService.AnalyzeResumeAsync(
                    selectedResume.ExtractedText,
                    jobDescription.RawText,
                    selectedResume.ResumeId,
                    jdId,
                    userId.Value);

                // 3. Atomically persist Analysis, Skills, and Recommendations
                int analysisId = await _analysisRepository.SaveAnalysisAsync(analysisResult);
                if (analysisId <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Failed to record the analysis results in the database.");
                    return View(model);
                }

                TempData["SuccessMessage"] = $"Analysis completed! Compatibility score: {analysisResult.OverallScore:F1}%.";
                return RedirectToAction("Result", new { id = analysisId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing resume analysis for user {UserId}", userId.Value);
                ModelState.AddModelError(string.Empty, $"An unexpected error occurred during analysis: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Analysis/Result/5
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            string? role = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            ResumeAnalysis? analysis;
            // Admins can inspect any analysis result; normal users only their own
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                analysis = await _analysisRepository.GetByIdAsync(id);
            }
            else
            {
                analysis = await _analysisRepository.GetByIdAndUserAsync(id, userId.Value);
            }

            if (analysis == null)
            {
                TempData["ErrorMessage"] = "Analysis report not found or access is unauthorized.";
                return RedirectToAction("History");
            }

            return View(analysis);
        }

        // GET: /Analysis/History
        [HttpGet]
        public async Task<IActionResult> History(string? search)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var analyses = await _analysisRepository.GetByUserIdAsync(userId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string query = search.Trim().ToLowerInvariant();
                analyses = analyses.Where(a =>
                    (a.JobTitle != null && a.JobTitle.ToLowerInvariant().Contains(query)) ||
                    (a.CompanyName != null && a.CompanyName.ToLowerInvariant().Contains(query)) ||
                    (a.ResumeFileName != null && a.ResumeFileName.ToLowerInvariant().Contains(query))).ToList();
            }

            var model = new AnalysisHistoryViewModel
            {
                Analyses = analyses,
                SearchQuery = search
            };

            return View(model);
        }

        // GET: /Analysis/PrintReport/5
        [HttpGet]
        public async Task<IActionResult> PrintReport(int id)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            string? role = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            ResumeAnalysis? analysis;
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                analysis = await _analysisRepository.GetByIdAsync(id);
            }
            else
            {
                analysis = await _analysisRepository.GetByIdAndUserAsync(id, userId.Value);
            }

            if (analysis == null)
            {
                TempData["ErrorMessage"] = "Analysis record not found.";
                return RedirectToAction("History");
            }

            return View(analysis);
        }
    }
}
