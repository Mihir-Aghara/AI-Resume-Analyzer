using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AIResumeAnalyzer.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            int? userId = session.GetInt32(SessionKeys.UserId);
            string? role = session.GetString(SessionKeys.UserRole);

            // Auto-hydrate session from ClaimsPrincipal if persistent Auth Cookie is active
            if ((!userId.HasValue || userId.Value <= 0) && context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var claims = context.HttpContext.User;
                var idClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int parsedId))
                {
                    userId = parsedId;
                    role = claims.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                    session.SetInt32(SessionKeys.UserId, parsedId);
                    session.SetString(SessionKeys.UserName, claims.FindFirst(ClaimTypes.Name)?.Value ?? "User");
                    session.SetString(SessionKeys.UserEmail, claims.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty);
                    session.SetString(SessionKeys.UserRole, role);
                }
            }

            if (!userId.HasValue || userId.Value <= 0)
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
