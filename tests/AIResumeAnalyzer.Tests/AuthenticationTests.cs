using AIResumeAnalyzer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIResumeAnalyzer.Tests
{
    public class AuthenticationTests
    {
        [Fact]
        public async Task EmailService_SendWelcomeEmailAsync_ShouldSucceedWithFallbackLogging()
        {
            var config = new ConfigurationBuilder().Build();
            var logger = NullLogger<EmailService>.Instance;
            var emailService = new EmailService(config, logger);

            bool result = await emailService.SendWelcomeEmailAsync("test.user@example.com", "Test User");

            Assert.True(result);
        }

        [Fact]
        public async Task EmailService_SendPasswordResetEmailAsync_ShouldSucceedWithFallbackLogging()
        {
            var config = new ConfigurationBuilder().Build();
            var logger = NullLogger<EmailService>.Instance;
            var emailService = new EmailService(config, logger);

            string resetLink = "https://airesumeanalyzer.local/Account/ResetPassword?token=test1234&email=test%40example.com";
            bool result = await emailService.SendPasswordResetEmailAsync("test@example.com", "Test User", resetLink);

            Assert.True(result);
        }

        [Theory]
        [InlineData("User@Example.COM", "user@example.com")]
        [InlineData("  Alex.Smith@GMAIL.com  ", "alex.smith@gmail.com")]
        [InlineData("sachin.kumar@company.org", "sachin.kumar@company.org")]
        public void EmailNormalization_ShouldTrimAndLowerCase(string input, string expected)
        {
            string normalized = input.Trim().ToLowerInvariant();
            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void PasswordHasher_WithEnterprisePassword_ShouldHashAndVerifySuccessfully()
        {
            var hasher = new PasswordHasher();
            string strongPwd = "SecureEnterprisePass123!";

            var (hash, salt) = hasher.HashPassword(strongPwd);

            Assert.NotNull(hash);
            Assert.NotNull(salt);
            Assert.True(hasher.VerifyPassword(strongPwd, hash, salt));
            Assert.False(hasher.VerifyPassword("WrongPassword123!", hash, salt));
        }
    }
}
