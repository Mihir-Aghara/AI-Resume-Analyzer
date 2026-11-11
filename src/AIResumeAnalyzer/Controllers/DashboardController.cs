using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers
{
    [SessionAuthorize]
    public class DashboardController : Controller
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IAnalysisRepository _analysisRepository;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IResumeRepository resumeRepository,
            IAnalysisRepository analysisRepository,
            ILogger<DashboardController> logger)
        {
            _resumeRepository = resumeRepository;
            _analysisRepository = analysisRepository;
            _logger = logger;
        }

        // GET: /Dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            int id = userId.Value;

            var summary = new UserDashboardSummary
            {
                TotalResumes = await _resumeRepository.GetCountByUserIdAsync(id),
                TotalAnalyses = await _analysisRepository.GetCountByUserIdAsync(id),
                AverageScore = await _analysisRepository.GetAverageScoreByUserIdAsync(id),
                HighestScore = await _analysisRepository.GetHighestScoreByUserIdAsync(id),
                RecentAnalyses = await _analysisRepository.GetRecentByUserIdAsync(id, 5),
                RecentResumes = (await _resumeRepository.GetByUserIdAsync(id)).Take(5).ToList()
            };

            return View(summary);
        }
    }
}
