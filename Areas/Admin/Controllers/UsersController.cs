using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? role = null, string? search = null, int page = 1)
    {
        const int pageSize = 20;

        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrEmpty(role) && role != "All")
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
        }

        var totalUsers = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Role = role ?? "All";
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalUsers == 0 ? 1 : (int)Math.Ceiling(totalUsers / (double)pageSize);
        ViewBag.TotalUsers = totalUsers;

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        await LoadUserDetailsViewBag(id, user);
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> DetailsPartial(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound("User not found.");

        await LoadUserDetailsViewBag(id, user);
        return PartialView("_UserDetailsPartial", user);
    }

    private async Task LoadUserDetailsViewBag(int id, ApplicationUser user)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == id);
        var applicationCount = await _db.Applications.CountAsync(a => a.UserId == id);
        var savedJobCount = await _db.SavedJobs.CountAsync(s => s.UserId == id);

        ViewBag.Company = company;
        ViewBag.ApplicationCount = applicationCount;
        ViewBag.SavedJobCount = savedJobCount;
        ViewBag.IsBanned = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ban(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        if (user.Role == "Admin")
        {
            TempData["Error"] = "Không thể khóa tài khoản Admin.";
            return RedirectToAction(nameof(Index));
        }

        var isBanned = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

        if (isBanned)
        {
            // Unban
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"Đã mở khóa tài khoản {user.FullName}.";
        }
        else
        {
            // Ban for 100 years
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            TempData["Success"] = $"Đã khóa tài khoản {user.FullName}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        if (user.Role == "Admin")
        {
            TempData["Error"] = "Không thể xóa tài khoản Admin.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = $"Đã xóa tài khoản {user.FullName}.";
        }
        else
        {
            TempData["Error"] = "Không thể xóa tài khoản. " + string.Join("; ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }
}
