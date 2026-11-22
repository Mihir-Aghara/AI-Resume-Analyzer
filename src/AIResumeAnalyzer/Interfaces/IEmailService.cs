namespace AIResumeAnalyzer.Interfaces
{
    /// <summary>
    /// Service contract for dispatching transactional email communications
    /// (e.g., account confirmation, welcome onboarding, security alerts, and password reset).
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Dispatches an onboarding welcome email upon successful user registration.
        /// </summary>
        Task<bool> SendWelcomeEmailAsync(string toEmail, string recipientName);

        /// <summary>
        /// Dispatches a time-sensitive password reset link with a secure cryptographically generated token.
        /// </summary>
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetLink);

        /// <summary>
        /// Dispatches a generic transactional email message.
        /// </summary>
        Task<bool> SendGenericEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
