using JobPortal.Data;
using JobPortal.Models.ViewModels.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PagedList.Core;

namespace JobPortal.Controllers;

public class JobsController : Controller
{
    private readonly ApplicationDbContext _db;

    public JobsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] JobSearchViewModel vm)
    {
        vm.Page = vm.Page <= 0 ? 1 : vm.Page;
        vm.PageSize = vm.PageSize is < 5 or > 50 ? 10 : vm.PageSize;

        vm.Categories = await _db.Categories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();

        var query = _db.Jobs
            .Include(j => j.Company)
            .Include(j => j.Category)
            .Where(j => j.IsActive && j.ModerationStatus == "Approved")
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(vm.Keyword))
        {
            var kw = vm.Keyword.Trim();
            query = query.Where(j =>
                j.Title.Contains(kw) ||
                j.Description.Contains(kw) ||
                (j.Requirements != null && j.Requirements.Contains(kw)) ||
                j.Company.CompanyName.Contains(kw));
        }

        if (vm.CategoryId is > 0)
        {
            query = query.Where(j => j.CategoryId == vm.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(vm.Location))
        {
            var loc = vm.Location.Trim();
            query = query.Where(j => j.Location != null && j.Location.Contains(loc));
        }

        if (!string.IsNullOrWhiteSpace(vm.JobType))
        {
            query = query.Where(j => j.JobType == vm.JobType);
        }

        if (!string.IsNullOrWhiteSpace(vm.ExperienceLevel))
        {
            query = query.Where(j => j.ExperienceLevel == vm.ExperienceLevel);
        }

        if (vm.SalaryMin is > 0)
        {
            query = query.Where(j => (j.SalaryMax ?? j.SalaryMin) >= vm.SalaryMin);
        }

        query = query.OrderByDescending(j => j.IsFeatured)
            .ThenByDescending(j => j.PostedDate);

        vm.Results = new PagedList<JobPortal.Models.Entities.Job>(query, vm.Page, vm.PageSize);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var job = await _db.Jobs
            .Include(j => j.Company)
            .Include(j => j.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id && j.IsActive && j.ModerationStatus == "Approved");

        if (job is null)
        {
            return NotFound();
        }

        bool isSaved = false;
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Candidate"))
        {
            var userName = User.Identity.Name;
            var user = await _db.Users
                .AsNoTracking()
                .Select(u => new { u.Id, u.UserName })
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user != null)
            {
                isSaved = await _db.SavedJobs.AnyAsync(s => s.JobId == id && s.UserId == user.Id);
            }
        }
        ViewBag.IsSaved = isSaved;

        return View(job);
    }
}

