using AIResumeAnalyzer.Services;
using Xunit;

namespace AIResumeAnalyzer.Tests
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void HashPassword_ShouldReturnValidHashAndSalt()
        {
            var (hash, salt) = _hasher.HashPassword("TestPassword@123");

            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.False(string.IsNullOrWhiteSpace(salt));
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
        {
            var (hash, salt) = _hasher.HashPassword("SecretPassword123!");
            bool result = _hasher.VerifyPassword("SecretPassword123!", hash, salt);

            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
        {
            var (hash, salt) = _hasher.HashPassword("SecretPassword123!");
            bool result = _hasher.VerifyPassword("WrongPassword!", hash, salt);

            Assert.False(result);
        }

        [Fact]
        public void OutputSampleHashesForSeedData()
        {
            var admin = _hasher.HashPassword("Admin@123");
            Assert.True(_hasher.VerifyPassword("Admin@123", admin.Hash, admin.Salt));

            var student = _hasher.HashPassword("Student@123");
            Assert.True(_hasher.VerifyPassword("Student@123", student.Hash, student.Salt));
        }
    }
}
