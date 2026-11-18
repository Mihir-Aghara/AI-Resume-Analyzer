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

        // Comprehensive Technical & Professional Skill Taxonomy: Skill Name -> (Category, Regex Pattern)
        private static readonly List<SkillDefinition> SkillTaxonomy = new()
        {
            // Programming Languages
            new("C#", "Programming Language", @"(?:\bc#(?=[^a-zA-Z0-9]|$)|csharp\b|\b\.net c#)"),
            new("Java", "Programming Language", @"\b(java|core java|jdk)\b(?!script)"),
            new("Python", "Programming Language", @"\b(python|python3)\b"),
            new("JavaScript", "Programming Language", @"\b(javascript|js|ecmascript|es6)\b"),
            new("TypeScript", "Programming Language", @"\b(typescript|ts)\b"),
            new("C++", "Programming Language", @"(?:\bc\+\+(?=[^a-zA-Z0-9]|$)|cpp\b)"),
            new("C", "Programming Language", @"(?:\bc programming\b|\bc language\b|\bc/c\+\+)"),
            new("SQL", "Programming Language", @"\b(sql|t-sql|pl/sql|structured query language)\b"),
            new("Go", "Programming Language", @"\b(golang|go language)\b"),
            new("Kotlin", "Programming Language", @"\b(kotlin)\b"),
            new("Swift", "Programming Language", @"\b(swift|swiftui)\b"),
            new("PHP", "Programming Language", @"\b(php|laravel)\b"),

            // Web, Mobile & Frameworks
            new("ASP.NET Core", "Web & Framework", @"\b(asp\.net core|aspnet core|\.net core|\.net 6|\.net 7|\.net 8|\.net 9|\.net 10|dotnet core)\b"),
            new("ASP.NET MVC", "Web & Framework", @"\b(asp\.net mvc|aspnet mvc|razor pages|razor views)\b"),
            new("HTML", "Web & Framework", @"\b(html|html5)\b"),
            new("CSS", "Web & Framework", @"\b(css|css3|scss|sass)\b"),
            new("Bootstrap", "Web & Framework", @"\b(bootstrap|bootstrap 5)\b"),
            new("Tailwind CSS", "Web & Framework", @"\b(tailwind|tailwindcss|tailwind css)\b"),
            new("REST API", "Web & Framework", @"\b(rest api|rest apis|restful api|restful apis|restful services|web api|web apis|api development)\b"),
            new("React", "Web & Framework", @"\b(react|reactjs|react\.js|next\.js|nextjs)\b"),
            new("Angular", "Web & Framework", @"\b(angular|angularjs)\b"),
            new("Vue.js", "Web & Framework", @"\b(vue|vuejs|vue\.js|nuxt)\b"),
            new("Node.js", "Web & Framework", @"\b(node\.js|nodejs|express\.js|expressjs)\b"),
            new("Spring Boot", "Web & Framework", @"\b(spring boot|spring mvc|spring framework)\b"),
            new("Django", "Web & Framework", @"\b(django)\b"),
            new("Flask", "Web & Framework", @"\b(flask|fastapi)\b"),
            new("Flutter", "Web & Framework", @"\b(flutter|dart)\b"),
            new("Android", "Web & Framework", @"\b(android|android studio|jetpack compose)\b"),
            new("GraphQL", "Web & Framework", @"\b(graphql)\b"),
            new("jQuery", "Web & Framework", @"\b(jquery)\b"),

            // Databases & Data Access
            new("SQL Server", "Database", @"\b(sql server|mssql|ms sql|microsoft sql server|ssms)\b"),
            new("ADO.NET", "Database", @"\b(ado\.net|adonet|sqlclient|sqlconnection|sqldataadapter)\b"),
            new("Entity Framework", "Database", @"\b(entity framework|ef core|ef6)\b"),
            new("LINQ", "Database", @"\b(linq)\b"),
            new("MySQL", "Database", @"\b(mysql)\b"),
            new("PostgreSQL", "Database", @"\b(postgresql|postgres)\b"),
            new("MongoDB", "Database", @"\b(mongodb|mongo|nosql)\b"),
            new("Redis", "Database", @"\b(redis|caching)\b"),
            new("SQLite", "Database", @"\b(sqlite)\b"),
            new("Oracle", "Database", @"\b(oracle db|oracle database|oracle sql)\b"),

            // Architecture, Testing & Engineering Practices
            new("OOP", "Architecture & Practices", @"\b(oop|oops|object oriented|object-oriented)\b"),
            new("MVC", "Architecture & Practices", @"\b(mvc|model view controller|model-view-controller)\b"),
            new("Dependency Injection", "Architecture & Practices", @"\b(dependency injection|ioc container|inversion of control)\b"),
            new("Clean Architecture", "Architecture & Practices", @"\b(clean architecture|onion architecture|n-tier|3-tier|layered architecture)\b"),
            new("Microservices", "Architecture & Practices", @"\b(microservices|microservice|distributed systems)\b"),
            new("Design Patterns", "Architecture & Practices", @"\b(design patterns|solid principles|repository pattern)\b"),
            new("System Design", "Architecture & Practices", @"\b(system design|scalability|high availability)\b"),
            new("Unit Testing", "Testing & Quality", @"\b(unit testing|unit tests|xunit|nunit|mstest|jest|pytest|junit|tdd)\b"),
            new("Agile", "Collaboration", @"\b(agile|scrum|kanban|sprint|jira)\b"),

            // Core Computer Science
            new("Data Structures", "Core CS", @"\b(data structures|dsa|arrays|linked lists|trees|graphs|hash tables)\b"),
            new("Algorithms", "Core CS", @"\b(algorithms|dsa|sorting|searching|dynamic programming|time complexity)\b"),
            new("Operating Systems", "Core CS", @"\b(operating systems|multithreading|concurrency)\b"),
            new("Computer Networks", "Core CS", @"\b(computer networks|networking|tcp/ip|http|https|dns)\b"),
            new("Cybersecurity", "Core CS", @"\b(cybersecurity|owasp|authentication|authorization|oauth|jwt|encryption)\b"),

            // Cloud, Tools & DevOps
            new("Git", "Tools & DevOps", @"\b(git|version control)\b(?!hub)"),
            new("GitHub", "Tools & DevOps", @"\b(github|gitlab|bitbucket)\b"),
            new("Docker", "Tools & DevOps", @"\b(docker|dockerfile|containerization|containers)\b"),
            new("Kubernetes", "Tools & DevOps", @"\b(kubernetes|k8s)\b"),
            new("AWS", "Cloud & DevOps", @"\b(aws|amazon web services|ec2|s3|aws lambda)\b"),
            new("Azure", "Cloud & DevOps", @"\b(azure|microsoft azure|azure devops|app service)\b"),
            new("Google Cloud", "Cloud & DevOps", @"\b(gcp|google cloud|google cloud platform)\b"),
            new("CI/CD", "Cloud & DevOps", @"\b(ci/cd|cicd|github actions|jenkins|azure pipelines|devops)\b"),
            new("Linux", "Tools & DevOps", @"\b(linux|ubuntu|bash|shell scripting)\b"),
            new("Postman", "Tools & DevOps", @"\b(postman|swagger|openapi)\b"),

            // AI, Data & Analytics
            new("Machine Learning", "Data & AI", @"\b(machine learning|ml\.net|scikit-learn|sklearn|regression|classification)\b|\bml\b"),
            new("Artificial Intelligence", "Data & AI", @"\b(artificial intelligence|deep learning|neural networks|nlp|llm|generative ai)\b|\bai\b"),
            new("Data Analysis", "Data & AI", @"\b(data analysis|data analytics|pandas|numpy|power bi|tableau|excel)\b"),
            new("TensorFlow", "Data & AI", @"\b(tensorflow|keras|pytorch)\b"),

            // Workplace & Professional Skills
            new("Problem Solving", "Professional", @"\b(problem solving|problem-solving|analytical thinking|debugging|troubleshooting)\b"),
            new("Communication", "Professional", @"\b(communication|presentation|technical writing|documentation)\b"),
            new("Leadership", "Professional", @"\b(leadership|mentoring|team lead|mentored|led a team)\b")
        };

        // Transferable / Related Skill Families for fair partial credit and encouraging bridge advice
        private static readonly List<HashSet<string>> RelatedSkillFamilies = new()
        {
            new(StringComparer.OrdinalIgnoreCase) { "SQL", "SQL Server", "MySQL", "PostgreSQL", "SQLite", "Oracle" },
            new(StringComparer.OrdinalIgnoreCase) { "ASP.NET Core", "ASP.NET MVC", "MVC" },
            new(StringComparer.OrdinalIgnoreCase) { "ADO.NET", "Entity Framework", "LINQ" },
            new(StringComparer.OrdinalIgnoreCase) { "JavaScript", "TypeScript" },
            new(StringComparer.OrdinalIgnoreCase) { "React", "Angular", "Vue.js" },
            new(StringComparer.OrdinalIgnoreCase) { "CSS", "Bootstrap", "Tailwind CSS" },
            new(StringComparer.OrdinalIgnoreCase) { "Git", "GitHub" },
            new(StringComparer.OrdinalIgnoreCase) { "AWS", "Azure", "Google Cloud" },
            new(StringComparer.OrdinalIgnoreCase) { "Docker", "Kubernetes", "CI/CD" },
            new(StringComparer.OrdinalIgnoreCase) { "Machine Learning", "Artificial Intelligence", "TensorFlow", "Data Analysis" },
            new(StringComparer.OrdinalIgnoreCase) { "REST API", "GraphQL", "Microservices" },
            new(StringComparer.OrdinalIgnoreCase) { "Django", "Flask" }
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

        // Generic HR / Job-Posting Filler Words excluded from strict keyword penalty so scoring stays fair & useful
        private static readonly HashSet<string> JobPostingNoiseWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "looking", "seeking", "ideal", "candidate", "candidates", "role", "position", "job", "company",
            "team", "teams", "work", "working", "strong", "excellent", "good", "great", "ability", "skills",
            "skill", "required", "requirements", "requirement", "preferred", "plus", "bonus", "years", "year",
            "must", "responsibilities", "qualifications", "apply", "application", "join", "opportunity",
            "equal", "employer", "benefits", "salary", "competitive", "remote", "hybrid", "onsite", "full",
            "time", "part", "fast", "paced", "environment", "passionate", "motivated", "detail", "oriented",
            "etc", "using", "use", "used", "knowledge", "understanding", "familiarity", "proficiency",
            "experience", "experienced", "including", "related", "field", "help", "make", "making", "ensure",
            "high", "quality", "best", "practices", "new", "existing", "various", "multiple", "across",
            "within", "well", "also", "need", "needed", "want", "day", "daily", "hands", "written", "verbal",
            "junior", "senior", "mid", "level", "entry", "hiring", "location", "based", "collaborate", "support"
        };

        private static readonly string[] StrongActionVerbs =
        {
            "developed", "architected", "engineered", "designed", "built", "implemented", "created",
            "maintained", "led", "lead", "optimized", "spearheaded", "deployed", "integrated",
            "automated", "delivered", "launched", "improved", "reduced", "increased", "streamlined",
            "analyzed", "tested", "debugged", "mentored", "collaborated", "managed", "configured"
        };

        public ResumeAnalysisService(IConfiguration? configuration = null)
        {
            // Configurable scoring weights (default 40 / 25 / 20 / 15)
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
                string safeResume = resumeText ?? string.Empty;
                string safeJd = jobDescriptionText ?? string.Empty;

                var analysis = new ResumeAnalysis
                {
                    ResumeId = resumeId,
                    JobDescriptionId = jobDescriptionId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                // 1. SKILLS EXTRACTION & BALANCED MATCHING (Weight: 40%)
                var resumeSkills = ExtractSkillsFromText(safeResume);
                var jdSkills = ExtractSkillsFromText(safeJd);

                var matchedSkills = new List<AnalysisSkill>();
                var missingSkills = new List<AnalysisSkill>();
                var transferableBridges = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                decimal skillsScore;

                if (jdSkills.Count > 0)
                {
                    decimal earnedSkillCredit = 0m;

                    foreach (var target in jdSkills)
                    {
                        var def = SkillTaxonomy.FirstOrDefault(s => string.Equals(s.SkillName, target, StringComparison.OrdinalIgnoreCase))
                                  ?? new SkillDefinition(target, "Technical", "");

                        if (resumeSkills.Contains(target))
                        {
                            earnedSkillCredit += 1.0m;
                            matchedSkills.Add(new AnalysisSkill
                            {
                                SkillName = target,
                                Category = def.Category,
                                IsMatched = true,
                                ProficiencyLevel = "Matched"
                            });
                        }
                        else
                        {
                            string? related = FindTransferableSkill(target, resumeSkills);
                            if (related != null)
                            {
                                // Fair 50% transferable credit so users aren't harshly penalized for closely related tools
                                earnedSkillCredit += 0.5m;
                                transferableBridges[target] = related;
                            }

                            missingSkills.Add(new AnalysisSkill
                            {
                                SkillName = target,
                                Category = def.Category,
                                IsMatched = false,
                                ProficiencyLevel = related != null ? $"Related: {related}" : null
                            });
                        }
                    }

                    skillsScore = Math.Round((earnedSkillCredit / jdSkills.Count) * 100m, 2);
                }
                else if (resumeSkills.Count > 0)
                {
                    // Job description didn't list specific technical tools — reward candidate's demonstrated skills fairly
                    foreach (var skill in resumeSkills)
                    {
                        var def = SkillTaxonomy.FirstOrDefault(s => string.Equals(s.SkillName, skill, StringComparison.OrdinalIgnoreCase))
                                  ?? new SkillDefinition(skill, "Technical", "");
                        matchedSkills.Add(new AnalysisSkill
                        {
                            SkillName = skill,
                            Category = def.Category,
                            IsMatched = true,
                            ProficiencyLevel = "Demonstrated"
                        });
                    }
                    skillsScore = Math.Min(95m, 70m + (resumeSkills.Count * 5m));
                }
                else
                {
                    skillsScore = string.IsNullOrWhiteSpace(safeResume) ? 0m : 60m;
                }

                analysis.SkillsScore = Math.Min(100m, Math.Max(0m, skillsScore));
                analysis.MatchedSkillsCount = matchedSkills.Count;
                analysis.MissingSkillsCount = missingSkills.Count;
                analysis.TotalSkillsIdentified = matchedSkills.Count + missingSkills.Count;

                analysis.Skills.AddRange(matchedSkills);
                analysis.Skills.AddRange(missingSkills);

                // 2. FAIR ROLE KEYWORD ALIGNMENT (Weight: 25%)
                var (keywordsScore, missingDomainKeywords) = EvaluateKeywords(safeResume, safeJd);
                analysis.KeywordsScore = Math.Min(100m, Math.Max(0m, keywordsScore));

                // 3. RELEVANT EXPERIENCE & IMPACT EVALUATION (Weight: 20%)
                var (experienceScore, actionVerbCount, metricsCount) = EvaluateExperience(safeResume);
                analysis.ExperienceScore = Math.Min(100m, Math.Max(0m, experienceScore));

                // 4. EDUCATION, CERTIFICATIONS & READINESS (Weight: 15%)
                decimal educationScore = EvaluateEducation(safeResume, safeJd);
                analysis.EducationScore = Math.Min(100m, Math.Max(0m, educationScore));

                // 5. OVERALL COMPATIBILITY SCORE (Weighted Sum)
                decimal overallScore = (_skillsWeight * analysis.SkillsScore) +
                                       (_keywordsWeight * analysis.KeywordsScore) +
                                       (_experienceWeight * analysis.ExperienceScore) +
                                       (_educationWeight * analysis.EducationScore);

                analysis.OverallScore = Math.Round(Math.Min(100m, Math.Max(0m, overallScore)), 2);

                // 6. RICH, PRACTICAL CAREER COACHING RECOMMENDATIONS
                analysis.Recommendations = GenerateRecommendations(
                    safeResume,
                    matchedSkills,
                    missingSkills,
                    transferableBridges,
                    missingDomainKeywords,
                    analysis.KeywordsScore,
                    analysis.ExperienceScore,
                    analysis.EducationScore,
                    actionVerbCount,
                    metricsCount);

                // 7. CLEAR, TRANSPARENT CALCULATION BREAKDOWN
                analysis.CalculationExplanation = GenerateCalculationExplanation(
                    analysis,
                    transferableBridges.Count,
                    _skillsWeight,
                    _keywordsWeight,
                    _experienceWeight,
                    _educationWeight);

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

        private static string? FindTransferableSkill(string targetSkill, HashSet<string> resumeSkills)
        {
            foreach (var family in RelatedSkillFamilies)
            {
                if (family.Contains(targetSkill))
                {
                    var match = resumeSkills.FirstOrDefault(rs => family.Contains(rs) && !string.Equals(rs, targetSkill, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        return match;
                    }
                }
            }

            return null;
        }

        private (decimal Score, List<string> MissingDomainKeywords) EvaluateKeywords(string resumeText, string jdText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
            {
                return (0m, new List<string>());
            }

            var resumeKeywords = ExtractKeywords(resumeText);
            var jdKeywords = ExtractKeywords(jdText);

            // Filter out generic HR filler so candidates are scored on meaningful domain language
            var meaningfulJdKeywords = jdKeywords
                .Where(k => !JobPostingNoiseWords.Contains(k) && k.Length >= 3)
                .ToList();

            if (meaningfulJdKeywords.Count == 0)
            {
                return (string.IsNullOrWhiteSpace(jdText) ? 0m : 85m, new List<string>());
            }

            var resumeStems = resumeKeywords.Select(StemWord).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int matchedCount = 0;
            var missing = new List<string>();

            foreach (var kw in meaningfulJdKeywords)
            {
                if (resumeKeywords.Contains(kw) || resumeStems.Contains(StemWord(kw)))
                {
                    matchedCount++;
                }
                else if (kw.Length >= 4 && !Regex.IsMatch(kw, @"^\d"))
                {
                    missing.Add(kw);
                }
            }

            // Realistic scaling: matching ~75% of a real job description's meaningful terms is already top-tier
            decimal rawRatio = (decimal)matchedCount / meaningfulJdKeywords.Count;
            decimal scaledScore = Math.Round(Math.Min(100m, rawRatio * 130m), 2);

            return (scaledScore, missing.Take(6).ToList());
        }

        private static string StemWord(string word)
        {
            string w = word.ToLowerInvariant();
            if (w.Length <= 4) return w;

            if (w.EndsWith("ing") && w.Length > 6) return w[..^3];
            if (w.EndsWith("tion") && w.Length > 6) return w[..^4];
            if (w.EndsWith("ment") && w.Length > 6) return w[..^4];
            if (w.EndsWith("ies") && w.Length > 5) return w[..^3] + "y";
            if (w.EndsWith("ed") && w.Length > 5) return w[..^2];
            if (w.EndsWith("es") && w.Length > 5) return w[..^2];
            if (w.EndsWith("s") && !w.EndsWith("ss") && w.Length > 4) return w[..^1];
            return w;
        }

        private static (decimal Score, int ActionVerbCount, int MetricsCount) EvaluateExperience(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
            {
                return (0m, 0, 0);
            }

            decimal score = 55m; // Fair starting baseline for any readable resume

            // Strong action verbs
            int actionCount = StrongActionVerbs.Count(v => Regex.IsMatch(resumeText, $@"\b{v}\b", RegexOptions.IgnoreCase));
            score += Math.Min(20m, actionCount * 4m);

            // Quantifiable impact indicators (percentages, multipliers, scale numbers)
            int metricsCount = Regex.Matches(
                resumeText,
                @"(?:\b\d+(?:\.\d+)?%|\b\d+\+|\$\d+|\b\d+x\b|\b\d+\s*(?:users|clients|customers|projects|requests|hours|days|months|years|members|endpoints|records|ms|seconds)\b)",
                RegexOptions.IgnoreCase).Count;
            score += Math.Min(15m, metricsCount * 5m);

            // Real-world, academic & internship ownership markers
            string[] contextMarkers =
            {
                "project", "projects", "internship", "intern", "experience", "responsibilities",
                "production", "capstone", "freelance", "hackathon", "open source", "portfolio", "client"
            };
            int contextCount = contextMarkers.Count(c => Regex.IsMatch(resumeText, $@"\b{Regex.Escape(c)}\b", RegexOptions.IgnoreCase));
            score += Math.Min(15m, contextCount * 3m);

            return (Math.Min(100m, score), actionCount, metricsCount);
        }

        private static decimal EvaluateEducation(string resumeText, string jdText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
            {
                return 0m;
            }

            // Check if the job description explicitly requests a degree
            bool jdMentionsDegree = Regex.IsMatch(
                jdText,
                @"\b(b\.tech|btech|bachelor|b\.e\.|b\.s\.|b\.sc|m\.tech|master|mca|bca|degree|diploma|phd)\b",
                RegexOptions.IgnoreCase);

            decimal score = jdMentionsDegree ? 50m : 65m;

            string[] degreeMarkers =
            {
                "b.tech", "btech", "bachelor", "b.e.", "b.s.", "b.sc", "bsc", "bca", "mca",
                "m.tech", "mtech", "m.sc", "master", "mba", "bba", "b.com", "diploma", "phd",
                "computer science", "computer engineering", "information technology",
                "software engineering", "data science", "engineering"
            };
            int foundDegrees = degreeMarkers.Count(d => Regex.IsMatch(resumeText, $@"\b{Regex.Escape(d)}\b", RegexOptions.IgnoreCase));
            if (foundDegrees > 0)
            {
                score += 30m;
            }

            // Academic, certification, and career readiness signals
            string[] readinessMarkers =
            {
                "gpa", "cgpa", "coursework", "university", "institute", "college", "graduated",
                "graduation", "semester", "certification", "certified", "certificate",
                "coursera", "udemy", "bootcamp", "github", "linkedin", "portfolio"
            };
            int readinessCount = readinessMarkers.Count(a => Regex.IsMatch(resumeText, $@"\b{Regex.Escape(a)}\b", RegexOptions.IgnoreCase));
            score += Math.Min(20m, readinessCount * 5m);

            return Math.Min(100m, score);
        }

        private static List<Recommendation> GenerateRecommendations(
            string resumeText,
            List<AnalysisSkill> matchedSkills,
            List<AnalysisSkill> missingSkills,
            Dictionary<string, string> transferableBridges,
            List<string> missingDomainKeywords,
            decimal keywordsScore,
            decimal experienceScore,
            decimal educationScore,
            int actionVerbCount,
            int metricsCount)
        {
            var list = new List<Recommendation>();

            // 1. CELEBRATE STRENGTHS FIRST (Encouraging & Honest)
            if (matchedSkills.Count > 0)
            {
                string topMatched = string.Join(", ", matchedSkills.Take(5).Select(s => s.SkillName));
                list.Add(new Recommendation
                {
                    Category = "Key Strength",
                    Suggestion = $"Great match on core skills! Your resume already highlights **{matchedSkills.Count}** relevant competencies including **{topMatched}**. Make sure these stand out near the top of your resume and in your strongest project bullets.",
                    Priority = "Strength"
                });
            }

            if (metricsCount >= 2)
            {
                list.Add(new Recommendation
                {
                    Category = "Impact Writing",
                    Suggestion = "Excellent work quantifying your achievements with numbers and measurable outcomes — resumes with concrete results consistently hold recruiter attention much longer.",
                    Priority = "Strength"
                });
            }

            // 2. PRIORITY SKILL GAPS & TRANSFERABLE BRIDGES (High Priority)
            foreach (var missing in missingSkills.Take(4))
            {
                if (transferableBridges.TryGetValue(missing.SkillName, out var relatedSkill))
                {
                    list.Add(new Recommendation
                    {
                        Category = "Transferable Skill Bridge",
                        Suggestion = $"The role mentions **{missing.SkillName}** ({missing.Category}), and you already have **{relatedSkill}**! You've earned partial credit — if you've touched {missing.SkillName} or can connect your {relatedSkill} concepts to it, highlight that transfer clearly.",
                        Priority = "High"
                    });
                }
                else
                {
                    list.Add(new Recommendation
                    {
                        Category = "Priority Skill Gap",
                        Suggestion = $"This job looks for **{missing.SkillName}** ({missing.Category}). If you have hands-on experience from projects, labs, or internships, weave **{missing.SkillName}** into your Skills and Projects sections — or consider a focused mini-project to build confidence.",
                        Priority = "High"
                    });
                }
            }

            // 3. CONCRETE MISSING ROLE KEYWORDS (Medium Priority)
            if (keywordsScore < 75m && missingDomainKeywords.Count > 0)
            {
                string keywordList = string.Join(", ", missingDomainKeywords);
                list.Add(new Recommendation
                {
                    Category = "Role Language Alignment",
                    Suggestion = $"Mirroring the employer's natural terminology makes your resume easier to scan. Where accurate to your experience, consider incorporating terms from this posting such as: **{keywordList}**.",
                    Priority = "Medium"
                });
            }

            // 4. QUANTIFIED IMPACT COACHING (Medium Priority)
            if (metricsCount < 2 && !string.IsNullOrWhiteSpace(resumeText))
            {
                list.Add(new Recommendation
                {
                    Category = "Quantified Impact",
                    Suggestion = "Add 2–3 measurable results using the **Action + Context + Result** formula. Example: instead of *'Built a backend API'*, try **'Engineered a REST API handling 500+ requests/min and reduced response time by 30%'**.",
                    Priority = "Medium"
                });
            }

            // 5. ACTION VERBS & OWNERSHIP (Medium Priority)
            if (actionVerbCount < 3 && experienceScore < 85m && !string.IsNullOrWhiteSpace(resumeText))
            {
                list.Add(new Recommendation
                {
                    Category = "Stronger Action Verbs",
                    Suggestion = "Start your project and experience bullets with decisive ownership verbs like **Built, Designed, Engineered, Optimized, Automated, or Delivered** instead of passive phrasing like *'Worked on'* or *'Helped with'*.",
                    Priority = "Medium"
                });
            }

            // 6. RESUME COMPLETENESS & SECTION CHECK (Medium / Low Priority)
            if (!string.IsNullOrWhiteSpace(resumeText))
            {
                var missingSections = new List<string>();
                if (!Regex.IsMatch(resumeText, @"\b(project|projects|experience|internship|work history)\b", RegexOptions.IgnoreCase))
                    missingSections.Add("Projects or Experience");
                if (!Regex.IsMatch(resumeText, @"\b(skills|technologies|technical stack|competencies)\b", RegexOptions.IgnoreCase))
                    missingSections.Add("Dedicated Skills Section");
                if (!Regex.IsMatch(resumeText, @"\b(education|university|college|b\.tech|bachelor|degree)\b", RegexOptions.IgnoreCase))
                    missingSections.Add("Education");

                if (missingSections.Count > 0)
                {
                    list.Add(new Recommendation
                    {
                        Category = "Resume Structure",
                        Suggestion = $"Consider adding clear section headings for **{string.Join(", ", missingSections)}**. Clean, predictable headings help both recruiters and screening tools read your profile effortlessly.",
                        Priority = "Medium"
                    });
                }

                // Portfolio / Links check
                if (!Regex.IsMatch(resumeText, @"\b(github|linkedin|portfolio|gitlab)\b", RegexOptions.IgnoreCase))
                {
                    list.Add(new Recommendation
                    {
                        Category = "Portfolio & Links",
                        Suggestion = "Include clickable **LinkedIn** and **GitHub / Portfolio** links in your header. Letting hiring managers inspect live projects or code often makes the difference between a shortlist and a pass.",
                        Priority = "Low"
                    });
                }

                // Word count / scannability check
                int wordCount = Regex.Matches(resumeText, @"\b\w+\b").Count;
                if (wordCount > 0 && wordCount < 140)
                {
                    list.Add(new Recommendation
                    {
                        Category = "Detail & Depth",
                        Suggestion = $"Your resume text appears quite brief (**~{wordCount} words**). Aim for **300–650 words** by adding 2–3 clear bullet points per project or role explaining what you built, which tools you used, and why it mattered.",
                        Priority = "Low"
                    });
                }
                else if (wordCount > 1100)
                {
                    list.Add(new Recommendation
                    {
                        Category = "Scannability & Length",
                        Suggestion = $"Your resume is fairly detailed (**~{wordCount} words**). Consider trimming older or repetitive bullets so recruiters can spot your strongest achievements within the first 10 seconds.",
                        Priority = "Low"
                    });
                }
            }

            // 7. EDUCATION & CERTIFICATIONS POLISH (Low Priority)
            if (educationScore < 80m)
            {
                list.Add(new Recommendation
                {
                    Category = "Education & Credentials",
                    Suggestion = "Highlight your **degree, graduation year, key coursework, or relevant certifications** clearly so recruiters can confirm your qualifications at a glance.",
                    Priority = "Low"
                });
            }

            return list;
        }

        private static string GenerateCalculationExplanation(
            ResumeAnalysis a,
            int transferableCount,
            decimal wSkills,
            decimal wKeywords,
            decimal wExp,
            decimal wEdu)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Your compatibility score is calculated transparently across 4 career pillars:");
            string transferableNote = transferableCount > 0 ? $", plus partial credit for {transferableCount} transferable skill(s)" : string.Empty;
            sb.AppendLine($"• Core & Related Skills ({wSkills:P0} Weight): {a.SkillsScore:F1}% ({a.MatchedSkillsCount}/{a.TotalSkillsIdentified} direct matches{transferableNote})");
            sb.AppendLine($"• Role Language & Keywords ({wKeywords:P0} Weight): {a.KeywordsScore:F1}% (alignment with meaningful job terms, excluding generic HR filler)");
            sb.AppendLine($"• Experience & Impact ({wExp:P0} Weight): {a.ExperienceScore:F1}% (action verbs, quantified results, and project/work context)");
            sb.AppendLine($"• Education & Readiness ({wEdu:P0} Weight): {a.EducationScore:F1}% (degrees, coursework, certifications, and portfolio links)");
            sb.AppendLine();
            sb.AppendLine($"Formula: ({wSkills:F2} × {a.SkillsScore:F1}) + ({wKeywords:F2} × {a.KeywordsScore:F1}) + ({wExp:F2} × {a.ExperienceScore:F1}) + ({wEdu:F2} × {a.EducationScore:F1}) = {a.OverallScore:F1}%");
            sb.AppendLine();
            sb.AppendLine("Note: This score is an explainable career-readiness guide to help you strengthen your application — not an official proprietary ATS score. Real hiring decisions also value your portfolio, communication, and growth potential.");

            return sb.ToString();
        }

        private record SkillDefinition(string SkillName, string Category, string Pattern);
    }
}
