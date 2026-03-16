using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate")]
[Authorize(Roles = "Candidate")]
public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ApplicationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var apps = await _db.Applications
            .Where(a => a.UserId == user.Id)
            .Include(a => a.Job).ThenInclude(j => j.Company)
            .OrderByDescending(a => a.AppliedDate)
            .AsNoTracking()
            .ToListAsync();

        return View(apps);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int jobId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var job = await _db.Jobs
            .Include(j => j.Company)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId && j.IsActive && j.ModerationStatus == "Approved");

        if (job is null) return NotFound();

        var userCvs = await _db.UserCvs
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.IsDefault)
            .ThenByDescending(c => c.UploadedAt)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Job = job;
        ViewBag.UserCvs = userCvs;
        return View(new Application { JobId = jobId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int jobId, string? coverLetter, IFormFile? resumeFile, int? selectedCvId, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId && j.IsActive && j.ModerationStatus == "Approved");
        if (job is null) return NotFound();

        var existing = await _db.Applications.FirstOrDefaultAsync(a => a.JobId == jobId && a.UserId == user.Id);
        if (existing is not null)
        {
            TempData["Error"] = "Bạn đã ứng tuyển công việc này rồi.";
            return RedirectToAction("Details", "Jobs", new { area = "", id = jobId });
        }

        string? resumePath = null;
        if (selectedCvId.HasValue)
        {
            var selectedCv = await _db.UserCvs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == selectedCvId.Value && c.UserId == user.Id);
            if (selectedCv != null)
                resumePath = selectedCv.StoredPath;
        }
        if (resumePath == null && resumeFile is not null && resumeFile.Length > 0)
        {
            var ext = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx" };
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "CV chỉ hỗ trợ file .pdf, .doc, .docx.";
                return RedirectToAction(nameof(Create), new { jobId });
            }

            var resumesDir = Path.Combine(_env.WebRootPath, "uploads", "resumes");
            Directory.CreateDirectory(resumesDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(resumesDir, fileName);
            await using (var stream = System.IO.File.Create(fullPath))
            {
                await resumeFile.CopyToAsync(stream);
            }

            resumePath = $"/uploads/resumes/{fileName}";
        }

        var app = new Application
        {
            JobId = jobId,
            UserId = user.Id,
            CoverLetter = coverLetter,
            ResumePath = resumePath,
            Status = "Pending"
        };

        _db.Applications.Add(app);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Ứng tuyển thành công.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}

