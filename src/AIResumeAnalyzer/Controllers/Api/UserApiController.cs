using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers.Api
{
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    public class UserApiController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IResumeRepository _resumeRepository;
        private readonly IAnalysisRepository _analysisRepository;

        public UserApiController(
            IUserRepository userRepository,
            IResumeRepository resumeRepository,
            IAnalysisRepository analysisRepository)
        {
            _userRepository = userRepository;
            _resumeRepository = resumeRepository;
            _analysisRepository = analysisRepository;
        }

        // GET: /api/users/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.Fail("User is not authenticated."));
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail("User account not found."));
            }

            // Exclude password hash and salt from API response
            var safeUser = new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Role,
                user.CreatedAt,
                user.IsActive
            };

            return Ok(ApiResponse<object>.Ok(safeUser, "Profile retrieved successfully."));
        }

        // GET: /api/users/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.Fail("User is not authenticated."));
            }

            int id = userId.Value;
            var stats = new
            {
                TotalResumes = await _resumeRepository.GetCountByUserIdAsync(id),
                TotalAnalyses = await _analysisRepository.GetCountByUserIdAsync(id),
                AverageScore = await _analysisRepository.GetAverageScoreByUserIdAsync(id),
                HighestScore = await _analysisRepository.GetHighestScoreByUserIdAsync(id)
            };

            return Ok(ApiResponse<object>.Ok(stats, "User statistics retrieved successfully."));
        }
    }
}
