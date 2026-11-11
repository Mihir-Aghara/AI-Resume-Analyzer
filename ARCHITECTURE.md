# Architectural Design Document: AI Resume Analyzer

**Academic Submission – B.Tech Computer Engineering, Semester 5**  
**Target Subject:** .NET Technologies

---

## 1. Architectural Philosophy & Design Principles

The application is structured according to the classic **Layered Model-View-Controller (MVC) Architectural Pattern** combined with **Inversion of Control (IoC) / Dependency Injection (DI)**.

### Core Principles Maintained:
1. **Separation of Concerns (SoC):** Controllers are lightweight orchestrators that handle HTTP request binding, session validation, and view/JSON dispatching. Business computations reside in Services, and persistent data storage is encapsulated in Repositories.
2. **Single Responsibility Principle (SRP):** Each repository manages a distinct relational entity aggregate; the `ResumeTextExtractor` only processes file streams; `PasswordHasher` strictly manages cryptographic hashes.
3. **Pure ADO.NET Mandate:** To fulfill syllabus requirements without relying on ORM abstraction, all data interactions use Microsoft SQL Server ADO.NET objects (`SqlConnection`, `SqlCommand`, `SqlDataReader`, `SqlDataAdapter`, `DataSet`, `DataTable`, `SqlTransaction`).
4. **Defense in Depth Security:** Multiple security layers protect against SQL injection (parameterized queries), Cross-Site Scripting (Razor HTML encoding + CSP headers), Cross-Site Request Forgery (Anti-forgery tokens), and arbitrary file upload execution (binary magic byte checks + non-public storage).

---

## 2. Component Layering Breakdown

### A. Presentation Layer
- **MVC Controllers:**
  - `HomeController`: Landing page and global error handler.
  - `AccountController`: Register, Login, Logout, Profile update, and Password change.
  - `DashboardController`: User metrics and activity overview.
  - `ResumeController`: Document upload, file validation, listing, and inspection.
  - `AnalysisController`: Benchmark execution, results gauge, history, and printable report.
  - `AdminController`: Administrative KPIs and user role management.
- **REST Web API Controllers:**
  - `ResumeApiController` (`/api/resumes`): Document retrieval and upload via API.
  - `AnalysisApiController` (`/api/analyses`): Programmatic resume analysis execution.
  - `UserApiController` (`/api/users`): Profile and statistic endpoints.
- **Razor Views & Partials:**
  - Server-side Razor markup rendering responsive HTML5 and Bootstrap 5 components.
  - Partial views (`_Navbar`, `_Footer`, `_NotificationPartial`, `_ValidationScriptsPartial`) promote DRY principles.

### B. Security & Pipeline Middleware Layer
- `SecurityHeadersMiddleware`: Emits `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, and `Referrer-Policy`.
- `ExceptionHandlingMiddleware`: Intercepts unhandled exceptions globally. Returns structured JSON for API calls and redirects to `/Home/Error` for MVC views.
- `SessionAuthorizeAttribute`: Protects candidate resources by verifying active session claims.
- `AdminAuthorizeAttribute`: Enforces Role-Based Access Control (RBAC) ensuring only `Admin` users access administrative endpoints.

### C. Business Services Layer
- `IPasswordHasher` / `PasswordHasher`:
  - Algorithm: PBKDF2 with HMAC-SHA256
  - Iterations: 100,000
  - Salt Size: 128 bits (cryptographically random)
  - Key Size: 256 bits
  - Time-constant comparison via `CryptographicOperations.FixedTimeEquals` preventing timing attacks.
- `IResumeTextExtractor` / `ResumeTextExtractor`:
  - Binary signature validation (checking for `%PDF` and `PK\x03\x04`).
  - Text extraction from PDF documents using `PdfPig`.
  - Text extraction from Word (.docx) documents using `DocumentFormat.OpenXml`.
  - Normalizes line feeds, tabs, and excess whitespace.
- `IResumeAnalysisService` / `ResumeAnalysisService`:
  - Technical Taxonomy covering 6 categories and 25+ essential skills.
  - Lexical tokenization filtering out natural language English stop words.
  - Action verb and seniority analysis for experience alignment.
  - Accredited degree detection for qualification alignment.
  - Weighted composite formula with configurable weights.
  - Generation of prioritized improvement guidance.

### D. Data Access Layer (Pure ADO.NET)
- `IDbConnectionFactory` / `DbConnectionFactory`:
  - Encapsulates connection string retrieval and returns `SqlConnection` instances.
  - Configured with managed networking (`Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows`).
- `UserRepository`:
  - Demonstrates **Connected ADO.NET Architecture** using `SqlCommand.ExecuteReaderAsync()` and `SqlDataReader`.
- `ResumeRepository`:
  - Demonstrates **Disconnected ADO.NET Architecture** using `SqlDataAdapter.Fill()` into `DataSet` and `DataTable`.
- `AnalysisRepository`:
  - Demonstrates **Transactional ADO.NET** using `SqlTransaction` to ensure that inserting into `ResumeAnalyses`, `AnalysisSkills`, and `Recommendations` succeeds as an atomic unit or rolls back on failure.
- `AdminRepository`:
  - Direct SQL Server aggregate queries (`COUNT`, `AVG`, `MAX`) computing system telemetry.

---

## 3. Request Pipeline Flow

```text
HTTP Request
     │
     ▼
[SecurityHeadersMiddleware]       ── Injects security headers
     │
     ▼
[ExceptionHandlingMiddleware]     ── Catches & formats unhandled exceptions
     │
     ▼
[UseStaticFiles]                  ── Serves CSS, JS, Images from wwwroot
     │
     ▼
[UseRouting]                      ── Matches URL routes
     │
     ▼
[UseSession]                      ── Deserializes session cookie into HttpContext.Session
     │
     ▼
[Action Filters]                  ── [SessionAuthorize] or [AdminAuthorize] validation
     │
     ▼
[Controller Action]               ── Model binding & validation (ModelState.IsValid)
     │
     ▼
[Business Services]               ── ResumeTextExtractor / ResumeAnalysisService
     │
     ▼
[ADO.NET Repositories]            ── Parameterized queries executed on SQL Server
     │
     ▼
[View / JSON Response]            ── Rendered HTML or structured ApiResponse<T>
```

---

## 4. Academic Evaluation Checklist

- [x] C# and ASP.NET Core MVC architecture
- [x] Pure ADO.NET implementation (SqlConnection, SqlCommand, SqlDataReader, SqlDataAdapter, DataSet, DataTable, SqlTransaction)
- [x] Zero Entity Framework Core primary dependency
- [x] Microsoft SQL Server 2025 LocalDB (`AIResumeAnalyzerDB`)
- [x] PBKDF2 SHA-256 password hashing with salt
- [x] Session-based state management and custom Action Filters
- [x] PDF and DOCX text extraction with pure .NET libraries
- [x] Explainable 4-pillar compatibility scoring engine
- [x] Candidate User Dashboard and Admin Center
- [x] RESTful Web API endpoints (`/api/resumes`, `/api/analyses`, `/api/users`)
- [x] 21 automated passing xUnit unit and integration tests
- [x] Comprehensive documentation and two-student collaboration breakdown
