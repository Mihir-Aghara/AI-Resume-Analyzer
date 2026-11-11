using AIResumeAnalyzer.Data;
using AIResumeAnalyzer.Interfaces;
using AIResumeAnalyzer.Middleware;
using AIResumeAnalyzer.Repositories;
using AIResumeAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// Ensure managed networking for SQL Server on Windows
AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

// 1. MVC Controllers, Web API & Razor Views
builder.Services.AddControllersWithViews();

// 2. HTTP Context & Session Management
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".AIResumeAnalyzer.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// 3. ADO.NET Infrastructure & Repositories Registration (Dependency Injection)
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

// 4. Core Business Services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IResumeTextExtractor, ResumeTextExtractor>();
builder.Services.AddScoped<IResumeAnalysisService, ResumeAnalysisService>();

var app = builder.Build();

// 5. Custom Security & Exception Middleware
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable ASP.NET Core Session state
app.UseSession();

app.UseAuthorization();

// Route Web API Controllers (e.g., /api/resumes, /api/analyses, /api/users)
app.MapControllers();

// Route MVC Views
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
