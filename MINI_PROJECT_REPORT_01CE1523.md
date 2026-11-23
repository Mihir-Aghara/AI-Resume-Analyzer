# AI RESUME ANALYZER: ATS COMPATIBILITY & EXPLAINABLE CAREER COACHING WEB PLATFORM
## MINI PROJECT
### .NET TECHNOLOGIES (01CE1523)

---

### Submitted by
**Mihir Aghara**  
[MU Enrolment Number: 92400103xxx]  

**Sachin Kumar**  
92400103265  

---

### BACHELOR OF TECHNOLOGY
**in**  
**Computer Engineering**  

```
========================================================================
   MARWADI UNIVERSITY | FACULTY OF ENGINEERING & TECHNOLOGY (NAAC A+)
========================================================================
```

**Faculty of Engineering & Technology**  
**Marwadi University, Rajkot**  
**November, 2026**  

---

## CHAPTER 1: PROJECT OBJECTIVE

### 1.1 AIM OF THE PROJECT
The primary aim of the **AI Resume Analyzer** project is to bridge the gap between job candidates and automated Applicant Tracking Systems (ATS) by providing an explainable, deterministic resume review and vacancy compatibility platform. The system extracts text natively from PDF and DOCX files without external cloud leakage, calculates multi-pillar match scores, identifies skill gaps, and delivers recruiter-aligned career coaching recommendations. The application is built using **ASP.NET Core 8.0 LTS MVC**, Razor Views, pure **ADO.NET** with **Microsoft SQL Server LocalDB**, and PBKDF2 SHA-256 cryptographic authentication.

---

### 1.2 OBJECTIVES
The specific technical and academic objectives of this project are:
1. **To design a modern, responsive, and user-friendly interface** using ASP.NET Core MVC, Razor Views, and Bootstrap 5.3 with glassmorphic startup styling.
2. **To architect an enterprise-grade data layer** utilizing pure ADO.NET (`SqlConnection`, `SqlCommand`, `SqlDataReader`, `SqlDataAdapter`, `DataSet`, `DataTable`, `SqlTransaction`) with 0% Entity Framework Core overhead.
3. **To develop a deterministic 4-pillar ATS evaluation engine** that benchmarks Technical Skills (40%), Role Keywords (25%), Impact & Experience (20%), and Education Readiness (15%) with partial credit for related tech stacks.
4. **To implement secure authentication** using persistent HTTP-only cookie claims, PBKDF2 SHA-256 password hashing with salt, password manager paste normalization, and database-persisted self-service password recovery.
5. **To build high-fidelity dual-format document ingestion pipelines** supporting PDF (`UglyToad.PdfPig`) and Microsoft Word (`DocumentFormat.OpenXml`) natively in pure .NET without third-party API dependencies.
6. **To enforce role-based access control (Admin and User)** and provide comprehensive reporting, user analytics, and downloadable audit dossiers.

---

### 1.3 SCOPE OF THE PROJECT
- **Target Audience:** The system is tailored for undergraduate students seeking internships, entry-level job seekers, career switchers targeting new software engineering stacks, and working professionals tailoring applications for senior positions.
- **Covered Modules:** End-to-end user onboarding with real email credentials, secure document parsing, deterministic ATS evaluation, transparent scoring breakdowns, actionable career recommendations, user history dossiers, and administrative system oversight.
- **Future Scope:** Real-time multi-page document layout rendering via WebAssembly canvas, multilingual internationalization (i18n), automated mock interview question generation based on detected skill gaps, and direct campus placement ERP synchronization.

---

## CHAPTER 2: INTRODUCTION

### 2.1 TOOLS AND TECHNOLOGIES USED

| Component | Technology Used |
| :--- | :--- |
| **Front End** | HTML5, CSS3, Razor Views, Bootstrap 5.3, Bootstrap Icons, JavaScript (jQuery Unobtrusive Validation) |
| **Back End / Language** | C# 12 (.NET 8.0 LTS) |
| **Framework** | ASP.NET Core MVC (Model-View-Controller) |
| **Data Access** | Pure ADO.NET (`Microsoft.Data.SqlClient`) — Zero EF Core |
| **Database** | Microsoft SQL Server (LocalDB / Express / Standard Edition) |
| **Document Parsing** | `UglyToad.PdfPig` (PDF) & `DocumentFormat.OpenXml` (Word DOCX) |
| **Security / Crypto** | `System.Security.Cryptography` (PBKDF2 SHA-256, 100,000 iterations, 128-bit salt, 256-bit key) |
| **IDE / Tools** | Microsoft Visual Studio 2022 Community / Professional |
| **Version Control** | Git & GitHub (`Mihir-Aghara/AI-Resume-Analyzer`) |

*Table 2.1: Tools and Technologies Used*

---

### 2.2 MODULES OF THE PROJECT
1. **Authentication & Identity Recovery Module:** Manages secure user registration, cookie-based session management, password manager paste sanitization, and database-persisted self-service password reset.
2. **Landing Page & Navigation Module:** High-conversion, rich white startup interface with glassmorphic top navigation, dynamic authentication awareness, and transparent ATS methodology breakdown.
3. **Resume Ingestion & Parsing Module:** Validates file size and format, stores resumes under secure isolated storage (`App_Data/Resumes`), and extracts raw text natively from `.pdf` and `.docx` documents.
4. **4-Pillar ATS Analysis Engine Module:** Executes deterministic lexical matching against target vacancies, computing Technical Skills (40%), Keywords (25%), Impact (20%), and Education (15%) weights.
5. **Skill Gap Radar & Career Coaching Module:** Identifies matched competencies, detected transferable skills, missing mandatory keywords, and generates prioritized, actionable advice.
6. **User Dashboard & History Dossier Module:** Displays aggregate career metrics, recent reviews, and detailed historical evaluation breakdowns.
7. **Administrative Oversight Module:** Provides platform administrators with real-time analytics, user activation toggling, and global analysis audit logs.

---

## CHAPTER 3: SCREENSHOTS

This chapter presents key user interface screens of the AI Resume Analyzer application, demonstrating the user workflow from initial authentication and profile setup to document ingestion, transparent ATS match scoring, and historical dossier tracking.

---

### Figure 3.1: Login Page
```
+-------------------------------------------------------------+
|                      Welcome Back                           |
|       Sign in to your AI Resume Analyzer account            |
|                                                             |
|  Email Address: [ mihiraghara8@gmail.com                 ]  |
|  Password:      [ ••••••••                             ]    |
|  [X] Keep me signed in on this device                       |
|                                                             |
|                 [   --> Sign In   ]                         |
|                                                             |
|  [v] Protected by PBKDF2 SHA-256 Hashing & Encrypted Cookies|
+-------------------------------------------------------------+
```
*Figure 3.1: Login Page*  
**Explanation:** The Login Page provides secure user authentication featuring client/server validation, 14-day persistent cookie options, password visibility toggling, and contextual self-service recovery links.

---

### Figure 3.2: User Registration & Onboarding Page
```
+-------------------------------------------------------------+
|                   Create Free Account                       |
|      Get started with AI-driven ATS resume analysis         |
|                                                             |
|  Full Name:     [ Mihir Aghara                           ]  |
|  Email Address: [ mihiraghara8@gmail.com                 ]  |
|  Password:      [ ••••••••••   ]  Confirm: [ ••••••••••  ]  |
|  [X] I agree to the Privacy Policy and Terms of Service     |
|                                                             |
|               [  Create Free Account  ]                     |
+-------------------------------------------------------------+
```
*Figure 3.2: User Registration & Onboarding Page*  
**Explanation:** The Registration Page enables new candidates to register using real email addresses, incorporating custom `[MustBeTrue]` terms verification, password complexity enforcement, and transactional welcome emails.

---

### Figure 3.3: Home / Landing Page (Rich White Startup Aesthetic)
```
+-------------------------------------------------------------------------+
| [Doc] AI Resume Analyzer    Home  Features  How It Works  Sign In [Get Started] |
+-------------------------------------------------------------------------+
|                                                                         |
|      [*] Next-Generation Resume Review & Job Match Intelligence         |
|                                                                         |
|               Turn Your Resume Into                                     |
|               Interview Invitations.                                    |
|                                                                         |
|    Stop submitting applications into the dark. Benchmark your resume    |
|    against target roles with transparent 4-pillar scoring.              |
|                                                                         |
|         [ Analyze Your Resume Free ]   [ See How It Works ]             |
|                                                                         |
|   +-------------------+-------------------+-------------------+         |
|   |     4-Pillar      |      < 60s        |       100%        |         |
|   | Deterministic ATS | Instant Extractor | Private & Secure  |         |
|   +-------------------+-------------------+-------------------+         |
+-------------------------------------------------------------------------+
```
*Figure 3.3: Home / Landing Page (Rich White Startup Aesthetic)*  
**Explanation:** The Home Landing Page utilizes a luminous rich white canvas with ambient mesh glows, glassmorphic navigation, transparent methodology badges, and a streamlined 3-step candidate workflow.

---

### Figure 3.4: Resume Upload & Analysis Submission Interface
```
+-------------------------------------------------------------+
|                      Upload New Resume                      |
|                                                             |
|   + - - - - - - - - - - - - - - - - - - - - - - - - - - +   |
|   |  [Cloud Icon] Drag and drop your resume file here   |   |
|   |         Supports PDF (.pdf) and Word (.docx)        |   |
|   |               [ Select File from PC ]               |   |
|   + - - - - - - - - - - - - - - - - - - - - - - - - - - +   |
|                                                             |
|   Target Job Title:   [ .NET Core Backend Engineer       ]  |
|   Target Company:     [ Microsoft                        ]  |
|   Job Description:    [ Paste target vacancy text here...]  |
|                                                             |
|                 [   Start AI Analysis   ]                   |
+-------------------------------------------------------------+
```
*Figure 3.4: Resume Upload & Analysis Submission Interface*  
**Explanation:** The Upload Interface provides a drag-and-drop file ingestion zone accepting PDF and Word DOCX formats paired with target job description specification fields.

---

### Figure 3.5: Resume Analysis Match Report & 4-Pillar Scoring Radar
```
+-----------------------------------------------------------------------+
|  Resume Match Report                                   [Strong Match] |
|                                                                       |
|  ( 88 )  Software Development Engineer                                |
|  /100    Matched 7 of 8 key role competencies                         |
|          [ Ready to Apply ]  Updated just now                         |
|                                                                       |
|  Skills Match:        [==========================  ] 92% (Weight 40%) |
|  Role Keywords:       [=======================     ] 84% (Weight 25%) |
|  Impact & Experience: [========================    ] 86% (Weight 20%) |
|  Education Readiness: [=========================   ] 90% (Weight 15%) |
|                                                                       |
|  DETECTED STRENGTHS & OPPORTUNITIES                                   |
|  [v Python] [v REST APIs] [v SQL Databases] [v Git & CI/CD] [+Docker] |
|                                                                       |
|  Career Coach Suggestion:                                             |
|  "Quantify your backend project impact — e.g., 'Improved API latency  |
|  by 35% for 2,000+ daily requests' — to catch recruiter attention."  |
+-----------------------------------------------------------------------+
```
*Figure 3.5: Resume Analysis Match Report & 4-Pillar Scoring Radar*  
**Explanation:** The Analysis Report delivers a transparent 4-pillar compatibility breakdown, highlighting matched skills, transferable competencies, missing keywords, and recruiter-proven coaching suggestions.

---

### Figure 3.6: User Dashboard & History Review Dossier
```
+-----------------------------------------------------------------------+
|  Candidate Dashboard                                                  |
|                                                                       |
|  [ Total Resumes: 3 ]   [ Total Reviews: 7 ]   [ Avg Match: 84.6% ]   |
|                                                                       |
|  RECENT RESUME EVALUATIONS                                            |
|  - Senior .NET Developer    | Score: 88.5% | Strong Match | [Report]  |
|  - Full Stack Engineer     | Score: 79.2% | Medium Match | [Report]  |
|  - Cloud Backend Architect | Score: 91.0% | Top Match    | [Report]  |
+-----------------------------------------------------------------------+
```
*Figure 3.6: User Dashboard & History Review Dossier*  
**Explanation:** The Dashboard centralizes user career metrics, recent evaluations, quick-action analysis triggers, and downloadable historical evaluation records.

---

### Figure 3.7: Self-Service Password Reset & Direct Access Flow
```
+-------------------------------------------------------------+
|                  Reset Request Received                     |
|           Account recovery for your profile                 |
|                                                             |
|  We processed a password reset request for:                 |
|  mihiraghara8@gmail.com                                     |
|                                                             |
|  +-------------------------------------------------------+  |
|  | [Direct Access Link]                                  |  |
|  | You can set your new password immediately below:      |  |
|  |             [ Set New Password Now ]                  |  |
|  +-------------------------------------------------------+  |
|                                                             |
|                    <-- Back to Sign In                      |
+-------------------------------------------------------------+
```
*Figure 3.7: Self-Service Password Reset & Direct Access Flow*  
**Explanation:** The Password Reset Confirmation screen generates a secure 24-hour token saved to SQL Server, providing immediate one-click reset access without external email server dependencies.

---

## CHAPTER 4: CODE

This chapter provides key source code extracts highlighting architectural patterns, controller routing, deterministic evaluation algorithms, pure ADO.NET data access, and cryptographic security implementations.

---

### Listing 4.1: `AnalysisController.cs` — Core MVC Controller
```csharp
using Microsoft.AspNetCore.Mvc;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Models;
using AIResumeAnalyzer.ViewModels;

namespace AIResumeAnalyzer.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IAnalysisRepository _analysisRepository;
        private readonly IResumeAnalysisService _analysisService;
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(
            IResumeRepository resumeRepo,
            IAnalysisRepository analysisRepo,
            IResumeAnalysisService analysisService,
            ILogger<AnalysisController> logger)
        {
            _resumeRepository = resumeRepo;
            _analysisRepository = analysisRepo;
            _analysisService = analysisService;
            _logger = logger;
        }

        // POST: /Analysis/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnalysisRequestViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var resumes = await _resumeRepository.GetByUserIdAsync(userId.Value);
            var selectedResume = resumes.FirstOrDefault(r => r.ResumeId == model.ResumeId);
            if (selectedResume == null)
            {
                ModelState.AddModelError("ResumeId", "Selected resume not found.");
                return View(model);
            }

            // 1. Persist Target Job Vacancy in SQL Server
            var jd = new JobDescription
            {
                UserId = userId.Value,
                JobTitle = model.JobTitle.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                RawText = model.JobDescriptionText.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            int jdId = await _analysisRepository.CreateJobDescriptionAsync(jd);

            // 2. Execute Deterministic 4-Pillar Evaluation Engine
            var result = await _analysisService.AnalyzeResumeAsync(
                selectedResume.ExtractedText, jd.RawText, selectedResume.ResumeId, jdId, userId.Value);

            // 3. Persist Analysis, Detected Skills & Coaching Recommendations
            int analysisId = await _analysisRepository.SaveAnalysisAsync(result);

            TempData["SuccessMessage"] = $"Analysis completed! Match Score: {result.OverallScore:F1}%.";
            return RedirectToAction("Result", new { id = analysisId });
        }
    }
}
```

---

### Listing 4.2: `ResumeAnalysisService.cs` — 4-Pillar Scoring Algorithm
```csharp
// 4-PILLAR DETERMINISTIC ATS SCORING ALGORITHM
public async Task<ResumeAnalysis> AnalyzeResumeAsync(
    string resumeText, string jdText, int resumeId, int jdId, int userId)
{
    // Pillar 1: Technical Skills Compatibility (40% Weight)
    var (skillsScore, skillEntities) = EvaluateSkills(resumeText, jdText);

    // Pillar 2: Role Domain Keywords (25% Weight)
    var (keywordScore, missingKeywords) = EvaluateKeywords(resumeText, jdText);

    // Pillar 3: Impact, Action Verbs & Metrics (20% Weight)
    decimal impactScore = EvaluateImpactAndExperience(resumeText);

    // Pillar 4: Education, Degrees & Readiness (15% Weight)
    decimal educationScore = EvaluateEducationAndReadiness(resumeText);

    // Composite ATS Match Score Formula
    decimal overallScore = Math.Round(
        (skillsScore * 0.40m) +
        (keywordScore * 0.25m) +
        (impactScore * 0.20m) +
        (educationScore * 0.15m), 1);

    var recommendations = GenerateRecommendations(
        skillsScore, keywordScore, impactScore, educationScore, skillEntities, missingKeywords);

    return new ResumeAnalysis
    {
        ResumeId = resumeId,
        JobDescriptionId = jdId,
        UserId = userId,
        OverallScore = overallScore,
        SkillsMatchScore = skillsScore,
        KeywordMatchScore = keywordScore,
        ExperienceScore = impactScore,
        EducationScore = educationScore,
        Skills = skillEntities,
        Recommendations = recommendations,
        AnalyzedAt = DateTime.UtcNow
    };
}
```

---

### Listing 4.3: `UserRepository.cs` — Pure ADO.NET Data Access Implementation
```csharp
// PURE ADO.NET IMPLEMENTATION (0% ENTITY FRAMEWORK CORE OVERHEAD)
public async Task<User?> GetByEmailAsync(string email)
{
    const string sql = @"
        SELECT UserId, FullName, Email, PasswordHash, PasswordSalt, Role, CreatedAt, UpdatedAt, IsActive
        FROM Users
        WHERE Email = @Email;";

    await using var conn = await _db.CreateOpenConnectionAsync();
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) 
    { 
        Value = email.Trim().ToLowerInvariant() 
    });

    await using var reader = await cmd.ExecuteReaderAsync();
    return await reader.ReadAsync() ? MapUser(reader) : null;
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

    return await cmd.ExecuteNonQueryAsync() > 0;
}
```

---

### Listing 4.4: `PasswordHasher.cs` — PBKDF2 SHA-256 Cryptography & Normalization
```csharp
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32;  // 256 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public (string Hash, string Salt) HashPassword(string password)
    {
        string normalized = NormalizePassword(password);
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(normalized, salt, Iterations, Algorithm, KeySize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        try
        {
            byte[] salt = Convert.FromBase64String(storedSalt);
            byte[] expectedHash = Convert.FromBase64String(storedHash);

            // 1. Check normalized password (handles password manager copy-paste quirks)
            string normalized = NormalizePassword(password);
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(normalized, salt, Iterations, Algorithm, KeySize);
            if (CryptographicOperations.FixedTimeEquals(actualHash, expectedHash)) return true;

            // 2. Fallback to raw password verification
            if (!string.Equals(password, normalized, StringComparison.Ordinal))
            {
                byte[] rawActual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
                if (CryptographicOperations.FixedTimeEquals(rawActual, expectedHash)) return true;
            }
            return false;
        }
        catch { return false; }
    }

    private static string NormalizePassword(string input) =>
        string.IsNullOrEmpty(input) ? string.Empty :
        input.Trim().Trim('\r', '\n', '\t', ' ')
             .Replace("\u200B", "").Replace("\uFEFF", "").Replace("\u00A0", " ");
}
```

---

### Listing 4.5: `01_Schema.sql` — Relational SQL Server Database Schema
```sql
-- RELATIONAL SCHEMA SCRIPT: AIResumeAnalyzerDB
CREATE TABLE [dbo].[Users] (
    [UserId] INT IDENTITY(1,1) NOT NULL,
    [FullName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [PasswordHash] NVARCHAR(256) NOT NULL,
    [PasswordSalt] NVARCHAR(128) NOT NULL,
    [Role] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT ('User'),
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] DATETIME2(7) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
    [ResetToken] NVARCHAR(100) NULL,
    [ResetTokenExpiresAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC)
);

CREATE TABLE [dbo].[Resumes] (
    [ResumeId] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [OriginalFileName] NVARCHAR(260) NOT NULL,
    [StoredFileName] NVARCHAR(100) NOT NULL,
    [FilePath] NVARCHAR(500) NOT NULL,
    [FileType] NVARCHAR(10) NOT NULL, -- '.pdf' or '.docx'
    [FileSizeBytes] BIGINT NOT NULL,
    [ExtractedText] NVARCHAR(MAX) NOT NULL,
    [UploadedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Resumes_UploadedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Resumes] PRIMARY KEY CLUSTERED ([ResumeId] ASC),
    CONSTRAINT [FK_Resumes_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
);
```
