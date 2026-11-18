using System.Data;
using AIResumeAnalyzer.Data;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.Data.SqlClient;

namespace AIResumeAnalyzer.Repositories
{
    public class AnalysisRepository : IAnalysisRepository
    {
        private readonly IDbConnectionFactory _db;

        public AnalysisRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<int> CreateJobDescriptionAsync(JobDescription jobDescription)
        {
            const string sql = @"
                INSERT INTO JobDescriptions (UserId, JobTitle, CompanyName, RawText, CreatedAt)
                OUTPUT INSERTED.JobDescriptionId
                VALUES (@UserId, @JobTitle, @CompanyName, @RawText, @CreatedAt);";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = jobDescription.UserId });
            cmd.Parameters.Add(new SqlParameter("@JobTitle", SqlDbType.NVarChar, 150) { Value = jobDescription.JobTitle });
            cmd.Parameters.Add(new SqlParameter("@CompanyName", SqlDbType.NVarChar, 150) { Value = (object?)jobDescription.CompanyName ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@RawText", SqlDbType.NVarChar, -1) { Value = jobDescription.RawText });
            cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = jobDescription.CreatedAt });

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Saves analysis, extracted skills, and generated recommendations atomically using SqlTransaction.
        /// Demonstrates ADO.NET Transaction Management.
        /// </summary>
        public async Task<int> SaveAnalysisAsync(ResumeAnalysis analysis)
        {
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                const string analysisSql = @"
                    INSERT INTO ResumeAnalyses 
                    (ResumeId, JobDescriptionId, UserId, OverallScore, SkillsScore, KeywordsScore, ExperienceScore, EducationScore, 
                     MatchedSkillsCount, MissingSkillsCount, TotalSkillsIdentified, CalculationExplanation, CreatedAt)
                    OUTPUT INSERTED.AnalysisId
                    VALUES 
                    (@ResumeId, @JobDescriptionId, @UserId, @OverallScore, @SkillsScore, @KeywordsScore, @ExperienceScore, @EducationScore, 
                     @MatchedSkillsCount, @MissingSkillsCount, @TotalSkillsIdentified, @CalculationExplanation, @CreatedAt);";

                await using var analysisCmd = new SqlCommand(analysisSql, conn, transaction);
                analysisCmd.Parameters.Add(new SqlParameter("@ResumeId", SqlDbType.Int) { Value = analysis.ResumeId });
                analysisCmd.Parameters.Add(new SqlParameter("@JobDescriptionId", SqlDbType.Int) { Value = analysis.JobDescriptionId });
                analysisCmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = analysis.UserId });
                analysisCmd.Parameters.Add(new SqlParameter("@OverallScore", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = analysis.OverallScore });
                analysisCmd.Parameters.Add(new SqlParameter("@SkillsScore", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = analysis.SkillsScore });
                analysisCmd.Parameters.Add(new SqlParameter("@KeywordsScore", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = analysis.KeywordsScore });
                analysisCmd.Parameters.Add(new SqlParameter("@ExperienceScore", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = analysis.ExperienceScore });
                analysisCmd.Parameters.Add(new SqlParameter("@EducationScore", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = analysis.EducationScore });
                analysisCmd.Parameters.Add(new SqlParameter("@MatchedSkillsCount", SqlDbType.Int) { Value = analysis.MatchedSkillsCount });
                analysisCmd.Parameters.Add(new SqlParameter("@MissingSkillsCount", SqlDbType.Int) { Value = analysis.MissingSkillsCount });
                analysisCmd.Parameters.Add(new SqlParameter("@TotalSkillsIdentified", SqlDbType.Int) { Value = analysis.TotalSkillsIdentified });
                analysisCmd.Parameters.Add(new SqlParameter("@CalculationExplanation", SqlDbType.NVarChar, -1) { Value = analysis.CalculationExplanation });
                analysisCmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = analysis.CreatedAt });

                var analysisIdObj = await analysisCmd.ExecuteScalarAsync();
                int analysisId = Convert.ToInt32(analysisIdObj);
                analysis.AnalysisId = analysisId;

                // Insert Skills
                if (analysis.Skills.Count > 0)
                {
                    const string skillSql = @"
                        INSERT INTO AnalysisSkills (AnalysisId, SkillName, Category, IsMatched, ProficiencyLevel)
                        VALUES (@AnalysisId, @SkillName, @Category, @IsMatched, @ProficiencyLevel);";

                    foreach (var skill in analysis.Skills)
                    {
                        await using var skillCmd = new SqlCommand(skillSql, conn, transaction);
                        skillCmd.Parameters.Add(new SqlParameter("@AnalysisId", SqlDbType.Int) { Value = analysisId });
                        skillCmd.Parameters.Add(new SqlParameter("@SkillName", SqlDbType.NVarChar, 100) { Value = skill.SkillName });
                        skillCmd.Parameters.Add(new SqlParameter("@Category", SqlDbType.NVarChar, 50) { Value = skill.Category });
                        skillCmd.Parameters.Add(new SqlParameter("@IsMatched", SqlDbType.Bit) { Value = skill.IsMatched });
                        skillCmd.Parameters.Add(new SqlParameter("@ProficiencyLevel", SqlDbType.NVarChar, 50) { Value = (object?)skill.ProficiencyLevel ?? DBNull.Value });

                        await skillCmd.ExecuteNonQueryAsync();
                    }
                }

                // Insert Recommendations
                if (analysis.Recommendations.Count > 0)
                {
                    const string recSql = @"
                        INSERT INTO Recommendations (AnalysisId, Category, Suggestion, Priority)
                        VALUES (@AnalysisId, @Category, @Suggestion, @Priority);";

                    foreach (var rec in analysis.Recommendations)
                    {
                        await using var recCmd = new SqlCommand(recSql, conn, transaction);
                        recCmd.Parameters.Add(new SqlParameter("@AnalysisId", SqlDbType.Int) { Value = analysisId });
                        recCmd.Parameters.Add(new SqlParameter("@Category", SqlDbType.NVarChar, 50) { Value = rec.Category });
                        recCmd.Parameters.Add(new SqlParameter("@Suggestion", SqlDbType.NVarChar, -1) { Value = rec.Suggestion });
                        recCmd.Parameters.Add(new SqlParameter("@Priority", SqlDbType.NVarChar, 20) { Value = rec.Priority });

                        await recCmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
                return analysisId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ResumeAnalysis?> GetByIdAsync(int analysisId)
        {
            const string sql = @"
                SELECT a.AnalysisId, a.ResumeId, a.JobDescriptionId, a.UserId,
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
                WHERE a.AnalysisId = @AnalysisId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@AnalysisId", SqlDbType.Int) { Value = analysisId });

            ResumeAnalysis? analysis = null;
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    analysis = MapAnalysis(reader);
                }
            }

            if (analysis != null)
            {
                await PopulateSkillsAndRecommendationsAsync(conn, analysis);
            }

            return analysis;
        }

        public async Task<ResumeAnalysis?> GetByIdAndUserAsync(int analysisId, int userId)
        {
            const string sql = @"
                SELECT a.AnalysisId, a.ResumeId, a.JobDescriptionId, a.UserId,
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
                WHERE a.AnalysisId = @AnalysisId AND a.UserId = @UserId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@AnalysisId", SqlDbType.Int) { Value = analysisId });
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            ResumeAnalysis? analysis = null;
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    analysis = MapAnalysis(reader);
                }
            }

            if (analysis != null)
            {
                await PopulateSkillsAndRecommendationsAsync(conn, analysis);
            }

            return analysis;
        }

        public async Task<List<ResumeAnalysis>> GetByUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT a.AnalysisId, a.ResumeId, a.JobDescriptionId, a.UserId,
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
                WHERE a.UserId = @UserId
                ORDER BY a.CreatedAt DESC;";

            var list = new List<ResumeAnalysis>();
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapAnalysis(reader));
            }

            return list;
        }

        public async Task<List<ResumeAnalysis>> GetRecentByUserIdAsync(int userId, int limit = 5)
        {
            string sql = $@"
                SELECT TOP ({limit}) a.AnalysisId, a.ResumeId, a.JobDescriptionId, a.UserId,
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
                WHERE a.UserId = @UserId
                ORDER BY a.CreatedAt DESC;";

            var list = new List<ResumeAnalysis>();
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapAnalysis(reader));
            }

            return list;
        }

        public async Task<List<ResumeAnalysis>> GetAllAnalysesAsync(int limit = 50)
        {
            string sql = $@"
                SELECT TOP ({limit}) a.AnalysisId, a.ResumeId, a.JobDescriptionId, a.UserId,
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

            var list = new List<ResumeAnalysis>();
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapAnalysis(reader));
            }

            return list;
        }

        public async Task<int> GetCountByUserIdAsync(int userId)
        {
            const string sql = "SELECT COUNT(*) FROM ResumeAnalyses WHERE UserId = @UserId;";
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<decimal> GetAverageScoreByUserIdAsync(int userId)
        {
            const string sql = "SELECT ISNULL(AVG(OverallScore), 0.0) FROM ResumeAnalyses WHERE UserId = @UserId;";
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToDecimal(result) : 0m;
        }

        public async Task<decimal> GetHighestScoreByUserIdAsync(int userId)
        {
            const string sql = "SELECT ISNULL(MAX(OverallScore), 0.0) FROM ResumeAnalyses WHERE UserId = @UserId;";
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToDecimal(result) : 0m;
        }

        private static async Task PopulateSkillsAndRecommendationsAsync(SqlConnection conn, ResumeAnalysis analysis)
        {
            // Fetch skills
            const string skillsSql = @"
                SELECT SkillId, AnalysisId, SkillName, Category, IsMatched, ProficiencyLevel
                FROM AnalysisSkills
                WHERE AnalysisId = @AnalysisId
                ORDER BY IsMatched DESC, SkillName ASC;";

            await using (var cmd = new SqlCommand(skillsSql, conn))
            {
                cmd.Parameters.Add(new SqlParameter("@AnalysisId", SqlDbType.Int) { Value = analysis.AnalysisId });
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    analysis.Skills.Add(new AnalysisSkill
                    {
                        SkillId = reader.GetInt32(reader.GetOrdinal("SkillId")),
                        AnalysisId = reader.GetInt32(reader.GetOrdinal("AnalysisId")),
                        SkillName = reader.GetString(reader.GetOrdinal("SkillName")),
                        Category = reader.GetString(reader.GetOrdinal("Category")),
                        IsMatched = reader.GetBoolean(reader.GetOrdinal("IsMatched")),
                        ProficiencyLevel = reader.IsDBNull(reader.GetOrdinal("ProficiencyLevel")) ? null : reader.GetString(reader.GetOrdinal("ProficiencyLevel"))
                    });
                }
            }

            // Fetch recommendations
            const string recSql = @"
                SELECT RecommendationId, AnalysisId, Category, Suggestion, Priority
                FROM Recommendations
                WHERE AnalysisId = @AnalysisId
                ORDER BY CASE Priority WHEN 'Strength' THEN 1 WHEN 'High' THEN 2 WHEN 'Medium' THEN 3 WHEN 'Low' THEN 4 ELSE 5 END, RecommendationId ASC;";

            await using (var cmd = new SqlCommand(recSql, conn))
            {
                cmd.Parameters.Add(new SqlParameter("@AnalysisId", SqlDbType.Int) { Value = analysis.AnalysisId });
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    analysis.Recommendations.Add(new Recommendation
                    {
                        RecommendationId = reader.GetInt32(reader.GetOrdinal("RecommendationId")),
                        AnalysisId = reader.GetInt32(reader.GetOrdinal("AnalysisId")),
                        Category = reader.GetString(reader.GetOrdinal("Category")),
                        Suggestion = reader.GetString(reader.GetOrdinal("Suggestion")),
                        Priority = reader.GetString(reader.GetOrdinal("Priority"))
                    });
                }
            }
        }

        private static ResumeAnalysis MapAnalysis(SqlDataReader reader)
        {
            return new ResumeAnalysis
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
            };
        }
    }
}
