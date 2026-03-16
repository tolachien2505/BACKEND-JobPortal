using JobPortal.Models.Entities;
using JobPortal.Models.ViewModels.Candidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate")]
[Authorize(Roles = "Candidate")]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var vm = new ProfileEditViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Edit));
    }
}

