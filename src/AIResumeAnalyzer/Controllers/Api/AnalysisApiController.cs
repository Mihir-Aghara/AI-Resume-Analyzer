using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers.Api
{
    public class ApiAnalysisRequest
    {
        public int ResumeId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string JobDescriptionText { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/analyses")]
    [Produces("application/json")]
    public class AnalysisApiController : ControllerBase
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IAnalysisRepository _analysisRepository;
        private readonly IResumeAnalysisService _analysisService;
        private readonly ILogger<AnalysisApiController> _logger;

        public AnalysisApiController(
            IResumeRepository resumeRepository,
            IAnalysisRepository analysisRepository,
            IResumeAnalysisService analysisService,
            ILogger<AnalysisApiController> logger)
        {
            _resumeRepository = resumeRepository;
            _analysisRepository = analysisRepository;
            _analysisService = analysisService;
            _logger = logger;
        }

        // GET: /api/analyses
        [HttpGet]
        public async Task<IActionResult> GetMyAnalyses()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.Fail("User is not authenticated."));
            }

            var analyses = await _analysisRepository.GetByUserIdAsync(userId.Value);
            return Ok(ApiResponse<List<ResumeAnalysis>>.Ok(analyses, "Analysis history retrieved successfully."));
        }

        // GET: /api/analyses/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAnalysisById(int id)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            string? role = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.Fail("User is not authenticated."));
            }

            ResumeAnalysis? analysis = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                ? await _analysisRepository.GetByIdAsync(id)
                : await _analysisRepository.GetByIdAndUserAsync(id, userId.Value);

            if (analysis == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Analysis report with ID {id} was not found."));
            }

            return Ok(ApiResponse<ResumeAnalysis>.Ok(analysis, "Analysis details retrieved successfully."));
        }

        // POST: /api/analyses
        [HttpPost]
        public async Task<IActionResult> CreateAnalysis([FromBody] ApiAnalysisRequest request)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.Fail("User is not authenticated."));
            }

            if (request == null || string.IsNullOrWhiteSpace(request.JobTitle) || string.IsNullOrWhiteSpace(request.JobDescriptionText))
            {
                return BadRequest(ApiResponse<object>.Fail("JobTitle and JobDescriptionText are mandatory fields."));
            }

            var resume = await _resumeRepository.GetByIdAndUserAsync(request.ResumeId, userId.Value);
            if (resume == null)
            {
                return BadRequest(ApiResponse<object>.Fail($"Resume with ID {request.ResumeId} does not exist or does not belong to you."));
            }

            try
            {
                var jobDescription = new JobDescription
                {
                    UserId = userId.Value,
                    JobTitle = request.JobTitle.Trim(),
                    CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim(),
                    RawText = request.JobDescriptionText.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                int jdId = await _analysisRepository.CreateJobDescriptionAsync(jobDescription);
                var analysis = await _analysisService.AnalyzeResumeAsync(
                    resume.ExtractedText,
                    jobDescription.RawText,
                    resume.ResumeId,
                    jdId,
                    userId.Value);

                int analysisId = await _analysisRepository.SaveAnalysisAsync(analysis);
                analysis.AnalysisId = analysisId;

                return CreatedAtAction(nameof(GetAnalysisById), new { id = analysisId }, ApiResponse<ResumeAnalysis>.Ok(analysis, "Analysis completed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Analysis failed for User {UserId}", userId.Value);
                return StatusCode(500, ApiResponse<object>.Fail($"Analysis processing failed: {ex.Message}"));
            }
        }
    }
}
