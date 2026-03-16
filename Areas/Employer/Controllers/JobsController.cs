using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Employer.Controllers;

[Area("Employer")]
[Authorize(Roles = "Employer")]
public class JobsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public JobsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task<Company?> GetMyCompanyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return null;
        return await _db.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var company = await GetMyCompanyAsync();
        if (company is null)
        {
            TempData["Error"] = "Bạn cần cập nhật thông tin công ty trước khi đăng tin.";
            return RedirectToAction("Edit", "Company");
        }

        var jobs = await _db.Jobs
            .Where(j => j.CompanyId == company.Id)
            .Include(j => j.Category)
            .OrderByDescending(j => j.PostedDate)
            .AsNoTracking()
            .ToListAsync();

        return View(jobs);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var company = await GetMyCompanyAsync();
        if (company is null)
        {
            TempData["Error"] = "Bạn cần cập nhật thông tin công ty trước khi đăng tin.";
            return RedirectToAction("Edit", "Company");
        }

        await LoadCategoriesAsync();
        return View(new Job { CompanyId = company.Id, IsActive = true, ModerationStatus = "Pending" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Job model)
    {
        var company = await GetMyCompanyAsync();
        if (company is null)
        {
            TempData["Error"] = "Bạn cần cập nhật thông tin công ty trước khi đăng tin.";
            return RedirectToAction("Edit", "Company");
        }

        model.CompanyId = company.Id;
        model.PostedDate = DateTime.Now;
        model.ModerationStatus = "Pending";
        model.ModeratedAt = null;
        model.ModeratedByUserId = null;
        model.ModerationNote = null;

        ModelState.Remove("Company");
        ModelState.Remove("Category");
        ModelState.Remove("Applications");
        ModelState.Remove("SavedJobs");

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        _db.Jobs.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã tạo bài đăng. Bài sẽ hiển thị sau khi Admin duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var company = await GetMyCompanyAsync();
        if (company is null) return RedirectToAction("Edit", "Company");

        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);
        if (job is null) return NotFound();

        await LoadCategoriesAsync();
        return View(job);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Job model)
    {
        var company = await GetMyCompanyAsync();
        if (company is null) return RedirectToAction("Edit", "Company");

        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);
        if (job is null) return NotFound();

        ModelState.Remove("Company");
        ModelState.Remove("Category");
        ModelState.Remove("Applications");
        ModelState.Remove("SavedJobs");

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        job.Title = model.Title;
        job.Description = model.Description;
        job.Requirements = model.Requirements;
        job.Location = model.Location;
        job.SalaryMin = model.SalaryMin;
        job.SalaryMax = model.SalaryMax;
        job.JobType = model.JobType;
        job.ExperienceLevel = model.ExperienceLevel;
        job.Vacancies = model.Vacancies;
        job.ExpiryDate = model.ExpiryDate;
        job.IsActive = model.IsActive;
        job.IsFeatured = model.IsFeatured;
        job.CategoryId = model.CategoryId;

        job.ModerationStatus = "Pending";
        job.ModeratedAt = null;
        job.ModeratedByUserId = null;
        job.ModerationNote = "Employer updated job; requires re-approval.";

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật. Bài sẽ hiển thị sau khi Admin duyệt lại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var company = await GetMyCompanyAsync();
        if (company is null) return RedirectToAction("Edit", "Company");

        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.CompanyId == company.Id);
        if (job is null) return NotFound();

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa bài đăng.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _db.Categories.OrderBy(c => c.Name).AsNoTracking().ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");
    }
}

