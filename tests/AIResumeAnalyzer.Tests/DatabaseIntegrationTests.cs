using AIResumeAnalyzer.Data;
using AIResumeAnalyzer.Repositories;
using Xunit;

namespace AIResumeAnalyzer.Tests
{
    public class DatabaseIntegrationTests
    {
        private const string TestConnectionString = 
            "Server=(localdb)\\MSSQLLocalDB;Database=AIResumeAnalyzerDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
        
        private readonly IDbConnectionFactory _dbFactory;

        public DatabaseIntegrationTests()
        {
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);
            _dbFactory = new DbConnectionFactory(TestConnectionString);
        }

        [Fact]
        public async Task DbConnectionFactory_CanOpenConnectionAsync()
        {
            await using var conn = await _dbFactory.CreateOpenConnectionAsync();
            Assert.NotNull(conn);
            Assert.Equal(System.Data.ConnectionState.Open, conn.State);
        }

        [Fact]
        public async Task UserRepository_CanGetSeededAdminAsync()
        {
            var userRepo = new UserRepository(_dbFactory);
            var admin = await userRepo.GetByEmailAsync("admin@airesume.local");

            Assert.NotNull(admin);
            Assert.Equal("Admin", admin.Role);
            Assert.Equal("System Administrator", admin.FullName);
        }

        [Fact]
        public async Task ResumeRepository_CanGetResumesUsingDataSetAsync()
        {
            var userRepo = new UserRepository(_dbFactory);
            var student = await userRepo.GetByEmailAsync("student@college.edu");
            Assert.NotNull(student);

            var resumeRepo = new ResumeRepository(_dbFactory);
            var resumes = await resumeRepo.GetByUserIdAsync(student.UserId);

            Assert.NotEmpty(resumes);
            Assert.Contains(resumes, r => r.OriginalFileName == "Alex_Johnson_Resume.pdf");
        }

        [Fact]
        public async Task AnalysisRepository_CanGetSeededAnalysisWithSkillsAsync()
        {
            var analysisRepo = new AnalysisRepository(_dbFactory);
            var analyses = await analysisRepo.GetAllAnalysesAsync(1);

            Assert.NotEmpty(analyses);
            var detailed = await analysisRepo.GetByIdAsync(analyses[0].AnalysisId);

            Assert.NotNull(detailed);
            Assert.NotEmpty(detailed.Skills);
            Assert.NotEmpty(detailed.Recommendations);
            Assert.True(detailed.OverallScore > 0);
        }

        [Fact]
        public async Task AdminRepository_CanGetSystemMetricsAsync()
        {
            var adminRepo = new AdminRepository(_dbFactory);
            var summary = await adminRepo.GetDashboardSummaryAsync();

            Assert.True(summary.TotalUsers >= 2);
            Assert.True(summary.TotalResumes >= 1);
            Assert.True(summary.TotalAnalyses >= 1);
            Assert.True(summary.SystemAverageScore > 0);
        }
    }
}
