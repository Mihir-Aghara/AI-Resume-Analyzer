using AIResumeAnalyzer.Services;
using Xunit;

namespace AIResumeAnalyzer.Tests
{
    public class AnalysisEngineTests
    {
        private readonly ResumeAnalysisService _engine = new();

        [Fact]
        public void ExtractSkills_ShouldDetectMandatoryTaxonomySkills()
        {
            string sampleText = @"
                Experienced Software Engineer proficient in C#, ASP.NET Core, and Microsoft SQL Server.
                Strong background in ADO.NET, REST API development, OOP, Git, and GitHub.
                Academic coursework in Data Structures, Algorithms, and Machine Learning.
            ";

            var skills = _engine.ExtractSkills(sampleText);

            Assert.Contains("C#", skills);
            Assert.Contains("ASP.NET Core", skills);
            Assert.Contains("SQL Server", skills);
            Assert.Contains("ADO.NET", skills);
            Assert.Contains("REST API", skills);
            Assert.Contains("OOP", skills);
            Assert.Contains("Git", skills);
            Assert.Contains("GitHub", skills);
            Assert.Contains("Data Structures", skills);
            Assert.Contains("Algorithms", skills);
            Assert.Contains("Machine Learning", skills);
        }

        [Fact]
        public void ExtractKeywords_ShouldExcludeStopWordsAndPunctuation()
        {
            string text = "The engineer is developing a very high performance API with C# and SQL Server!";
            var keywords = _engine.ExtractKeywords(text);

            Assert.DoesNotContain("the", keywords);
            Assert.DoesNotContain("is", keywords);
            Assert.DoesNotContain("and", keywords);
            Assert.DoesNotContain("with", keywords);
            Assert.Contains("engineer", keywords);
            Assert.Contains("developing", keywords);
            Assert.Contains("performance", keywords);
        }

        [Fact]
        public async Task AnalyzeResumeAsync_WhenSkillsMatch_CalculatesAccurateScore()
        {
            string resume = @"
                B.Tech Graduate in Computer Engineering.
                Extensive experience in C#, ASP.NET Core, and SQL Server.
                Developed REST API services with ADO.NET and Git.
            ";

            string jobDescription = @"
                Looking for a Junior C# Engineer with ASP.NET Core and SQL Server skills.
                Experience in REST API and Git is required. B.Tech degree required.
            ";

            var result = await _engine.AnalyzeResumeAsync(resume, jobDescription, 1, 1, 1);

            Assert.NotNull(result);
            Assert.True(result.OverallScore >= 75m, $"Expected OverallScore >= 75, got {result.OverallScore}");
            Assert.True(result.SkillsScore >= 80m, $"Expected SkillsScore >= 80, got {result.SkillsScore}");
            Assert.Equal(0, result.MissingSkillsCount);
            Assert.Contains("Formula:", result.CalculationExplanation);
        }

        [Fact]
        public async Task AnalyzeResumeAsync_WhenMissingSkillsExist_GeneratesActionableRecommendations()
        {
            string resume = @"
                Graduate with Python and MySQL knowledge. Built simple data apps.
            ";

            string jobDescription = @"
                Require C#, ASP.NET Core, Docker, and Microsoft SQL Server.
            ";

            var result = await _engine.AnalyzeResumeAsync(resume, jobDescription, 1, 1, 1);

            Assert.NotNull(result);
            Assert.True(result.MissingSkillsCount > 0);
            Assert.Contains(result.Skills, s => !s.IsMatched && s.SkillName == "C#");
            Assert.Contains(result.Skills, s => !s.IsMatched && s.SkillName == "Docker");

            // Verify high priority recommendation was generated for missing skills
            Assert.Contains(result.Recommendations, r => r.Priority == "High" && r.Suggestion.Contains("C#"));
        }

        [Fact]
        public async Task AnalyzeResumeAsync_EmptyInputs_ShouldNotThrowException()
        {
            var result = await _engine.AnalyzeResumeAsync("", "", 1, 1, 1);

            Assert.NotNull(result);
            Assert.True(result.OverallScore >= 0m);
            Assert.True(result.OverallScore <= 100m);
        }
    }
}
