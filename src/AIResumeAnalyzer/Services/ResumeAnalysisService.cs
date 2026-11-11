using System.Text;
using System.Text.RegularExpressions;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using Microsoft.Extensions.Configuration;

namespace AIResumeAnalyzer.Services
{
    public class ResumeAnalysisService : IResumeAnalysisService
    {
        private readonly decimal _skillsWeight;
        private readonly decimal _keywordsWeight;
        private readonly decimal _experienceWeight;
        private readonly decimal _educationWeight;

        // Structured Technical Taxonomy: Skill Name -> (Category, Regex Pattern)
        private static readonly List<SkillDefinition> SkillTaxonomy = new()
        {
            // Programming Languages
            new("C#", "Programming Language", @"(?:\bc#(?=[^a-zA-Z0-9]|$)|csharp\b|\b\.net c#)"),
            new("Java", "Programming Language", @"\b(java)\b(?!script)"),
            new("Python", "Programming Language", @"\b(python|py)\b"),
            new("JavaScript", "Programming Language", @"\b(javascript|js|ecmascript)\b"),
            new("TypeScript", "Programming Language", @"\b(typescript|ts)\b"),
            new("C++", "Programming Language", @"(?:\bc\+\+(?=[^a-zA-Z0-9]|$)|cpp\b)"),
            new("SQL", "Programming Language", @"\b(sql|t-sql|pl/sql)\b"),

            // Web & Frameworks
            new("ASP.NET Core", "Web & Framework", @"\b(asp\.net core|aspnet core|\.net core mvc|\.net core web api|\.net 8|\.net 9|\.net 10)\b"),
            new("ASP.NET MVC", "Web & Framework", @"\b(asp\.net mvc|aspnet mvc)\b"),
            new("HTML", "Web & Framework", @"\b(html|html5)\b"),
            new("CSS", "Web & Framework", @"\b(css|css3)\b"),
            new("Bootstrap", "Web & Framework", @"\b(bootstrap|bootstrap 5)\b"),
            new("REST API", "Web & Framework", @"\b(rest api|restful api|restful services|web api)\b"),

            // Databases & ORM
            new("SQL Server", "Database", @"\b(sql server|mssql|microsoft sql server)\b"),
            new("ADO.NET", "Database", @"\b(ado\.net|adonet|sqlclient|sqlconnection|sqldataadapter)\b"),
            new("MySQL", "Database", @"\b(mysql)\b"),
            new("PostgreSQL", "Database", @"\b(postgresql|postgres)\b"),

            // Architecture & Practices
            new("OOP", "Architecture & Practices", @"\b(oop|object oriented programming|object-oriented programming)\b"),
            new("MVC", "Architecture & Practices", @"\b(mvc|model view controller)\b"),
            new("Dependency Injection", "Architecture & Practices", @"\b(dependency injection|\bdi\b|ioc container|inversion of control)\b"),
            new("Clean Architecture", "Architecture & Practices", @"\b(clean architecture|onion architecture|n-tier architecture|3-tier)\b"),

            // Core Computer Science
            new("Data Structures", "Core CS", @"\b(data structures|arrays|linked lists|trees|graphs|hash tables)\b"),
            new("Algorithms", "Core CS", @"\b(algorithms|sorting|searching|dynamic programming|time complexity)\b"),

            // Tools, DevOps & Emerging Tech
            new("Git", "Tools & DevOps", @"\b(git)\b(?!hub)"),
            new("GitHub", "Tools & DevOps", @"\b(github|gitlab|bitbucket)\b"),
            new("Docker", "Tools & DevOps", @"\b(docker|containerization|containers)\b"),
            new("Machine Learning", "Emerging Tech", @"\b(machine learning|\bml\b|scikit-learn|ml\.net)\b"),
            new("Artificial Intelligence", "Emerging Tech", @"\b(artificial intelligence|\bai\b|deep learning|nlp)\b")
        };

        // English Stop Words Filter
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "about", "above", "after", "again", "against", "all", "am", "an", "and", "any", "are", "aren't",
            "as", "at", "be", "because", "been", "before", "being", "below", "between", "both", "but", "by",
            "can", "can't", "cannot", "could", "couldn't", "did", "didn't", "do", "does", "doesn't", "doing",
            "don't", "down", "during", "each", "few", "for", "from", "further", "had", "hadn't", "has", "hasn't",
            "have", "haven't", "having", "he", "he'd", "he'll", "he's", "her", "here", "here's", "hers", "herself",
            "him", "himself", "his", "how", "how's", "i", "i'd", "i'll", "i'm", "i've", "if", "in", "into", "is",
            "isn't", "it", "it's", "its", "itself", "let's", "me", "more", "most", "mustn't", "my", "myself", "no",
            "nor", "not", "of", "off", "on", "once", "only", "or", "other", "ought", "our", "ours", "ourselves",
            "out", "over", "own", "same", "shan't", "she", "she'd", "she'll", "she's", "should", "shouldn't", "so",
            "some", "such", "than", "that", "that's", "the", "their", "theirs", "them", "themselves", "then",
            "there", "there's", "these", "they", "they'd", "they'll", "they're", "they've", "this", "those",
            "through", "to", "too", "under", "until", "up", "very", "was", "wasn't", "we", "we'd", "we'll",
            "we're", "we've", "were", "weren't", "what", "what's", "when", "when's", "where", "where's", "which",
            "while", "who", "who's", "whom", "why", "why's", "with", "won't", "would", "wouldn't", "you",
            "you'd", "you'll", "you're", "you've", "your", "yours", "yourself", "yourselves", "will", "shall"
        };

        public ResumeAnalysisService(IConfiguration? configuration = null)
        {
            // Configurable scoring weights with reliable academic defaults
            _skillsWeight = decimal.TryParse(configuration?["ScoringWeights:SkillsMatchWeight"], out var sw) ? sw : 0.40m;
            _keywordsWeight = decimal.TryParse(configuration?["ScoringWeights:KeywordMatchWeight"], out var kw) ? kw : 0.25m;
            _experienceWeight = decimal.TryParse(configuration?["ScoringWeights:ExperienceWeight"], out var ew) ? ew : 0.20m;
            _educationWeight = decimal.TryParse(configuration?["ScoringWeights:EducationWeight"], out var edw) ? edw : 0.15m;
        }

        public Task<ResumeAnalysis> AnalyzeResumeAsync(
            string resumeText, 
            string jobDescriptionText, 
            int resumeId, 
            int jobDescriptionId, 
            int userId, 
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var analysis = new ResumeAnalysis
                {
                    ResumeId = resumeId,
                    JobDescriptionId = jobDescriptionId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                // 1. SKILLS EXTRACTION & MATCHING (Weight: 40%)
                var resumeSkills = ExtractSkillsFromText(resumeText);
                var jdSkills = ExtractSkillsFromText(jobDescriptionText);

                // If JD didn't cite any known taxonomy skills, treat all resume skills as valid
                var targetSkills = jdSkills.Count > 0 ? jdSkills : SkillTaxonomy.Select(s => s.SkillName).ToHashSet();

                var matchedSkills = new List<AnalysisSkill>();
                var missingSkills = new List<AnalysisSkill>();

                foreach (var target in targetSkills)
                {
                    var def = SkillTaxonomy.FirstOrDefault(s => string.Equals(s.SkillName, target, StringComparison.OrdinalIgnoreCase))
                              ?? new SkillDefinition(target, "General Technical", "");

                    if (resumeSkills.Contains(target))
                    {
                        matchedSkills.Add(new AnalysisSkill
                        {
                            SkillName = target,
                            Category = def.Category,
                            IsMatched = true,
                            ProficiencyLevel = "Proficient"
                        });
                    }
                    else
                    {
                        missingSkills.Add(new AnalysisSkill
                        {
                            SkillName = target,
                            Category = def.Category,
                            IsMatched = false,
                            ProficiencyLevel = null
                        });
                    }
                }

                int totalTargetSkills = targetSkills.Count;
                int matchedCount = matchedSkills.Count;
                decimal skillsScore = totalTargetSkills > 0 
                    ? Math.Round(((decimal)matchedCount / totalTargetSkills) * 100m, 2) 
                    : 100m;

                analysis.SkillsScore = Math.Min(100m, Math.Max(0m, skillsScore));
                analysis.MatchedSkillsCount = matchedCount;
                analysis.MissingSkillsCount = missingSkills.Count;
                analysis.TotalSkillsIdentified = totalTargetSkills;

                // Add skills to analysis entity
                analysis.Skills.AddRange(matchedSkills);
                analysis.Skills.AddRange(missingSkills);

                // 2. KEYWORD OVERLAP (Weight: 25%)
                var resumeKeywords = ExtractKeywords(resumeText);
                var jdKeywords = ExtractKeywords(jobDescriptionText);

                int matchedKeywords = 0;
                foreach (var kw in jdKeywords)
                {
                    if (resumeKeywords.Contains(kw))
                    {
                        matchedKeywords++;
                    }
                }

                decimal keywordsScore = jdKeywords.Count > 0 
                    ? Math.Round(((decimal)matchedKeywords / jdKeywords.Count) * 100m, 2) 
                    : 100m;
                analysis.KeywordsScore = Math.Min(100m, Math.Max(0m, keywordsScore));

                // 3. RELEVANT EXPERIENCE ALIGNMENT (Weight: 20%)
                decimal experienceScore = EvaluateExperience(resumeText, jobDescriptionText);
                analysis.ExperienceScore = Math.Min(100m, Math.Max(0m, experienceScore));

                // 4. EDUCATION & QUALIFICATIONS (Weight: 15%)
                decimal educationScore = EvaluateEducation(resumeText, jobDescriptionText);
                analysis.EducationScore = Math.Min(100m, Math.Max(0m, educationScore));

                // 5. OVERALL COMPATIBILITY SCORE (Weighted Sum)
                decimal overallScore = (_skillsWeight * analysis.SkillsScore) +
                                       (_keywordsWeight * analysis.KeywordsScore) +
                                       (_experienceWeight * analysis.ExperienceScore) +
                                       (_educationWeight * analysis.EducationScore);

                analysis.OverallScore = Math.Round(Math.Min(100m, Math.Max(0m, overallScore)), 2);

                // 6. GENERATE ACTIONABLE RECOMMENDATIONS
                analysis.Recommendations = GenerateRecommendations(missingSkills, analysis.KeywordsScore, analysis.ExperienceScore, analysis.EducationScore);

                // 7. EXPLAINABLE CALCULATION BREAKDOWN
                analysis.CalculationExplanation = GenerateCalculationExplanation(analysis, _skillsWeight, _keywordsWeight, _experienceWeight, _educationWeight);

                return analysis;
            }, cancellationToken);
        }

        public List<string> ExtractSkills(string text)
        {
            return ExtractSkillsFromText(text).OrderBy(s => s).ToList();
        }

        public HashSet<string> ExtractKeywords(string text)
        {
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text))
            {
                return keywords;
            }

            // Clean punctuation and tokenize words
            var tokens = Regex.Matches(text.ToLowerInvariant(), @"\b[a-z0-9#\+\.]{3,25}\b");
            foreach (Match match in tokens)
            {
                string word = match.Value.Trim('.', '+');
                if (word.Length >= 3 && !StopWords.Contains(word) && !int.TryParse(word, out _))
                {
                    keywords.Add(word);
                }
            }

            return keywords;
        }

        private static HashSet<string> ExtractSkillsFromText(string text)
        {
            var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text))
            {
                return detected;
            }

            foreach (var skill in SkillTaxonomy)
            {
                if (Regex.IsMatch(text, skill.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    detected.Add(skill.SkillName);
                }
            }

            return detected;
        }

        private static decimal EvaluateExperience(string resumeText, string jdText)
        {
            if (string.IsNullOrWhiteSpace(resumeText)) return 0m;

            decimal score = 50m; // Baseline starting score for readable candidate text

            // Experience indicators in resume
            string[] actionVerbs = { "developed", "architected", "engineered", "designed", "built", "implemented", "maintained", "lead", "optimized", "spearheaded", "deployed", "integrated" };
            int actionCount = actionVerbs.Count(v => Regex.IsMatch(resumeText, $@"\b{v}\b", RegexOptions.IgnoreCase));
            score += Math.Min(25m, actionCount * 5m);

            // Capstone / Work context markers
            string[] contextMarkers = { "project", "projects", "internship", "experience", "responsibilities", "production", "capstone" };
            int contextCount = contextMarkers.Count(c => Regex.IsMatch(resumeText, $@"\b{c}\b", RegexOptions.IgnoreCase));
            score += Math.Min(25m, contextCount * 5m);

            return Math.Min(100m, score);
        }

        private static decimal EvaluateEducation(string resumeText, string jdText)
        {
            if (string.IsNullOrWhiteSpace(resumeText)) return 0m;

            decimal score = 40m;

            // Relevant Degree terms
            string[] degreeMarkers = { "b.tech", "bachelor", "b.e.", "b.s.", "b.sc", "computer science", "computer engineering", "information technology", "m.tech", "master", "mca" };
            int foundDegrees = degreeMarkers.Count(d => Regex.IsMatch(resumeText, $@"\b{Regex.Escape(d)}\b", RegexOptions.IgnoreCase));

            if (foundDegrees > 0)
            {
                score += 40m;
            }

            // Academic metrics / coursework markers
            string[] academicMarkers = { "gpa", "cgpa", "coursework", "university", "institute", "college", "graduated", "semester" };
            int academicCount = academicMarkers.Count(a => Regex.IsMatch(resumeText, $@"\b{a}\b", RegexOptions.IgnoreCase));
            score += Math.Min(20m, academicCount * 5m);

            return Math.Min(100m, score);
        }

        private static List<Recommendation> GenerateRecommendations(
            List<AnalysisSkill> missingSkills, 
            decimal keywordsScore, 
            decimal experienceScore, 
            decimal educationScore)
        {
            var list = new List<Recommendation>();

            // 1. Missing Skill Recommendations (High Priority)
            foreach (var missing in missingSkills.Take(5))
            {
                list.Add(new Recommendation
                {
                    Category = "Missing Technical Skill",
                    Suggestion = $"The target job requires **{missing.SkillName}** ({missing.Category}). Add relevant coursework, personal lab projects, or code repositories demonstrating hands-on experience with {missing.SkillName}.",
                    Priority = "High"
                });
            }

            // 2. Keyword Density Recommendations (Medium Priority)
            if (keywordsScore < 70m)
            {
                list.Add(new Recommendation
                {
                    Category = "Keyword Optimization",
                    Suggestion = "Your resume has low lexical overlap with the specific technical terminology in the job description. Mirror the exact phrasing used in the job requirements (e.g. 'RESTful APIs', 'Unit Testing', 'Relational Databases') in your bullet points.",
                    Priority = "Medium"
                });
            }

            // 3. Experience Impact Recommendations (Medium Priority)
            if (experienceScore < 75m)
            {
                list.Add(new Recommendation
                {
                    Category = "Experience Impact",
                    Suggestion = "Enhance project descriptions with quantifiable results and action verbs (e.g., 'Implemented parameterized queries reducing latency by 20%' rather than 'Worked on backend').",
                    Priority = "Medium"
                });
            }

            // 4. Education & Certifications (Low Priority)
            if (educationScore < 80m)
            {
                list.Add(new Recommendation
                {
                    Category = "Education & Certifications",
                    Suggestion = "Ensure your academic degree (e.g. B.Tech Computer Engineering), graduation year, and key CS coursework (Data Structures, DBMS, Operating Systems) are clearly visible at the top or bottom.",
                    Priority = "Low"
                });
            }

            return list;
        }

        private static string GenerateCalculationExplanation(
            ResumeAnalysis a, 
            decimal wSkills, 
            decimal wKeywords, 
            decimal wExp, 
            decimal wEdu)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Compatibility benchmark evaluated across 4 transparent pillars:");
            sb.AppendLine($"• Technical Skills Match ({wSkills:P0} Weight): {a.SkillsScore:F1}% ({a.MatchedSkillsCount}/{a.TotalSkillsIdentified} target competencies matched)");
            sb.AppendLine($"• Lexical Keyword Overlap ({wKeywords:P0} Weight): {a.KeywordsScore:F1}% (alignment across technical and domain n-grams)");
            sb.AppendLine($"• Relevant Experience Alignment ({wExp:P0} Weight): {a.ExperienceScore:F1}% (project action verbs, technical scope, and role markers)");
            sb.AppendLine($"• Education & Qualifications ({wEdu:P0} Weight): {a.EducationScore:F1}% (accredited degree and academic alignment)");
            sb.AppendLine();
            sb.AppendLine($"Formula: ({wSkills:F2} × {a.SkillsScore:F1}) + ({wKeywords:F2} × {a.KeywordsScore:F1}) + ({wExp:F2} × {a.ExperienceScore:F1}) + ({wEdu:F2} × {a.EducationScore:F1}) = {a.OverallScore:F1}%");
            sb.AppendLine();
            sb.AppendLine("Notice: This benchmark is an explainable academic compatibility estimate designed to help candidates identify curriculum gaps. It does not claim to represent an official proprietary ATS vendor result.");

            return sb.ToString();
        }

        private record SkillDefinition(string SkillName, string Category, string Pattern);
    }
}
