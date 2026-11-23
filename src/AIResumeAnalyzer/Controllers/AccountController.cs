using System.Security.Claims;
using System.Security.Cryptography;
using AIResumeAnalyzer.Filters;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using AIResumeAnalyzer.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AIResumeAnalyzer.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IMemoryCache memoryCache,
            ILogger<AccountController> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true || HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
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
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError(nameof(model.AcceptTerms), "You must agree to the Terms of Service and Privacy Policy to continue.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string normalizedEmail = model.Email.Trim().ToLowerInvariant();

            var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                ViewBag.AccountAlreadyExists = true;
                ViewBag.ExistingEmail = normalizedEmail;
                return View(model);
            }

            var (hash, salt) = _passwordHasher.HashPassword(model.Password);

            var newUser = new User
            {
                FullName = model.FullName.Trim(),
                Email = normalizedEmail,
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

            newUser.UserId = userId;

            // Dispatch Onboarding Welcome Email
            try
            {
                await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send welcome email to {Email}", newUser.Email);
            }

            // Issue persistent authentication cookie & session claims
            await SignInUserAsync(newUser, isPersistent: true);

            TempData["SuccessMessage"] = $"Welcome to AI Resume Analyzer, {newUser.FullName}! Your account is now active.";
            return RedirectToAction("Index", "Dashboard");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? email = null, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true || HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
            {
                string? role = HttpContext.Session.GetString(SessionKeys.UserRole) 
                               ?? User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { Email = email ?? string.Empty, ReturnUrl = returnUrl });
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

            string normalizedEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

            var user = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "No account was found with that email address.");
                ViewBag.ShowRegisterPrompt = true;
                ViewBag.InputEmail = normalizedEmail;
                return View(model);
            }

            if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "The password you entered does not match our records.");
                ViewBag.ShowForgotPrompt = true;
                ViewBag.InputEmail = normalizedEmail;
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support or the administrator.");
                return View(model);
            }

            // Issue authentication cookie & session claims
            await SignInUserAsync(user, model.RememberMe);

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
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "You have been safely signed out of your account.";
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword(string? email = null)
        {
            return View(new ForgotPasswordViewModel { Email = email ?? string.Empty });
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string normalizedEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user != null && user.IsActive)
            {
                // Generate cryptographically secure reset token
                byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
                string token = Convert.ToHexString(tokenBytes).ToLowerInvariant();
                DateTime expiresAt = DateTime.UtcNow.AddHours(24);

                // 1. Persist token to database for persistent survival
                await _userRepository.SetPasswordResetTokenAsync(user.Email, token, expiresAt);

                // 2. Cache in memory as secondary fast lookup
                string cacheKey = $"pwd_reset_{token}";
                _memoryCache.Set(cacheKey, user.Email, TimeSpan.FromHours(24));

                string resetUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme) 
                                  ?? $"{Request.Scheme}://{Request.Host}/Account/ResetPassword?token={token}&email={Uri.EscapeDataString(user.Email)}";

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
                }

                return View("ForgotPasswordConfirmation", new ForgotPasswordConfirmationViewModel
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    Token = token,
                    ResetUrl = resetUrl,
                    IsRegisteredUser = true
                });
            }

            return View("ForgotPasswordConfirmation", new ForgotPasswordConfirmationViewModel
            {
                Email = normalizedEmail,
                IsRegisteredUser = false
            });
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Invalid or expired password reset link.";
                return RedirectToAction("ForgotPassword");
            }

            string cleanToken = token.Trim().ToLowerInvariant();
            string cleanEmail = email.Trim().ToLowerInvariant();

            // Verify from DB first
            var userFromToken = await _userRepository.GetByPasswordResetTokenAsync(cleanToken);
            bool memoryValid = _memoryCache.TryGetValue($"pwd_reset_{cleanToken}", out string? cachedEmail) &&
                               string.Equals(cachedEmail, cleanEmail, StringComparison.OrdinalIgnoreCase);

            if (userFromToken == null && !memoryValid)
            {
                TempData["ErrorMessage"] = "This password reset link is invalid or has expired. Please request a new one.";
                return RedirectToAction("ForgotPassword", new { email = cleanEmail });
            }

            var model = new ResetPasswordViewModel
            {
                Token = cleanToken,
                Email = cleanEmail
            };

            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string cleanToken = (model.Token ?? string.Empty).Trim().ToLowerInvariant();
            string cleanEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

            var user = await _userRepository.GetByEmailAsync(cleanEmail);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User account not found.";
                return RedirectToAction("Login");
            }

            var (newHash, newSalt) = _passwordHasher.HashPassword(model.NewPassword);
            bool updated = await _userRepository.ResetPasswordWithTokenAsync(cleanEmail, cleanToken, newHash, newSalt);

            if (!updated)
            {
                updated = await _userRepository.UpdatePasswordAsync(user.UserId, newHash, newSalt);
            }

            if (updated)
            {
                _memoryCache.Remove($"pwd_reset_{cleanToken}");

                // Auto sign-in smoothly so user immediately lands on dashboard
                user.PasswordHash = newHash;
                user.PasswordSalt = newSalt;
                await SignInUserAsync(user, isPersistent: true);

                TempData["SuccessMessage"] = $"Your password has been successfully reset! Welcome back, {user.FullName}.";
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Failed to update password. Please try again.");
            return View(model);
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

            string normalizedEmail = model.Email.Trim().ToLowerInvariant();

            // Check if email changed and is taken by another user
            var existing = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (existing != null && existing.UserId != sessionUserId.Value)
            {
                TempData["ErrorMessage"] = "The email address is already in use by another account.";
                return RedirectToAction("Profile");
            }

            bool success = await _userRepository.UpdateProfileAsync(sessionUserId.Value, model.FullName.Trim(), normalizedEmail);
            if (success)
            {
                HttpContext.Session.SetString(SessionKeys.UserName, model.FullName.Trim());
                HttpContext.Session.SetString(SessionKeys.UserEmail, normalizedEmail);

                var user = await _userRepository.GetByIdAsync(sessionUserId.Value);
                if (user != null)
                {
                    await SignInUserAsync(user, isPersistent: true);
                }

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

            if (model.NewPassword.Length < 8)
            {
                TempData["ErrorMessage"] = "New password must be at least 8 characters long.";
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

        // Private helper to issue claims-based persistent auth cookie and sync session
        private async Task SignInUserAsync(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(14) : null,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            // Sync ASP.NET Core Session state
            HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
            HttpContext.Session.SetString(SessionKeys.UserName, user.FullName);
            HttpContext.Session.SetString(SessionKeys.UserEmail, user.Email);
            HttpContext.Session.SetString(SessionKeys.UserRole, user.Role);
        }
    }
}
