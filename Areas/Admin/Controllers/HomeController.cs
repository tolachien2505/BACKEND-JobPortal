using JobPortal.Data;
using JobPortal.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var totalJobs = await _db.Jobs.CountAsync();
        var pendingJobs = await _db.Jobs.CountAsync(j => j.ModerationStatus == "Pending");
        var approvedJobs = await _db.Jobs.CountAsync(j => j.ModerationStatus == "Approved");
        var rejectedJobs = await _db.Jobs.CountAsync(j => j.ModerationStatus == "Rejected");

        var totalUsers = await _db.Users.CountAsync();
        var totalCompanies = await _db.Companies.CountAsync();
        var totalApplications = await _db.Applications.CountAsync();
        var totalCategories = await _db.Categories.CountAsync();

        var pendingApps = await _db.Applications.CountAsync(a => a.Status == "Pending");
        var reviewingApps = await _db.Applications.CountAsync(a => a.Status == "Reviewing");
        var interviewApps = await _db.Applications.CountAsync(a => a.Status == "Interview");
        var rejectedApps = await _db.Applications.CountAsync(a => a.Status == "Rejected");
        var acceptedApps = await _db.Applications.CountAsync(a => a.Status == "Accepted");

        var pipelineApplications = pendingApps + reviewingApps + interviewApps;
        var completedApplications = acceptedApps + rejectedApps;

        var jobModerationStats = new[]
        {
            CreateStatusItem("Pending", "Chờ duyệt", pendingJobs, totalJobs, "#f59e0b", "#fff7e6", "fas fa-hourglass-half"),
            CreateStatusItem("Approved", "Đã duyệt", approvedJobs, totalJobs, "#10b981", "#ecfdf5", "fas fa-circle-check"),
            CreateStatusItem("Rejected", "Từ chối", rejectedJobs, totalJobs, "#ef4444", "#fef2f2", "fas fa-circle-xmark")
        };

        var applicationStats = new[]
        {
            CreateStatusItem("Pending", "Chờ duyệt", pendingApps, totalApplications, "#f59e0b", "#fff7e6", "fas fa-hourglass-half"),
            CreateStatusItem("Reviewing", "Đang xem xét", reviewingApps, totalApplications, "#0ea5e9", "#eff6ff", "fas fa-eye"),
            CreateStatusItem("Interview", "Phỏng vấn", interviewApps, totalApplications, "#6366f1", "#eef2ff", "fas fa-user-tie"),
            CreateStatusItem("Accepted", "Đã nhận", acceptedApps, totalApplications, "#10b981", "#ecfdf5", "fas fa-handshake"),
            CreateStatusItem("Rejected", "Từ chối", rejectedApps, totalApplications, "#ef4444", "#fef2f2", "fas fa-circle-xmark")
        };

        var model = new AdminDashboardViewModel
        {
            TotalJobs = totalJobs,
            PendingJobs = pendingJobs,
            ApprovedJobs = approvedJobs,
            RejectedJobs = rejectedJobs,
            TotalUsers = totalUsers,
            TotalCompanies = totalCompanies,
            TotalApplications = totalApplications,
            TotalCategories = totalCategories,
            PipelineApplications = pipelineApplications,
            CompletedApplications = completedApplications,
            PipelinePercentage = CalculatePercentage(pipelineApplications, totalApplications),
            CompletionPercentage = CalculatePercentage(completedApplications, totalApplications),
            InterviewRate = CalculatePercentage(interviewApps, totalApplications),
            AcceptanceRate = CalculatePercentage(acceptedApps, totalApplications),
            JobModerationStats = jobModerationStats,
            ApplicationStats = applicationStats
        };

        return View(model);
    }

    private static DashboardStatusItemViewModel CreateStatusItem(
        string key,
        string label,
        int value,
        int total,
        string color,
        string softColor,
        string iconClass)
    {
        return new DashboardStatusItemViewModel
        {
            Key = key,
            Label = label,
            Value = value,
            Percentage = CalculatePercentage(value, total),
            Color = color,
            SoftColor = softColor,
            IconClass = iconClass
        };
    }

    private static double CalculatePercentage(int value, int total)
    {
        return total == 0 ? 0 : Math.Round(value * 100d / total, 1);
    }
}
