using JobPortal.Models.Entities;
using JobPortal.Models.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.Areas.Client.Controllers;

[Area("Client")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Only allow Candidate or Employer
        var role = model.Role == "Employer" ? "Employer" : "Candidate";

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Role = role
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, role);
        await _signInManager.SignInAsync(user, isPersistent: false);

        if (role == "Employer")
        {
            return RedirectToAction("Edit", "Company", new { area = "Employer" });
        }
        
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        // Check if user is banned
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            if (await _userManager.IsInRoleAsync(user, "Employer"))
            {
                return RedirectToAction("Index", "Jobs", new { area = "Employer" });
            }
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "" });
    }
}
