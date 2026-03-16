using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Category());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid) return View(model);

        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();

        if (!ModelState.IsValid) return View(model);

        category.Name = model.Name;
        category.Description = model.Description;
        category.ParentId = model.ParentId;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();

        var hasJobs = await _db.Jobs.AnyAsync(j => j.CategoryId == id);
        if (hasJobs)
        {
            TempData["Error"] = "Không thể xóa danh mục vì đang có Jobs sử dụng.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa danh mục.";
        return RedirectToAction(nameof(Index));
    }
}

