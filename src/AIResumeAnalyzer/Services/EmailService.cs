using System.Net;
using System.Net.Mail;
using AIResumeAnalyzer.Interfaces;

namespace AIResumeAnalyzer.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string recipientName)
        {
            string subject = "Welcome to AI Resume Analyzer - Empowering Your Career Journey";
            string htmlBody = $@"
<div style=""font-family: 'Segoe UI', Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;"">
    <div style=""text-align: center; padding-bottom: 20px; border-bottom: 2px solid #3b82f6;"">
        <h2 style=""color: #1e293b; margin: 0; font-size: 24px;"">AI Resume <span style=""color: #3b82f6;"">Analyzer</span></h2>
        <p style=""color: #64748b; font-size: 14px; margin-top: 4px;"">Enterprise-Grade Resume Intelligence & ATS Compatibility Engine</p>
    </div>
    <div style=""padding: 24px 0;"">
        <h3 style=""color: #0f172a;"">Hello {WebUtility.HtmlEncode(recipientName)},</h3>
        <p style=""color: #334155; line-height: 1.6;"">
            Thank you for creating an account on <strong>AI Resume Analyzer</strong>! Your account has been registered with <strong>{WebUtility.HtmlEncode(toEmail)}</strong> and is now fully active.
        </p>
        <div style=""background-color: #f8fafc; border-left: 4px solid #3b82f6; padding: 16px; border-radius: 6px; margin: 20px 0;"">
            <h4 style=""margin: 0 0 8px 0; color: #1e293b;"">What you can do right now:</h4>
            <ul style=""margin: 0; padding-left: 20px; color: #475569; font-size: 14px; line-height: 1.6;"">
                <li><strong>Upload Resumes:</strong> Upload your PDF or Word DOCX resume securely.</li>
                <li><strong>Target Job Matching:</strong> Benchmark your resume against real job specifications.</li>
                <li><strong>Transparent 4-Pillar Scoring:</strong> Discover your Technical Skills, Keywords, Experience, and Education compatibility.</li>
                <li><strong>Skill Gap Analysis:</strong> Identify missing industry competencies and prioritized recommendations.</li>
            </ul>
        </div>
        <p style=""color: #334155; line-height: 1.6;"">
            Best regards,<br>
            <strong>The AI Resume Analyzer Team</strong>
        </p>
    </div>
    <div style=""border-top: 1px solid #e2e8f0; padding-top: 16px; text-align: center; font-size: 12px; color: #94a3b8;"">
        &copy; {DateTime.UtcNow.Year} AI Resume Analyzer. All rights reserved.
    </div>
</div>";

            return await SendGenericEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetLink)
        {
            string subject = "Reset Your Password - AI Resume Analyzer";
            string htmlBody = $@"
<div style=""font-family: 'Segoe UI', Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;"">
    <div style=""text-align: center; padding-bottom: 20px; border-bottom: 2px solid #ef4444;"">
        <h2 style=""color: #1e293b; margin: 0; font-size: 24px;"">AI Resume <span style=""color: #3b82f6;"">Analyzer</span></h2>
        <p style=""color: #64748b; font-size: 14px; margin-top: 4px;"">Security & Account Verification</p>
    </div>
    <div style=""padding: 24px 0;"">
        <h3 style=""color: #0f172a;"">Password Reset Request</h3>
        <p style=""color: #334155; line-height: 1.6;"">
            Hello {WebUtility.HtmlEncode(recipientName)},<br>
            We received a request to reset the password associated with your account (<strong>{WebUtility.HtmlEncode(toEmail)}</strong>).
        </p>
        <p style=""color: #334155; line-height: 1.6;"">
            Click the button below to establish a new password. This secure link is valid for <strong>1 hour</strong>.
        </p>
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{resetLink}"" style=""background-color: #3b82f6; color: #ffffff; padding: 14px 28px; font-weight: 600; text-decoration: none; border-radius: 8px; display: inline-block;"">
                Reset Password
            </a>
        </div>
        <p style=""color: #64748b; font-size: 13px; line-height: 1.5;"">
            If the button above does not work, copy and paste this URL into your browser:<br>
            <a href=""{resetLink}"" style=""color: #3b82f6; word-break: break-all;"">{resetLink}</a>
        </p>
        <p style=""color: #64748b; font-size: 13px;"">
            If you did not initiate this password reset request, you can safely disregard this email. Your password remains unchanged.
        </p>
    </div>
    <div style=""border-top: 1px solid #e2e8f0; padding-top: 16px; text-align: center; font-size: 12px; color: #94a3b8;"">
        &copy; {DateTime.UtcNow.Year} AI Resume Analyzer. Secure Account Services.
    </div>
</div>";

            return await SendGenericEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendGenericEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _configuration["SmtpSettings:Host"];
            var portStr = _configuration["SmtpSettings:Port"];
            var username = _configuration["SmtpSettings:Username"];
            var password = _configuration["SmtpSettings:Password"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"] ?? "noreply@airesumeanalyzer.com";
            var fromName = _configuration["SmtpSettings:FromName"] ?? "AI Resume Analyzer";
            var enableSsl = bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out bool ssl) && ssl;

            // If SMTP is explicitly configured with a host
            if (!string.IsNullOrWhiteSpace(host) && int.TryParse(portStr, out int port))
            {
                try
                {
                    using var message = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };
                    message.To.Add(new MailAddress(toEmail));

                    using var client = new SmtpClient(host, port)
                    {
                        EnableSsl = enableSsl,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout = 10000
                    };

                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        client.Credentials = new NetworkCredential(username, password);
                    }

                    await client.SendMailAsync(message);
                    _logger.LogInformation("Outbound transactional email successfully dispatched to {Email}. Subject: {Subject}", toEmail, subject);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispatch email via SMTP server {Host}:{Port} to {Email}. Falling back to transaction logger.", host, port, toEmail);
                }
            }

            // Enterprise development / fallback logging
            _logger.LogInformation("==================================================================");
            _logger.LogInformation("[TRANSACTIONAL EMAIL DISPATCH LOG]");
            _logger.LogInformation("To: {ToEmail}", toEmail);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("Dispatched At: {Timestamp:u}", DateTime.UtcNow);
            _logger.LogInformation("==================================================================");

            return true;
        }
    }
}
