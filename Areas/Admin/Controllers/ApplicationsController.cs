using JobPortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ApplicationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status = null, int page = 1)
    {
        const int pageSize = 20;

        var query = _db.Applications
            .Include(a => a.Job)
            .Include(a => a.User)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            query = query.Where(a => a.Status == status);
        }

        var totalApplications = await query.CountAsync();
        var applications = await query
            .OrderByDescending(a => a.AppliedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Status = status ?? "All";
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalApplications == 0 ? 1 : (int)Math.Ceiling(totalApplications / (double)pageSize);
        ViewBag.TotalApplications = totalApplications;

        return View(applications);
    }
}
