using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using AIResumeAnalyzer.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ILogger<AccountController> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userRepository.GetByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var (hash, salt) = _passwordHasher.HashPassword(model.Password);

            var newUser = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            int userId = await _userRepository.CreateAsync(newUser);
            if (userId <= 0)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating your account. Please try again.");
                return View(model);
            }

            // Auto-login newly registered user
            HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
            HttpContext.Session.SetString(SessionKeys.UserName, newUser.FullName);
            HttpContext.Session.SetString(SessionKeys.UserEmail, newUser.Email);
            HttpContext.Session.SetString(SessionKeys.UserRole, newUser.Role);

            TempData["SuccessMessage"] = $"Welcome to AI Resume Analyzer, {newUser.FullName}! Your account is now active.";
            return RedirectToAction("Index", "Dashboard");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
            {
                string? role = HttpContext.Session.GetString(SessionKeys.UserRole);
                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userRepository.GetByEmailAsync(model.Email);
            if (user == null || !_passwordHasher.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "Invalid email address or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support or the administrator.");
                return View(model);
            }

            // Set session claims
            HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
            HttpContext.Session.SetString(SessionKeys.UserName, user.FullName);
            HttpContext.Session.SetString(SessionKeys.UserEmail, user.Email);
            HttpContext.Session.SetString(SessionKeys.UserRole, user.Role);

            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "You have been successfully logged out.";
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/Profile
        [HttpGet]
        [SessionAuthorize]
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new UserProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                MemberSince = user.CreatedAt
            };

            return View(model);
        }

        // POST: /Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
        {
            int? sessionUserId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!sessionUserId.HasValue)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["ErrorMessage"] = "Name and Email are required.";
                return RedirectToAction("Profile");
            }

            // Check if email changed and is taken by another user
            var existing = await _userRepository.GetByEmailAsync(model.Email);
            if (existing != null && existing.UserId != sessionUserId.Value)
            {
                TempData["ErrorMessage"] = "The email address is already in use by another account.";
                return RedirectToAction("Profile");
            }

            bool success = await _userRepository.UpdateProfileAsync(sessionUserId.Value, model.FullName.Trim(), model.Email.Trim());
            if (success)
            {
                HttpContext.Session.SetString(SessionKeys.UserName, model.FullName.Trim());
                HttpContext.Session.SetString(SessionKeys.UserEmail, model.Email.Trim().ToLowerInvariant());
                TempData["SuccessMessage"] = "Profile details updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
            }

            return RedirectToAction("Profile");
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize]
        public async Task<IActionResult> ChangePassword(UserProfileViewModel model)
        {
            int? sessionUserId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!sessionUserId.HasValue)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmNewPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction("Profile");
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation do not match.";
                return RedirectToAction("Profile");
            }

            if (model.NewPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "New password must be at least 6 characters long.";
                return RedirectToAction("Profile");
            }

            var user = await _userRepository.GetByIdAsync(sessionUserId.Value);
            if (user == null || !_passwordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                TempData["ErrorMessage"] = "Your current password is incorrect.";
                return RedirectToAction("Profile");
            }

            var (newHash, newSalt) = _passwordHasher.HashPassword(model.NewPassword);
            bool updated = await _userRepository.UpdatePasswordAsync(sessionUserId.Value, newHash, newSalt);

            if (updated)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to change password. Please try again.";
            }

            return RedirectToAction("Profile");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
