using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate")]
[Authorize(Roles = "Candidate")]
public class SavedJobsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SavedJobsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var saved = await _db.SavedJobs
            .Where(s => s.UserId == user.Id)
            .Include(s => s.Job).ThenInclude(j => j.Company)
            .Include(s => s.Job).ThenInclude(j => j.Category)
            .OrderByDescending(s => s.SavedDate)
            .AsNoTracking()
            .ToListAsync();

        return View(saved);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int jobId, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var existing = await _db.SavedJobs.FirstOrDefaultAsync(s => s.JobId == jobId && s.UserId == user.Id);
        if (existing is null)
        {
            _db.SavedJobs.Add(new SavedJob { JobId = jobId, UserId = user.Id });
            TempData["Success"] = "Đã lưu việc làm.";
        }
        else
        {
            _db.SavedJobs.Remove(existing);
            TempData["Success"] = "Đã bỏ lưu việc làm.";
        }

        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}

