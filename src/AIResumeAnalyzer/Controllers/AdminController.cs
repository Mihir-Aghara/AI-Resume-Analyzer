using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers
{
    [AdminAuthorize]
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminRepository adminRepository,
            IUserRepository userRepository,
            ILogger<AdminController> logger)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        // GET: /Admin/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var summary = await _adminRepository.GetDashboardSummaryAsync();
            return View(summary);
        }

        // GET: /Admin/Users
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _adminRepository.GetAllUsersAsync();
            return View(users);
        }

        // POST: /Admin/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int userId, bool isActive)
        {
            int? currentAdminId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (currentAdminId.HasValue && currentAdminId.Value == userId && !isActive)
            {
                TempData["ErrorMessage"] = "Security Rule: You cannot deactivate your own active administrator account.";
                return RedirectToAction("Users");
            }

            bool success = await _userRepository.ToggleUserStatusAsync(userId, isActive);
            if (success)
            {
                TempData["SuccessMessage"] = $"User status updated to {(isActive ? "Active" : "Deactivated")}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update user status.";
            }

            return RedirectToAction("Users");
        }

        // POST: /Admin/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int userId, string role)
        {
            if (role != "User" && role != "Admin")
            {
                TempData["ErrorMessage"] = "Invalid role specified.";
                return RedirectToAction("Users");
            }

            int? currentAdminId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (currentAdminId.HasValue && currentAdminId.Value == userId && role == "User")
            {
                TempData["ErrorMessage"] = "Security Rule: You cannot demote yourself from the Administrator role.";
                return RedirectToAction("Users");
            }

            bool success = await _adminRepository.UpdateUserRoleAsync(userId, role);
            if (success)
            {
                TempData["SuccessMessage"] = $"User role updated to '{role}' successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update user role.";
            }

            return RedirectToAction("Users");
        }
    }
}
