using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using JobPortal.Models;
using JobPortal.Models.ViewModels.Home;

namespace JobPortal.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var categories = new List<JobPortal.Models.Entities.Category>();
        var featuredJobs = new List<HomeFeaturedJobCardViewModel>();

        try
        {
            categories = await _context.Categories
                .OrderBy(c => c.Id)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load categories on home page");
        }

        try
        {
            featuredJobs = await _context.Jobs
                .Where(j => j.ModerationStatus == "Approved" && j.IsActive)
                .OrderByDescending(j => j.IsFeatured)
                .ThenByDescending(j => j.PostedDate)
                .Select(j => new HomeFeaturedJobCardViewModel
                {
                    Id = j.Id,
                    Title = j.Title,
                    CompanyName = j.Company.CompanyName,
                    CompanyLogo = j.Company.Logo,
                    SalaryMin = j.SalaryMin,
                    SalaryMax = j.SalaryMax,
                    Location = j.Location,
                    JobType = j.JobType,
                    ExperienceLevel = j.ExperienceLevel,
                    IsFeatured = j.IsFeatured,
                    CategoryName = j.Category.Name,
                    PostedDate = j.PostedDate
                })
                .Take(6)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load featured jobs on home page");
        }

        var model = new HomeIndexViewModel
        {
            Categories = categories,
            FeaturedJobs = featuredJobs
        };
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult GuideCv()
    {
        ViewData["Title"] = "Hướng dẫn viết CV";
        return View();
    }

    public IActionResult CvTemplates()
    {
        ViewData["Title"] = "Mẫu CV tham khảo";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
