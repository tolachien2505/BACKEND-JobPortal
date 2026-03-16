using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using JobPortal.Models.Entities;

namespace JobPortal.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<SavedJob> SavedJobs { get; set; }
    public DbSet<UserCv> UserCvs { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<UserRoadmap> UserRoadmaps { get; set; }
    public DbSet<GeminiApiKey> GeminiApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole<int>>().ToTable("Roles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        // Company - User relationship
        builder.Entity<Company>()
            .HasOne(c => c.User)
            .WithOne(u => u.Company)
            .HasForeignKey<Company>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Job - Company relationship
        builder.Entity<Job>()
            .HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Job - Category relationship
        builder.Entity<Job>()
            .HasOne(j => j.Category)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Application - Job relationship
        builder.Entity<Application>()
            .HasOne(a => a.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Application - User relationship
        builder.Entity<Application>()
            .HasOne(a => a.User)
            .WithMany(u => u.Applications)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // SavedJob - Job relationship
        builder.Entity<SavedJob>()
            .HasOne(sj => sj.Job)
            .WithMany(j => j.SavedJobs)
            .HasForeignKey(sj => sj.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // SavedJob - User relationship
        builder.Entity<SavedJob>()
            .HasOne(sj => sj.User)
            .WithMany(u => u.SavedJobs)
            .HasForeignKey(sj => sj.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserCv - User relationship
        builder.Entity<UserCv>()
            .HasOne(uc => uc.User)
            .WithMany(u => u.UserCvs)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ChatSession - User relationship
        builder.Entity<ChatSession>()
            .HasOne(cs => cs.User)
            .WithMany(u => u.ChatSessions)
            .HasForeignKey(cs => cs.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ChatMessage - ChatSession relationship
        builder.Entity<ChatMessage>()
            .HasOne(cm => cm.ChatSession)
            .WithMany(cs => cs.Messages)
            .HasForeignKey(cm => cm.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserRoadmap - User relationship
        builder.Entity<UserRoadmap>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoadmaps)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserRoadmap - ChatSession relationship
        builder.Entity<UserRoadmap>()
            .HasOne(ur => ur.ChatSession)
            .WithMany(cs => cs.Roadmaps)
            .HasForeignKey(ur => ur.ChatSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Category self-referencing
        builder.Entity<Category>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Job>()
            .Property(j => j.ModerationStatus)
            .HasDefaultValue("Pending");

        // Seed Categories
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Công nghệ thông tin" },
            new Category { Id = 2, Name = "Kinh doanh / Marketing" },
            new Category { Id = 3, Name = "Kế toán / Tài chính" },
            new Category { Id = 4, Name = "Nhân sự / Hành chính" },
            new Category { Id = 5, Name = "Kỹ thuật" },
            new Category { Id = 6, Name = "Giáo dục / Đào tạo" },
            new Category { Id = 7, Name = "Y tế / Chăm sóc sức khỏe" },
            new Category { Id = 8, Name = "Du lịch / Khách sạn" }
        );
    }
}
