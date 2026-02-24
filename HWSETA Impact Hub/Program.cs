using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Infrastructure.Audit;
using HWSETA_Impact_Hub.Infrastructure.Confugations;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Infrastructure.RequestContext;
using HWSETA_Impact_Hub.Infrastructure.Seed;
using HWSETA_Impact_Hub.Services.Implementations;
using HWSETA_Impact_Hub.Services.Implementations.HWSETA_Impact_Hub.Services.Implementations;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Bind Security settings
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection("Sms"));

// MVC + Razor Pages (Identity UI)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ✅ Cookie paths (Individual Accounts)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Dynamic Authorization Policies from appsettings.json
builder.Services.AddAuthorization(options =>
{
    var sec = builder.Configuration.GetSection("Security").Get<SecurityOptions>() ?? new SecurityOptions();

    foreach (var kvp in sec.Policies)
    {
        var policyName = kvp.Key;
        var roles = kvp.Value?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray() ?? Array.Empty<string>();

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            if (roles.Length > 0)
                policy.RequireRole(roles);
        });
    }
});


// HttpContext
builder.Services.AddHttpContextAccessor();

// Request/User Context
builder.Services.AddScoped<IRequestContext, RequestContextAccessor>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Audit
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

// Services
builder.Services.AddScoped<IProgrammeService, ProgrammeService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IEmployerService, EmployerService>();
builder.Services.AddScoped<IBeneficiaryService, BeneficiaryService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICohortService, CohortService>();
builder.Services.AddScoped<IEnrollmentDocumentService, EnrollmentDocumentService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IFormTemplateService, FormTemplateService>();
builder.Services.AddScoped<IFormSubmissionService, FormSubmissionService>();

builder.Services.AddScoped<IEmailSenderService, SmtpEmailSenderService>();

// SMS sender selection:
builder.Services.AddHttpClient(); // for gateway sender
builder.Services.AddScoped<GatewaySmsSenderService>();
builder.Services.AddScoped<DevSmsSenderService>();
builder.Services.AddScoped<ISmsSenderService>(sp =>
{
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmsOptions>>().Value;
    if (string.Equals(opt.Provider, "Gateway", StringComparison.OrdinalIgnoreCase))
        return sp.GetRequiredService<GatewaySmsSenderService>();

    return sp.GetRequiredService<DevSmsSenderService>();
});

builder.Services.AddScoped<IBeneficiaryInviteService, BeneficiaryInviteService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();




var app = builder.Build();
await IdentitySeeder.SeedAsync(app.Services);
// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();   // ensure schema

    await LookupSeeder.SeedAsync(db);   // add missing rows
    await db.SaveChangesAsync();        // ✅ CRITICAL
}
app.UseHttpsRedirection();

// ✅ required for wwwroot files used by layout
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();