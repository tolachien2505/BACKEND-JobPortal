using System.IO;
using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate")]
[Authorize(Roles = "Candidate")]
public class CvsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public CvsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
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

        var list = await _db.UserCvs
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.IsDefault)
            .ThenByDescending(c => c.UploadedAt)
            .AsNoTracking()
            .ToListAsync();

        return View(list);
    }

    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file CV.";
            return RedirectToAction(nameof(Upload));
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx" };
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = "Chỉ chấp nhận file .pdf, .doc, .docx.";
            return RedirectToAction(nameof(Upload));
        }

        if (file.Length > 5 * 1024 * 1024) // 5MB
        {
            TempData["Error"] = "Kích thước file tối đa 5MB.";
            return RedirectToAction(nameof(Upload));
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "cvs");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var isFirst = !await _db.UserCvs.AnyAsync(c => c.UserId == user.Id);
        var userCv = new UserCv
        {
            UserId = user.Id,
            FileName = file.FileName,
            StoredPath = $"/uploads/cvs/{fileName}",
            IsDefault = isFirst
        };
        _db.UserCvs.Add(userCv);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Tải CV lên thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var cv = await _db.UserCvs.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        if (cv is null) return NotFound();

        var fullPath = Path.Combine(_env.WebRootPath, cv.StoredPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath))
        {
            try { System.IO.File.Delete(fullPath); } catch { /* ignore */ }
        }

        if (cv.IsDefault)
        {
            var next = await _db.UserCvs.Where(c => c.UserId == user.Id && c.Id != cv.Id).OrderBy(c => c.UploadedAt).FirstOrDefaultAsync();
            if (next != null)
                next.IsDefault = true;
        }
        _db.UserCvs.Remove(cv);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa CV.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var cv = await _db.UserCvs.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        if (cv is null) return NotFound();

        foreach (var c in await _db.UserCvs.Where(c => c.UserId == user.Id).ToListAsync())
            c.IsDefault = false;
        cv.IsDefault = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã đặt CV mặc định.";
        return RedirectToAction(nameof(Index));
    }
}
