using System.Security.Cryptography;
using AIResumeAnalyzer.Interfaces;

namespace AIResumeAnalyzer.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32;  // 256 bits
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public (string Hash, string Salt) HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            }

            // Normalize password by trimming accidental leading/trailing whitespace & control chars
            string normalized = NormalizePassword(password);

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(normalized, salt, Iterations, Algorithm, KeySize);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(storedSalt);
                byte[] expectedHash = Convert.FromBase64String(storedHash);

                // 1. Check normalized (trimmed & sanitized) password (handles password manager copy-paste whitespace)
                string normalized = NormalizePassword(password);
                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(normalized, salt, Iterations, Algorithm, KeySize);
                if (CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
                {
                    return true;
                }

                // 2. If different, also check raw password in case stored hash was generated without normalization
                if (!string.Equals(password, normalized, StringComparison.Ordinal))
                {
                    byte[] rawActualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
                    if (CryptographicOperations.FixedTimeEquals(rawActualHash, expectedHash))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizePassword(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return input
                .Trim()
                .Trim('\r', '\n', '\t', ' ')
                .Replace("\u200B", "") // Zero-width space
                .Replace("\uFEFF", "") // Zero-width no-break space
                .Replace("\u00A0", " "); // Non-breaking space
        }
    }
}
