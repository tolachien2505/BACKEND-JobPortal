using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using JobPortal.Models.Entities;

namespace JobPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class GeminiKeysController : Controller
{
    private readonly ApplicationDbContext _db;

    public GeminiKeysController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var keys = await _db.GeminiApiKeys
            .OrderBy(k => k.Priority)
            .ToListAsync();
        return View(keys);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string apiKey, string? label, string model, int? dailyLimit, int priority)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            TempData["Error"] = "API Key không được để trống";
            return RedirectToAction(nameof(Index));
        }

        var entity = new GeminiApiKey
        {
            ApiKey = apiKey.Trim(),
            Label = label?.Trim(),
            Model = string.IsNullOrWhiteSpace(model) ? "gemini-1.5-flash" : model.Trim(),
            DailyLimit = dailyLimit,
            Priority = priority,
            IsActive = true
        };

        _db.GeminiApiKeys.Add(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã thêm API Key thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var key = await _db.GeminiApiKeys.FindAsync(id);
        if (key == null) return NotFound();

        key.IsActive = !key.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = key.IsActive ? "Đã kích hoạt API Key" : "Đã tắt API Key";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var key = await _db.GeminiApiKeys.FindAsync(id);
        if (key == null) return NotFound();

        _db.GeminiApiKeys.Remove(key);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa API Key";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetUsage(int id)
    {
        var key = await _db.GeminiApiKeys.FindAsync(id);
        if (key == null) return NotFound();

        key.UsageCount = 0;
        key.LastErrorAt = null;
        key.LastErrorMessage = null;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã reset bộ đếm sử dụng";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAllUsage()
    {
        var keys = await _db.GeminiApiKeys.ToListAsync();
        foreach (var key in keys)
        {
            key.UsageCount = 0;
            key.LastErrorAt = null;
            key.LastErrorMessage = null;
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã reset tất cả bộ đếm sử dụng";
        return RedirectToAction(nameof(Index));
    }
}
