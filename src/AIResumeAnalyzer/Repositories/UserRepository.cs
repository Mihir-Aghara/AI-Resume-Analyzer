using System.Data;
using AIResumeAnalyzer.Data;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.Data.SqlClient;

namespace AIResumeAnalyzer.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _db;

        public UserRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            const string sql = @"
                SELECT UserId, FullName, Email, PasswordHash, PasswordSalt, Role, CreatedAt, UpdatedAt, IsActive
                FROM Users
                WHERE UserId = @UserId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }

            return null;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT UserId, FullName, Email, PasswordHash, PasswordSalt, Role, CreatedAt, UpdatedAt, IsActive
                FROM Users
                WHERE Email = @Email;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = email.Trim().ToLowerInvariant() });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }

            return null;
        }

        public async Task<int> CreateAsync(User user)
        {
            const string sql = @"
                INSERT INTO Users (FullName, Email, PasswordHash, PasswordSalt, Role, CreatedAt, IsActive)
                OUTPUT INSERTED.UserId
                VALUES (@FullName, @Email, @PasswordHash, @PasswordSalt, @Role, @CreatedAt, @IsActive);";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = user.FullName });
            cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = user.Email.Trim().ToLowerInvariant() });
            cmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = user.PasswordHash });
            cmd.Parameters.Add(new SqlParameter("@PasswordSalt", SqlDbType.NVarChar, 128) { Value = user.PasswordSalt });
            cmd.Parameters.Add(new SqlParameter("@Role", SqlDbType.NVarChar, 20) { Value = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role });
            cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = user.CreatedAt });
            cmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive });

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<bool> UpdateProfileAsync(int userId, string fullName, string email)
        {
            const string sql = @"
                UPDATE Users
                SET FullName = @FullName,
                    Email = @Email,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = fullName });
            cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = email.Trim().ToLowerInvariant() });

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash, string passwordSalt)
        {
            const string sql = @"
                UPDATE Users
                SET PasswordHash = @PasswordHash,
                    PasswordSalt = @PasswordSalt,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = passwordHash });
            cmd.Parameters.Add(new SqlParameter("@PasswordSalt", SqlDbType.NVarChar, 128) { Value = passwordSalt });

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> SetPasswordResetTokenAsync(string email, string token, DateTime expiresAt)
        {
            const string sql = @"
                UPDATE Users
                SET ResetToken = @ResetToken,
                    ResetTokenExpiresAt = @ExpiresAt,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Email = @Email AND IsActive = 1;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@ResetToken", SqlDbType.NVarChar, 100) { Value = token });
            cmd.Parameters.Add(new SqlParameter("@ExpiresAt", SqlDbType.DateTime2) { Value = expiresAt });
            cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = email.Trim().ToLowerInvariant() });

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            const string sql = @"
                SELECT UserId, FullName, Email, PasswordHash, PasswordSalt, Role, CreatedAt, UpdatedAt, IsActive, ResetToken, ResetTokenExpiresAt
                FROM Users
                WHERE ResetToken = @ResetToken
                  AND ResetTokenExpiresAt IS NOT NULL
                  AND ResetTokenExpiresAt > SYSUTCDATETIME()
                  AND IsActive = 1;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@ResetToken", SqlDbType.NVarChar, 100) { Value = token.Trim() });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }

            return null;
        }

        public async Task<bool> ResetPasswordWithTokenAsync(string email, string token, string newHash, string newSalt)
        {
            const string sql = @"
                UPDATE Users
                SET PasswordHash = @PasswordHash,
                    PasswordSalt = @PasswordSalt,
                    ResetToken = NULL,
                    ResetTokenExpiresAt = NULL,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Email = @Email
                  AND IsActive = 1;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 256) { Value = newHash });
            cmd.Parameters.Add(new SqlParameter("@PasswordSalt", SqlDbType.NVarChar, 128) { Value = newSalt });
            cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = email.Trim().ToLowerInvariant() });

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            const string sql = @"
                SELECT UserId, FullName, Email, PasswordHash, PasswordSalt, Role, CreatedAt, UpdatedAt, IsActive
                FROM Users
                ORDER BY CreatedAt DESC;";

            var list = new List<User>();
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapUser(reader));
            }

            return list;
        }

        public async Task<bool> ToggleUserStatusAsync(int userId, bool isActive)
        {
            const string sql = @"
                UPDATE Users
                SET IsActive = @IsActive,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive });

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static User MapUser(SqlDataReader reader)
        {
            var user = new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                PasswordSalt = reader.GetString(reader.GetOrdinal("PasswordSalt")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            };

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                if (colName.Equals("ResetToken", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))
                {
                    user.ResetToken = reader.GetString(i);
                }
                else if (colName.Equals("ResetTokenExpiresAt", StringComparison.OrdinalIgnoreCase) && !reader.IsDBNull(i))
                {
                    user.ResetTokenExpiresAt = reader.GetDateTime(i);
                }
            }

            return user;
        }
    }
}
