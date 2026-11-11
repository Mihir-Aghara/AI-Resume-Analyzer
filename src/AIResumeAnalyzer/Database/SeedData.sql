-- ============================================================================
-- AI RESUME ANALYZER - SEED DATA SCRIPT
-- Populates initial Admin account, demo Student user, job descriptions, 
-- and a baseline sample resume analysis for demonstration.
-- ============================================================================

USE [AIResumeAnalyzerDB];
GO

-- 1. SEED USERS
-- Admin Password: Admin@123
-- Student Password: Student@123
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'admin@airesume.local')
BEGIN
    INSERT INTO [dbo].[Users] ([FullName], [Email], [PasswordHash], [PasswordSalt], [Role], [CreatedAt], [IsActive])
    VALUES (
        N'System Administrator',
        N'admin@airesume.local',
        N'GLSdStCCTOFBF1N1BhZsLPJ2S7E8vQhArlGsRXFB4Ms=',
        N'Dvi09g4JHXmMe2+fJs+hgQ==',
        N'Admin',
        SYSUTCDATETIME(),
        1
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'student@college.edu')
BEGIN
    INSERT INTO [dbo].[Users] ([FullName], [Email], [PasswordHash], [PasswordSalt], [Role], [CreatedAt], [IsActive])
    VALUES (
        N'Alex Johnson',
        N'student@college.edu',
        N'3s1KxDBgYBKx3Dlywbsd7ZdOL9LDh9jheOUgwipoYFQ=',
        N'WsrorNybDhDC8FhZytMoVQ==',
        N'User',
        SYSUTCDATETIME(),
        1
    );
END
GO

-- 2. SEED JOB DESCRIPTIONS
DECLARE @UserId INT;
SELECT @UserId = [UserId] FROM [dbo].[Users] WHERE [Email] = 'student@college.edu';

IF @UserId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[JobDescriptions] WHERE [JobTitle] = N'.NET Core Backend Engineer')
BEGIN
    INSERT INTO [dbo].[JobDescriptions] ([UserId], [JobTitle], [CompanyName], [RawText], [CreatedAt])
    VALUES (
        @UserId,
        N'.NET Core Backend Engineer',
        N'TechNova Solutions',
        N'We are seeking a Junior/Mid-level .NET Backend Engineer with strong expertise in C#, ASP.NET Core, and Microsoft SQL Server. The candidate will design and maintain high-performance REST APIs, implement data access layers using ADO.NET and relational databases, adhere to OOP design patterns and Clean Architecture, and collaborate via Git and GitHub. Experience with Docker, Machine Learning basics, and Cloud concepts is a plus. Requirements: B.Tech/B.E. in Computer Science or equivalent, 1-2 years hands-on experience or substantial capstone projects.',
        SYSUTCDATETIME()
    );
END
GO

-- 3. SEED SAMPLE RESUME
DECLARE @AlexId INT;
SELECT @AlexId = [UserId] FROM [dbo].[Users] WHERE [Email] = 'student@college.edu';

IF @AlexId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Resumes] WHERE [OriginalFileName] = N'Alex_Johnson_Resume.pdf')
BEGIN
    INSERT INTO [dbo].[Resumes] ([UserId], [OriginalFileName], [StoredFileName], [FilePath], [FileType], [FileSizeBytes], [ExtractedText], [UploadedAt])
    VALUES (
        @AlexId,
        N'Alex_Johnson_Resume.pdf',
        N'seed-sample-resume-alex-johnson-2026.pdf',
        N'App_Data/Resumes/seed-sample-resume-alex-johnson-2026.pdf',
        N'.pdf',
        142857,
        N'ALEX JOHNSON
Email: student@college.edu | Phone: +1-555-0199 | Location: New York, NY
GitHub: github.com/alexjohnson | LinkedIn: linkedin.com/in/alexjohnson

PROFESSIONAL SUMMARY
Motivated B.Tech Computer Engineering graduate with strong foundation in C#, ASP.NET Core MVC, and database architecture. Proven experience building secure web applications, RESTful APIs, and implementing ADO.NET for relational data storage.

TECHNICAL SKILLS
- Programming Languages: C#, Python, SQL, JavaScript
- Frameworks & Web: ASP.NET Core, ASP.NET MVC, HTML5, CSS3, Bootstrap, REST API
- Databases: Microsoft SQL Server, ADO.NET, MySQL
- Core Competencies: OOP, Data Structures, Algorithms, MVC Architecture, Dependency Injection
- Tools & Version Control: Git, GitHub, Visual Studio, Postman

PROJECTS
1. E-Commerce Inventory Portal (ASP.NET Core MVC, ADO.NET, SQL Server)
- Architected a 3-tier MVC inventory management system with raw ADO.NET data access.
- Implemented role-based authentication, parameterized SQL queries preventing SQL injection, and session management.

2. Placement Record System (C#, Windows Forms, SQL Server)
- Built automated student placement tracking tool utilizing ADO.NET DataSets and DataTables.

EDUCATION
- Bachelor of Technology (B.Tech) in Computer Engineering | GPA: 3.8/4.0 (2022 - 2026)',
        SYSUTCDATETIME()
    );
END
GO

-- 4. SEED SAMPLE RESUME ANALYSIS
DECLARE @AlexUserId INT, @SampleResumeId INT, @SampleJobId INT, @AnalysisId INT;
SELECT @AlexUserId = [UserId] FROM [dbo].[Users] WHERE [Email] = 'student@college.edu';
SELECT @SampleResumeId = [ResumeId] FROM [dbo].[Resumes] WHERE [OriginalFileName] = N'Alex_Johnson_Resume.pdf';
SELECT @SampleJobId = [JobDescriptionId] FROM [dbo].[JobDescriptions] WHERE [JobTitle] = N'.NET Core Backend Engineer';

IF @AlexUserId IS NOT NULL AND @SampleResumeId IS NOT NULL AND @SampleJobId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[ResumeAnalyses] WHERE [ResumeId] = @SampleResumeId AND [JobDescriptionId] = @SampleJobId)
BEGIN
    INSERT INTO [dbo].[ResumeAnalyses] (
        [ResumeId], [JobDescriptionId], [UserId],
        [OverallScore], [SkillsScore], [KeywordsScore], [ExperienceScore], [EducationScore],
        [MatchedSkillsCount], [MissingSkillsCount], [TotalSkillsIdentified],
        [CalculationExplanation], [CreatedAt]
    )
    VALUES (
        @SampleResumeId, @SampleJobId, @AlexUserId,
        83.50, 85.00, 80.00, 82.50, 90.00,
        11, 2, 13,
        N'Calculated using 4-pillar weighted evaluation: Skills Match (40%): 85.00% [Matched 11/13 target skills including C#, ASP.NET Core, SQL Server, REST API, ADO.NET, Git, GitHub, OOP]; Keyword Overlap (25%): 80.00% [Strong alignment across backend terms]; Relevant Experience (20%): 82.50% [Demonstrated academic capstone and production patterns]; Education & Qualifications (15%): 90.00% [Relevant B.Tech degree in Computer Engineering detected]. Final Score: 83.50%',
        SYSUTCDATETIME()
    );

    SET @AnalysisId = SCOPE_IDENTITY();

    -- Seed Analysis Skills
    INSERT INTO [dbo].[AnalysisSkills] ([AnalysisId], [SkillName], [Category], [IsMatched], [ProficiencyLevel])
    VALUES 
        (@AnalysisId, N'C#', N'Programming Language', 1, N'Advanced'),
        (@AnalysisId, N'ASP.NET Core', N'Web & Framework', 1, N'Advanced'),
        (@AnalysisId, N'SQL Server', N'Database', 1, N'Proficient'),
        (@AnalysisId, N'ADO.NET', N'Database', 1, N'Proficient'),
        (@AnalysisId, N'REST API', N'Web & Framework', 1, N'Proficient'),
        (@AnalysisId, N'Git', N'Tools & DevOps', 1, N'Proficient'),
        (@AnalysisId, N'GitHub', N'Tools & DevOps', 1, N'Proficient'),
        (@AnalysisId, N'OOP', N'Architecture & Practices', 1, N'Advanced'),
        (@AnalysisId, N'Data Structures', N'Core CS', 1, N'Proficient'),
        (@AnalysisId, N'Algorithms', N'Core CS', 1, N'Proficient'),
        (@AnalysisId, N'MVC', N'Architecture & Practices', 1, N'Advanced'),
        (@AnalysisId, N'Docker', N'Tools & DevOps', 0, NULL),
        (@AnalysisId, N'Machine Learning', N'Emerging Tech', 0, NULL);

    -- Seed Recommendations
    INSERT INTO [dbo].[Recommendations] ([AnalysisId], [Category], [Suggestion], [Priority])
    VALUES
        (@AnalysisId, N'Missing Skill', N'The job description explicitly mentions containerization with Docker. Consider containerizing one of your ASP.NET Core capstone projects and highlighting Dockerfile/compose experience.', N'High'),
        (@AnalysisId, N'Missing Skill', N'Machine Learning basics was cited as a bonus. Highlight coursework or basic ML.NET integration to stand out.', N'Medium'),
        (@AnalysisId, N'Keyword Enhancement', N'Incorporate terms like "CI/CD Pipeline", "Clean Architecture", and "Unit Testing" in your project bullet points.', N'Low');
END
GO
