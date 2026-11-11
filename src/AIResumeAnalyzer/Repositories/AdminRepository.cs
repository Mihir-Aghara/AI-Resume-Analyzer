using System.Data;
using AIResumeAnalyzer.Data;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.Data.SqlClient;

namespace AIResumeAnalyzer.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly IDbConnectionFactory _db;

        public AdminRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<AdminDashboardSummary> GetDashboardSummaryAsync()
        {
            var summary = new AdminDashboardSummary();

            const string statsSql = @"
                SELECT 
                    (SELECT COUNT(*) FROM Users) AS TotalUsers,
                    (SELECT COUNT(*) FROM Resumes) AS TotalResumes,
                    (SELECT COUNT(*) FROM ResumeAnalyses) AS TotalAnalyses,
                    (SELECT ISNULL(AVG(OverallScore), 0.0) FROM ResumeAnalyses) AS SystemAverageScore;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using (var cmd = new SqlCommand(statsSql, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    summary.TotalUsers = reader.GetInt32(0);
                    summary.TotalResumes = reader.GetInt32(1);
                    summary.TotalAnalyses = reader.GetInt32(2);
                    summary.SystemAverageScore = reader.GetDecimal(3);
                }
            }

            // Recent Users
            const string recentUsersSql = @"
                SELECT TOP (5) UserId, FullName, Email, PasswordHash, PasswordSalt, Role, CreatedAt, UpdatedAt, IsActive
                FROM Users
                ORDER BY CreatedAt DESC;";

            await using (var cmd = new SqlCommand(recentUsersSql, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    summary.RecentUsers.Add(new User
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Role = reader.GetString(reader.GetOrdinal("Role")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                    });
                }
            }

            // Recent Analyses
            const string recentAnalysesSql = @"
                SELECT TOP (5) a.AnalysisId, a.ResumeId, a.JobDescriptionId, a.UserId,
                       a.OverallScore, a.SkillsScore, a.KeywordsScore, a.ExperienceScore, a.EducationScore,
                       a.MatchedSkillsCount, a.MissingSkillsCount, a.TotalSkillsIdentified,
                       a.CalculationExplanation, a.CreatedAt,
                       r.OriginalFileName AS ResumeFileName,
                       j.JobTitle, j.CompanyName,
                       u.FullName AS UserFullName
                FROM ResumeAnalyses a
                INNER JOIN Resumes r ON a.ResumeId = r.ResumeId
                INNER JOIN JobDescriptions j ON a.JobDescriptionId = j.JobDescriptionId
                INNER JOIN Users u ON a.UserId = u.UserId
                ORDER BY a.CreatedAt DESC;";

            await using (var cmd = new SqlCommand(recentAnalysesSql, conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    summary.RecentAnalyses.Add(new ResumeAnalysis
                    {
                        AnalysisId = reader.GetInt32(reader.GetOrdinal("AnalysisId")),
                        ResumeId = reader.GetInt32(reader.GetOrdinal("ResumeId")),
                        JobDescriptionId = reader.GetInt32(reader.GetOrdinal("JobDescriptionId")),
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        OverallScore = reader.GetDecimal(reader.GetOrdinal("OverallScore")),
                        SkillsScore = reader.GetDecimal(reader.GetOrdinal("SkillsScore")),
                        KeywordsScore = reader.GetDecimal(reader.GetOrdinal("KeywordsScore")),
                        ExperienceScore = reader.GetDecimal(reader.GetOrdinal("ExperienceScore")),
                        EducationScore = reader.GetDecimal(reader.GetOrdinal("EducationScore")),
                        MatchedSkillsCount = reader.GetInt32(reader.GetOrdinal("MatchedSkillsCount")),
                        MissingSkillsCount = reader.GetInt32(reader.GetOrdinal("MissingSkillsCount")),
                        TotalSkillsIdentified = reader.GetInt32(reader.GetOrdinal("TotalSkillsIdentified")),
                        CalculationExplanation = reader.GetString(reader.GetOrdinal("CalculationExplanation")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        ResumeFileName = reader.IsDBNull(reader.GetOrdinal("ResumeFileName")) ? null : reader.GetString(reader.GetOrdinal("ResumeFileName")),
                        JobTitle = reader.IsDBNull(reader.GetOrdinal("JobTitle")) ? null : reader.GetString(reader.GetOrdinal("JobTitle")),
                        CompanyName = reader.IsDBNull(reader.GetOrdinal("CompanyName")) ? null : reader.GetString(reader.GetOrdinal("CompanyName")),
                        UserFullName = reader.IsDBNull(reader.GetOrdinal("UserFullName")) ? null : reader.GetString(reader.GetOrdinal("UserFullName"))
                    });
                }
            }

            return summary;
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
                list.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Role = reader.GetString(reader.GetOrdinal("Role")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }

            return list;
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole)
        {
            const string sql = @"
                UPDATE Users
                SET Role = @Role,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserId = @UserId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@Role", SqlDbType.NVarChar, 20) { Value = newRole });

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
