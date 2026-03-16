using JobPortal.Data;
using JobPortal.Models.Entities;
using JobPortal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<FileTextExtractor>();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString) ||
    connectionString.Contains("YOUR_SERVER", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Missing database connection string. Configure ConnectionStrings:DefaultConnection in appsettings.Local.json, appsettings.json, or environment variables.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(60);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Client/Account/Login";
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

var applyMigrationsOnStartup = builder.Configuration.GetValue("AppSetup:ApplyMigrationsOnStartup", true);
var seedDemoDataOnStartup = builder.Configuration.GetValue("AppSetup:SeedDemoDataOnStartup", true);

if (applyMigrationsOnStartup || seedDemoDataOnStartup)
{
    await InitializeApplicationAsync(app.Services, applyMigrationsOnStartup, seedDemoDataOnStartup);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static async Task InitializeApplicationAsync(
    IServiceProvider services,
    bool applyMigrationsOnStartup,
    bool seedDemoDataOnStartup)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (applyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync();
        }

        if (seedDemoDataOnStartup)
        {
            await SeedDemoDataAsync(scope.ServiceProvider, db);
        }
    }
    catch (SqlException ex)
    {
        logger.LogCritical(ex, "Could not connect to SQL Server during startup initialization.");
        throw new InvalidOperationException(
            "Khong the ket noi SQL Server. Hay cap nhat ConnectionStrings:DefaultConnection den mot SQL Server truy cap duoc, hoac tam thoi dat AppSetup:ApplyMigrationsOnStartup = false.",
            ex);
    }
}

static async Task SeedDemoDataAsync(IServiceProvider services, ApplicationDbContext db)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = ["Admin", "Employer", "Candidate"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    const string adminEmail = "admin@gmail.com";
    const string adminPassword = "123456";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser is null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Administrator",
            Role = "Admin",
            EmailConfirmed = true
        };

        var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createAdminResult.Succeeded)
        {
            var errors = string.Join("; ", createAdminResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create default admin user: {errors}");
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        var addAdminRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!addAdminRoleResult.Succeeded)
        {
            var errors = string.Join("; ", addAdminRoleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign Admin role to default admin user: {errors}");
        }
    }

    if (!string.Equals(adminUser.Role, "Admin", StringComparison.Ordinal))
    {
        adminUser.Role = "Admin";
        await userManager.UpdateAsync(adminUser);
    }

    const string employerEmail = "employer@gmail.com";
    const string employerPassword = "123456";

    var employerUser = await userManager.FindByEmailAsync(employerEmail);
    if (employerUser is null)
    {
        employerUser = new ApplicationUser
        {
            UserName = employerEmail,
            Email = employerEmail,
            FullName = "Demo Employer",
            Role = "Employer",
            EmailConfirmed = true
        };

        var createEmployerResult = await userManager.CreateAsync(employerUser, employerPassword);
        if (!createEmployerResult.Succeeded)
        {
            var errors = string.Join("; ", createEmployerResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create demo employer user: {errors}");
        }
    }

    if (!await userManager.IsInRoleAsync(employerUser, "Employer"))
    {
        var addEmployerRoleResult = await userManager.AddToRoleAsync(employerUser, "Employer");
        if (!addEmployerRoleResult.Succeeded)
        {
            var errors = string.Join("; ", addEmployerRoleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign Employer role to demo employer user: {errors}");
        }
    }

    if (!string.Equals(employerUser.Role, "Employer", StringComparison.Ordinal))
    {
        employerUser.Role = "Employer";
        await userManager.UpdateAsync(employerUser);
    }

    var demoCompany = await db.Companies.FirstOrDefaultAsync(c => c.UserId == employerUser.Id);
    if (demoCompany is null)
    {
        demoCompany = new Company
        {
            UserId = employerUser.Id,
            CompanyName = "DemoTech JSC",
            Industry = "Software",
            Website = "https://example.com",
            Address = "Ha Noi",
            CompanySize = "Medium",
            Description = "Cong ty demo de trinh bay BTL."
        };
        db.Companies.Add(demoCompany);
        await db.SaveChangesAsync();
    }

    var hasApprovedJobs = await db.Jobs.AnyAsync(j => j.CompanyId == demoCompany.Id && j.ModerationStatus == "Approved");
    if (hasApprovedJobs)
    {
        return;
    }

    var categoryId = await db.Categories
        .OrderBy(c => c.Id)
        .Select(c => c.Id)
        .FirstOrDefaultAsync();

    if (categoryId == 0)
    {
        throw new InvalidOperationException("No categories available to seed demo jobs.");
    }

    db.Jobs.AddRange(
        new Job
        {
            CompanyId = demoCompany.Id,
            CategoryId = categoryId,
            Title = "Junior .NET Developer",
            Description = "Tham gia phat trien he thong ASP.NET Core.\nLam viec voi SQL Server va EF Core.",
            Requirements = "- Nam co ban C#/.NET\n- Biet Git la loi the",
            Location = "Ha Noi",
            JobType = "Full-time",
            ExperienceLevel = "Junior",
            SalaryMin = 12000000,
            SalaryMax = 20000000,
            Vacancies = 2,
            IsActive = true,
            IsFeatured = true,
            ModerationStatus = "Approved",
            ModeratedAt = DateTime.Now,
            ModeratedByUserId = adminUser.Id,
            ModerationNote = "Seed demo job"
        },
        new Job
        {
            CompanyId = demoCompany.Id,
            CategoryId = categoryId,
            Title = "QA Tester (Intern)",
            Description = "Ho tro test manual cho web.\nViet test case, report bug.",
            Requirements = "- Can than\n- Ham hoc hoi",
            Location = "Remote",
            JobType = "Internship",
            ExperienceLevel = "Fresher",
            SalaryMin = 3000000,
            SalaryMax = 5000000,
            Vacancies = 1,
            IsActive = true,
            IsFeatured = false,
            ModerationStatus = "Approved",
            ModeratedAt = DateTime.Now,
            ModeratedByUserId = adminUser.Id,
            ModerationNote = "Seed demo job"
        });

    await db.SaveChangesAsync();
}
