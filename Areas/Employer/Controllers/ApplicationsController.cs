using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Employer.Controllers;

[Area("Employer")]
[Authorize(Roles = "Employer")]
public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? jobId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        // Lấy Company của user hiện tại
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
        if (company is null)
        {
            TempData["Error"] = "Vui lòng cập nhật thông tin công ty trước.";
            return RedirectToAction("Edit", "Company");
        }

        // Lấy danh sách các đơn ứng tuyển nộp vào các Job của công ty này
        var query = _db.Applications
            .Include(a => a.Job)
            .Include(a => a.User)
            .Where(a => a.Job.CompanyId == company.Id);

        // Lọc theo JobId nếu có
        if (jobId.HasValue)
        {
            query = query.Where(a => a.JobId == jobId.Value);
            ViewBag.JobId = jobId.Value;
        }

        var applications = await query.OrderByDescending(a => a.AppliedDate).ToListAsync();

        // Lấy danh sách Jobs để làm filter dropdown
        ViewBag.CompanyJobs = await _db.Jobs
            .Where(j => j.CompanyId == company.Id)
            .OrderByDescending(j => j.PostedDate)
            .Select(j => new { j.Id, j.Title })
            .ToListAsync();

        return View(applications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == user.Id);
        if (company is null) return NotFound();

        var application = await _db.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == id && a.Job.CompanyId == company.Id);

        if (application is null)
        {
            return NotFound();
        }

        var validStatuses = new[] { "Pending", "Reviewing", "Interview", "Accepted", "Rejected" };
        if (validStatuses.Contains(status))
        {
            application.Status = status;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật trạng thái ứng viên thành công.";
        }

        return RedirectToAction(nameof(Index), new { jobId = application.JobId });
    }
}
