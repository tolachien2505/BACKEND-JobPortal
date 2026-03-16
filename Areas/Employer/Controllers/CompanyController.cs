using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Employer.Controllers;

[Area("Employer")]
[Authorize(Roles = "Employer")]
public class CompanyController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CompanyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
        company ??= new Company { UserId = user.Id, CompanyName = "" };

        return View(company);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Company model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        ModelState.Remove("User");
        ModelState.Remove("UserId");
        ModelState.Remove("Jobs");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
        if (company is null)
        {
            company = new Company { UserId = user.Id };
            _db.Companies.Add(company);
        }

        company.CompanyName = model.CompanyName;
        company.Description = model.Description;
        company.Website = model.Website;
        company.Address = model.Address;
        company.Industry = model.Industry;
        company.CompanySize = model.CompanySize;
        company.Logo = model.Logo;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu thông tin công ty.";
        return RedirectToAction(nameof(Edit));
    }
}

