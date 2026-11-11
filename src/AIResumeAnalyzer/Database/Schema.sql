-- ============================================================================
-- AI RESUME ANALYZER - DATABASE CREATION & SCHEMA SCRIPT
-- Database: AIResumeAnalyzerDB
-- Target Engine: Microsoft SQL Server (LocalDB / Express / Standard / Enterprise)
-- Concepts Demonstrated: Normalized 3NF Schema, Primary Keys, Foreign Keys,
--   Unique Constraints, Default Constraints, Cascading Deletes, Clustered/Non-Clustered Indexes
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AIResumeAnalyzerDB')
BEGIN
    CREATE DATABASE [AIResumeAnalyzerDB];
END
GO

USE [AIResumeAnalyzerDB];
GO

-- 1. USERS TABLE
-- Stores credentials (PBKDF2 SHA-256 hashed), role-based claims, and account status
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [UserId] INT IDENTITY(1,1) NOT NULL,
        [FullName] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(256) NOT NULL,
        [PasswordHash] NVARCHAR(256) NOT NULL,
        [PasswordSalt] NVARCHAR(128) NOT NULL,
        [Role] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT ('User'), -- 'User' or 'Admin'
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2(7) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC)
    );
END
GO

-- 2. RESUMES TABLE
-- Stores uploaded resume metadata, secure storage paths, and extracted text
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Resumes')
BEGIN
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
        CONSTRAINT [UQ_Resumes_StoredFileName] UNIQUE NONCLUSTERED ([StoredFileName] ASC),
        CONSTRAINT [FK_Resumes_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
END
GO

-- 3. JOB DESCRIPTIONS TABLE
-- Stores job postings entered by users for resume matching
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobDescriptions')
BEGIN
    CREATE TABLE [dbo].[JobDescriptions] (
        [JobDescriptionId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [JobTitle] NVARCHAR(150) NOT NULL,
        [CompanyName] NVARCHAR(150) NULL,
        [RawText] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_JobDescriptions_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_JobDescriptions] PRIMARY KEY CLUSTERED ([JobDescriptionId] ASC),
        CONSTRAINT [FK_JobDescriptions_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 4. RESUME ANALYSES TABLE
-- Stores the calculated scores across 4 evaluation pillars and high-level metrics
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ResumeAnalyses')
BEGIN
    CREATE TABLE [dbo].[ResumeAnalyses] (
        [AnalysisId] INT IDENTITY(1,1) NOT NULL,
        [ResumeId] INT NOT NULL,
        [JobDescriptionId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [OverallScore] DECIMAL(5, 2) NOT NULL,
        [SkillsScore] DECIMAL(5, 2) NOT NULL,
        [KeywordsScore] DECIMAL(5, 2) NOT NULL,
        [ExperienceScore] DECIMAL(5, 2) NOT NULL,
        [EducationScore] DECIMAL(5, 2) NOT NULL,
        [MatchedSkillsCount] INT NOT NULL CONSTRAINT [DF_ResumeAnalyses_Matched] DEFAULT (0),
        [MissingSkillsCount] INT NOT NULL CONSTRAINT [DF_ResumeAnalyses_Missing] DEFAULT (0),
        [TotalSkillsIdentified] INT NOT NULL CONSTRAINT [DF_ResumeAnalyses_Total] DEFAULT (0),
        [CalculationExplanation] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_ResumeAnalyses_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ResumeAnalyses] PRIMARY KEY CLUSTERED ([AnalysisId] ASC),
        CONSTRAINT [FK_ResumeAnalyses_Resumes] FOREIGN KEY ([ResumeId]) 
            REFERENCES [dbo].[Resumes] ([ResumeId]),
        CONSTRAINT [FK_ResumeAnalyses_JobDescriptions] FOREIGN KEY ([JobDescriptionId]) 
            REFERENCES [dbo].[JobDescriptions] ([JobDescriptionId]),
        CONSTRAINT [FK_ResumeAnalyses_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[Users] ([UserId])
    );
END
GO

-- 5. ANALYSIS SKILLS TABLE
-- Stores individual skills detected, matching status (matched vs missing), and domain category
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AnalysisSkills')
BEGIN
    CREATE TABLE [dbo].[AnalysisSkills] (
        [SkillId] INT IDENTITY(1,1) NOT NULL,
        [AnalysisId] INT NOT NULL,
        [SkillName] NVARCHAR(100) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [IsMatched] BIT NOT NULL,
        [ProficiencyLevel] NVARCHAR(50) NULL,
        CONSTRAINT [PK_AnalysisSkills] PRIMARY KEY CLUSTERED ([SkillId] ASC),
        CONSTRAINT [FK_AnalysisSkills_ResumeAnalyses] FOREIGN KEY ([AnalysisId]) 
            REFERENCES [dbo].[ResumeAnalyses] ([AnalysisId]) ON DELETE CASCADE
    );
END
GO

-- 6. RECOMMENDATIONS TABLE
-- Stores actionable improvement guidance grouped by priority
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Recommendations')
BEGIN
    CREATE TABLE [dbo].[Recommendations] (
        [RecommendationId] INT IDENTITY(1,1) NOT NULL,
        [AnalysisId] INT NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [Suggestion] NVARCHAR(MAX) NOT NULL,
        [Priority] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Recommendations_Priority] DEFAULT ('Medium'), -- 'High', 'Medium', 'Low'
        CONSTRAINT [PK_Recommendations] PRIMARY KEY CLUSTERED ([RecommendationId] ASC),
        CONSTRAINT [FK_Recommendations_ResumeAnalyses] FOREIGN KEY ([AnalysisId]) 
            REFERENCES [dbo].[ResumeAnalyses] ([AnalysisId]) ON DELETE CASCADE
    );
END
GO

-- INDEXES FOR QUERY OPTIMIZATION
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Resumes_UserId')
    CREATE NONCLUSTERED INDEX [IX_Resumes_UserId] ON [dbo].[Resumes] ([UserId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ResumeAnalyses_UserId')
    CREATE NONCLUSTERED INDEX [IX_ResumeAnalyses_UserId] ON [dbo].[ResumeAnalyses] ([UserId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ResumeAnalyses_ResumeId')
    CREATE NONCLUSTERED INDEX [IX_ResumeAnalyses_ResumeId] ON [dbo].[ResumeAnalyses] ([ResumeId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AnalysisSkills_AnalysisId')
    CREATE NONCLUSTERED INDEX [IX_AnalysisSkills_AnalysisId] ON [dbo].[AnalysisSkills] ([AnalysisId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Recommendations_AnalysisId')
    CREATE NONCLUSTERED INDEX [IX_Recommendations_AnalysisId] ON [dbo].[Recommendations] ([AnalysisId]);
GO
