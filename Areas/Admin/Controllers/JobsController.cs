using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class JobsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public JobsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status = "Pending")
    {
        status ??= "Pending";

        var jobs = await _db.Jobs
            .Include(j => j.Company)
            .Include(j => j.Category)
            .Where(j => j.ModerationStatus == status)
            .OrderByDescending(j => j.PostedDate)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Status = status;
        return View(jobs);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var job = await _db.Jobs
            .Include(j => j.Company)
            .Include(j => j.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();

        ViewBag.Status = job.ModerationStatus;
        return View(job);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();

        job.ModerationStatus = "Approved";
        job.ModeratedAt = DateTime.Now;
        job.ModeratedByUserId = admin.Id;
        job.ModerationNote = "Approved by admin.";
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã duyệt tin.";
        return RedirectToAction(nameof(Index), new { status = "Pending" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? note)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();

        job.ModerationStatus = "Rejected";
        job.ModeratedAt = DateTime.Now;
        job.ModeratedByUserId = admin.Id;
        job.ModerationNote = string.IsNullOrWhiteSpace(note) ? "Rejected by admin." : note.Trim();
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã từ chối tin.";
        return RedirectToAction(nameof(Index), new { status = "Pending" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa tin.";
        return RedirectToAction(nameof(Index), new { status = "Pending" });
    }
}

