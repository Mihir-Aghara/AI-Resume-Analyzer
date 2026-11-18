using System.Data;
using AIResumeAnalyzer.Data;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.Data.SqlClient;

namespace AIResumeAnalyzer.Repositories
{
    public class ResumeRepository : IResumeRepository
    {
        private readonly IDbConnectionFactory _db;

        public ResumeRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<int> CreateAsync(Resume resume)
        {
            const string sql = @"
                INSERT INTO Resumes (UserId, OriginalFileName, StoredFileName, FilePath, FileType, FileSizeBytes, ExtractedText, UploadedAt)
                OUTPUT INSERTED.ResumeId
                VALUES (@UserId, @OriginalFileName, @StoredFileName, @FilePath, @FileType, @FileSizeBytes, @ExtractedText, @UploadedAt);";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = resume.UserId });
            cmd.Parameters.Add(new SqlParameter("@OriginalFileName", SqlDbType.NVarChar, 260) { Value = resume.OriginalFileName });
            cmd.Parameters.Add(new SqlParameter("@StoredFileName", SqlDbType.NVarChar, 100) { Value = resume.StoredFileName });
            cmd.Parameters.Add(new SqlParameter("@FilePath", SqlDbType.NVarChar, 500) { Value = resume.FilePath });
            cmd.Parameters.Add(new SqlParameter("@FileType", SqlDbType.NVarChar, 10) { Value = resume.FileType });
            cmd.Parameters.Add(new SqlParameter("@FileSizeBytes", SqlDbType.BigInt) { Value = resume.FileSizeBytes });
            cmd.Parameters.Add(new SqlParameter("@ExtractedText", SqlDbType.NVarChar, -1) { Value = resume.ExtractedText });
            cmd.Parameters.Add(new SqlParameter("@UploadedAt", SqlDbType.DateTime2) { Value = resume.UploadedAt });

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<Resume?> GetByIdAsync(int resumeId)
        {
            const string sql = @"
                SELECT r.ResumeId, r.UserId, r.OriginalFileName, r.StoredFileName, r.FilePath, 
                       r.FileType, r.FileSizeBytes, r.ExtractedText, r.UploadedAt, u.FullName AS UserFullName
                FROM Resumes r
                INNER JOIN Users u ON r.UserId = u.UserId
                WHERE r.ResumeId = @ResumeId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@ResumeId", SqlDbType.Int) { Value = resumeId });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapResume(reader);
            }

            return null;
        }

        public async Task<Resume?> GetByIdAndUserAsync(int resumeId, int userId)
        {
            const string sql = @"
                SELECT r.ResumeId, r.UserId, r.OriginalFileName, r.StoredFileName, r.FilePath, 
                       r.FileType, r.FileSizeBytes, r.ExtractedText, r.UploadedAt, u.FullName AS UserFullName
                FROM Resumes r
                INNER JOIN Users u ON r.UserId = u.UserId
                WHERE r.ResumeId = @ResumeId AND r.UserId = @UserId;";

            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@ResumeId", SqlDbType.Int) { Value = resumeId });
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapResume(reader);
            }

            return null;
        }

        /// <summary>
        /// Demonstrates ADO.NET Disconnected Architecture using SqlDataAdapter, DataSet, and DataTable.
        /// </summary>
        public async Task<List<Resume>> GetByUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT r.ResumeId, r.UserId, r.OriginalFileName, r.StoredFileName, r.FilePath, 
                       r.FileType, r.FileSizeBytes, r.ExtractedText, r.UploadedAt, u.FullName AS UserFullName
                FROM Resumes r
                INNER JOIN Users u ON r.UserId = u.UserId
                WHERE r.UserId = @UserId
                ORDER BY r.UploadedAt DESC;";

            var resumes = new List<Resume>();

            // Disconnected ADO.NET Architecture demonstration (SqlDataAdapter + DataSet)
            await using var conn = _db.CreateConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            using var adapter = new SqlDataAdapter(cmd);
            var dataSet = new DataSet("ResumeDataSet");

            await Task.Run(() => adapter.Fill(dataSet, "ResumesTable"));

            if (dataSet.Tables.Contains("ResumesTable"))
            {
                DataTable table = dataSet.Tables["ResumesTable"]!;
                foreach (DataRow row in table.Rows)
                {
                    resumes.Add(new Resume
                    {
                        ResumeId = Convert.ToInt32(row["ResumeId"]),
                        UserId = Convert.ToInt32(row["UserId"]),
                        OriginalFileName = Convert.ToString(row["OriginalFileName"]) ?? string.Empty,
                        StoredFileName = Convert.ToString(row["StoredFileName"]) ?? string.Empty,
                        FilePath = Convert.ToString(row["FilePath"]) ?? string.Empty,
                        FileType = Convert.ToString(row["FileType"]) ?? string.Empty,
                        FileSizeBytes = Convert.ToInt64(row["FileSizeBytes"]),
                        ExtractedText = Convert.ToString(row["ExtractedText"]) ?? string.Empty,
                        UploadedAt = Convert.ToDateTime(row["UploadedAt"]),
                        UserFullName = Convert.ToString(row["UserFullName"])
                    });
                }
            }

            return resumes;
        }

        public async Task<List<Resume>> GetAllResumesAsync()
        {
            const string sql = @"
                SELECT r.ResumeId, r.UserId, r.OriginalFileName, r.StoredFileName, r.FilePath, 
                       r.FileType, r.FileSizeBytes, r.ExtractedText, r.UploadedAt, u.FullName AS UserFullName
                FROM Resumes r
                INNER JOIN Users u ON r.UserId = u.UserId
                ORDER BY r.UploadedAt DESC;";

            var resumes = new List<Resume>();
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                resumes.Add(MapResume(reader));
            }

            return resumes;
        }

        public async Task<int> GetCountByUserIdAsync(int userId)
        {
            const string sql = "SELECT COUNT(*) FROM Resumes WHERE UserId = @UserId;";
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<bool> DeleteAsync(int resumeId, int userId)
        {
            await using var conn = await _db.CreateOpenConnectionAsync();
            await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                // First delete any dependent analysis records (AnalysisSkills & Recommendations cascade automatically)
                const string deleteAnalysesSql = @"
                    DELETE FROM ResumeAnalyses
                    WHERE ResumeId = @ResumeId AND UserId = @UserId;";

                await using (var analysesCmd = new SqlCommand(deleteAnalysesSql, conn, transaction))
                {
                    analysesCmd.Parameters.Add(new SqlParameter("@ResumeId", SqlDbType.Int) { Value = resumeId });
                    analysesCmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                    await analysesCmd.ExecuteNonQueryAsync();
                }

                // Now safely delete the resume record itself
                const string deleteResumeSql = @"
                    DELETE FROM Resumes
                    WHERE ResumeId = @ResumeId AND UserId = @UserId;";

                int rows;
                await using (var resumeCmd = new SqlCommand(deleteResumeSql, conn, transaction))
                {
                    resumeCmd.Parameters.Add(new SqlParameter("@ResumeId", SqlDbType.Int) { Value = resumeId });
                    resumeCmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                    rows = await resumeCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return rows > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static Resume MapResume(SqlDataReader reader)
        {
            return new Resume
            {
                ResumeId = reader.GetInt32(reader.GetOrdinal("ResumeId")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                OriginalFileName = reader.GetString(reader.GetOrdinal("OriginalFileName")),
                StoredFileName = reader.GetString(reader.GetOrdinal("StoredFileName")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FileType = reader.GetString(reader.GetOrdinal("FileType")),
                FileSizeBytes = reader.GetInt64(reader.GetOrdinal("FileSizeBytes")),
                ExtractedText = reader.GetString(reader.GetOrdinal("ExtractedText")),
                UploadedAt = reader.GetDateTime(reader.GetOrdinal("UploadedAt")),
                UserFullName = reader.IsDBNull(reader.GetOrdinal("UserFullName")) ? null : reader.GetString(reader.GetOrdinal("UserFullName"))
            };
        }
    }
}
