using System.Net;
using System.Text.Json;
using AIResumeAnalyzer.Models;

namespace AIResumeAnalyzer.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred while processing request {Path}", context.Request.Path);

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    await HandleApiExceptionAsync(context, ex);
                }
                else if (!context.Response.HasStarted && !context.Request.Path.StartsWithSegments("/Home/Error"))
                {
                    context.Response.Clear();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Items["ErrorPath"] = context.Request.Path.Value;
                    if (_env.IsDevelopment())
                    {
                        context.Items["ErrorDetail"] = $"{ex.GetType().Name}: {ex.Message}";
                    }

                    // Re-execute the polished Error view cleanly (works for both GET and POST failures)
                    context.Request.Path = "/Home/Error";
                    context.Request.Method = HttpMethods.Get;
                    await _next(context);
                }
            }
        }

        private Task HandleApiExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            string message = _env.IsDevelopment()
                ? exception.Message
                : "An unexpected internal server error occurred while processing your request.";

            var response = ApiResponse<object>.Fail(message);
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return context.Response.WriteAsync(json);
        }
    }
}
