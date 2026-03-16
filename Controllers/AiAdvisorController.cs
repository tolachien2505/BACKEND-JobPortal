using JobPortal.Data;
using JobPortal.Models.Entities;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Controllers;

[Authorize]
public class AiAdvisorController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GeminiService _geminiService;
    private readonly FileTextExtractor _fileExtractor;
    private readonly ILogger<AiAdvisorController> _logger;

    public AiAdvisorController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        GeminiService geminiService,
        FileTextExtractor fileExtractor,
        ILogger<AiAdvisorController> logger)
    {
        _db = db;
        _userManager = userManager;
        _geminiService = geminiService;
        _fileExtractor = fileExtractor;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest? request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new ChatResponse
                {
                    Success = false,
                    Message = "Vui long dang nhap."
                });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ChatResponse
                {
                    Success = false,
                    Message = "Vui long nhap noi dung can tu van."
                });
            }

            var session = await GetOrCreateChatSessionAsync(
                user.Id,
                request.SessionId,
                BuildSessionTitle(request.Message),
                includeMessages: true);

            var userMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "user",
                Content = request.Message
            };

            _db.ChatMessages.Add(userMessage);
            session.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            var history = await _db.ChatMessages
                .Where(m => m.ChatSessionId == session.Id && m.Id != userMessage.Id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatHistoryItem
                {
                    Role = m.Role,
                    Content = m.Content
                })
                .ToListAsync();

            var aiResponse = await _geminiService.AnalyzeCareerAsync(request.Message, history);
            if (!aiResponse.Success)
            {
                return Ok(new ChatResponse
                {
                    SessionId = session.Id,
                    Success = false,
                    Message = aiResponse.ErrorMessage ?? "Loi AI khong xac dinh."
                });
            }

            var assistantMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "assistant",
                Content = aiResponse.Data?.danh_gia_chung ?? string.Empty,
                JsonData = aiResponse.RawJson
            };

            _db.ChatMessages.Add(assistantMessage);
            session.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            var matchedJobs = new List<MatchedJob>();
            var keywords = aiResponse.Data?.tu_khoa_mo_rong?
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .Select(keyword => keyword.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (keywords?.Length > 0)
            {
                matchedJobs = await SearchJobsByKeywords(keywords);
                MergeMatchedJobs(aiResponse.Data?.goi_y_cong_viec, matchedJobs);
            }

            return Ok(new ChatResponse
            {
                SessionId = session.Id,
                Success = true,
                Data = aiResponse.Data,
                MatchedJobs = matchedJobs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI chat failed for session {SessionId}", request?.SessionId);
            return StatusCode(500, new ChatResponse
            {
                SessionId = request?.SessionId ?? 0,
                Success = false,
                Message = "Khong the xu ly tu van luc nay. Vui long thu lai."
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadCv(IFormFile file, int? sessionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { error = "Vui long dang nhap." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Vui long chon file CV." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf" && extension != ".docx")
        {
            return BadRequest(new { error = "Chi ho tro file PDF hoac DOCX." });
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { error = "File qua lon (toi da 10MB)." });
        }

        try
        {
            string cvText;
            using (var stream = file.OpenReadStream())
            {
                cvText = _fileExtractor.ExtractText(stream, file.FileName);
            }

            if (string.IsNullOrWhiteSpace(cvText))
            {
                return BadRequest(new { error = "Khong the trich xuat noi dung tu file CV." });
            }

            var session = await GetOrCreateChatSessionAsync(
                user.Id,
                sessionId,
                $"Phan tich CV: {file.FileName}");

            var prompt = $"Toi vua upload CV cua minh. Hay phan tich CV nay va tu van nghe nghiep cho toi:\n\n---CV CONTENT---\n{cvText}\n---END CV---";

            var userMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "user",
                Content = $"[Da tai len CV: {file.FileName}]"
            };

            _db.ChatMessages.Add(userMessage);
            session.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            var aiResponse = await _geminiService.AnalyzeCareerAsync(prompt);
            if (!aiResponse.Success)
            {
                return Ok(new ChatResponse
                {
                    SessionId = session.Id,
                    Success = false,
                    Message = aiResponse.ErrorMessage ?? "Loi phan tich CV."
                });
            }

            var assistantMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "assistant",
                Content = aiResponse.Data?.danh_gia_chung ?? string.Empty,
                JsonData = aiResponse.RawJson
            };

            _db.ChatMessages.Add(assistantMessage);
            session.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            var matchedJobs = new List<MatchedJob>();
            var keywords = aiResponse.Data?.tu_khoa_mo_rong?
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .Select(keyword => keyword.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (keywords?.Length > 0)
            {
                matchedJobs = await SearchJobsByKeywords(keywords);
                MergeMatchedJobs(aiResponse.Data?.goi_y_cong_viec, matchedJobs);
            }

            return Ok(new ChatResponse
            {
                SessionId = session.Id,
                Success = true,
                Data = aiResponse.Data,
                MatchedJobs = matchedJobs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CV upload");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> History(int sessionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { error = "Vui long dang nhap." });
        }

        var sessionExists = await _db.ChatSessions
            .AsNoTracking()
            .AnyAsync(s => s.Id == sessionId && s.UserId == user.Id);

        if (!sessionExists)
        {
            return NotFound(new { error = "Phien chat khong ton tai." });
        }

        var messages = await _db.ChatMessages
            .Where(m => m.ChatSession.UserId == user.Id && m.ChatSessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.Role,
                m.Content,
                m.JsonData,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpGet]
    public async Task<IActionResult> Sessions()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var sessions = await _db.ChatSessions
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.CreatedAt,
                s.UpdatedAt,
                MessageCount = s.Messages.Count
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet]
    public async Task<IActionResult> RoadmapPage()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account", new { area = "Client" });
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SaveRoadmap([FromBody] SaveRoadmapRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var roadmap = new UserRoadmap
        {
            UserId = user.Id,
            ChatSessionId = request.SessionId,
            DanhGiaChung = request.DanhGiaChung ?? string.Empty,
            KyNangCanBoSung = request.KyNangCanBoSung ?? "[]",
            TuKhoaMoRong = request.TuKhoaMoRong ?? "[]",
            GoiYCongViec = request.GoiYCongViec ?? "[]"
        };

        _db.UserRoadmaps.Add(roadmap);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, id = roadmap.Id, message = "Da luu lo trinh thanh cong!" });
    }

    [HttpGet]
    public async Task<IActionResult> Roadmaps()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var roadmaps = await _db.UserRoadmaps
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.DanhGiaChung,
                r.KyNangCanBoSung,
                r.TuKhoaMoRong,
                r.GoiYCongViec,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(roadmaps);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRoadmap(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var roadmap = await _db.UserRoadmaps
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

        if (roadmap == null)
        {
            return NotFound(new { success = false, message = "Khong tim thay lo trinh." });
        }

        _db.UserRoadmaps.Remove(roadmap);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private async Task<List<MatchedJob>> SearchJobsByKeywords(string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
        {
            return new List<MatchedJob>();
        }

        var keywordList = keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!keywordList.Any())
        {
            return new List<MatchedJob>();
        }

        var jobs = await _db.Jobs
            .AsNoTracking()
            .Include(j => j.Company)
            .Include(j => j.Category)
            .Where(j => j.IsActive && j.ModerationStatus == "Approved")
            .OrderByDescending(j => j.IsFeatured)
            .ThenByDescending(j => j.PostedDate)
            .ToListAsync();

        return jobs
            .Where(j => keywordList.Any(kw =>
                ContainsIgnoreCase(j.Title, kw) ||
                ContainsIgnoreCase(j.Description, kw) ||
                ContainsIgnoreCase(j.Requirements, kw) ||
                ContainsIgnoreCase(j.Category?.Name, kw)))
            .Select(j => new MatchedJob
            {
                Id = j.Id,
                Title = j.Title,
                CompanyName = j.Company.CompanyName,
                Location = j.Location ?? string.Empty,
                JobType = j.JobType ?? string.Empty,
                SalaryMin = j.SalaryMin,
                SalaryMax = j.SalaryMax,
                Url = $"/Jobs/Details/{j.Id}"
            })
            .Take(5)
            .ToList();
    }

    private async Task<ChatSession> GetOrCreateChatSessionAsync(
        int userId,
        int? sessionId,
        string title,
        bool includeMessages = false)
    {
        ChatSession? session = null;
        if (sessionId.HasValue)
        {
            IQueryable<ChatSession> query = _db.ChatSessions;
            if (includeMessages)
            {
                query = query.Include(s => s.Messages);
            }

            session = await query.FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.UserId == userId);
            if (session == null)
            {
                _logger.LogInformation(
                    "Chat session {SessionId} was not found for user {UserId}. Creating a new session.",
                    sessionId.Value,
                    userId);
            }
        }

        if (session != null)
        {
            return session;
        }

        session = new ChatSession
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? "Phien tu van moi" : title
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    private static string BuildSessionTitle(string message)
    {
        return message.Length > 50 ? message[..50] + "..." : message;
    }

    private static void MergeMatchedJobs(IEnumerable<JobSuggestion>? suggestions, IEnumerable<MatchedJob> matchedJobs)
    {
        if (suggestions == null)
        {
            return;
        }

        foreach (var suggestion in suggestions)
        {
            if (string.IsNullOrWhiteSpace(suggestion.chuc_danh))
            {
                continue;
            }

            var match = matchedJobs.FirstOrDefault(job =>
                ContainsIgnoreCase(job.Title, suggestion.chuc_danh) ||
                ContainsIgnoreCase(suggestion.chuc_danh, job.Title));

            if (match != null)
            {
                suggestion.job_url = match.Url;
                suggestion.job_id = match.Id;
            }
        }
    }

    private static bool ContainsIgnoreCase(string? source, string? value)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               !string.IsNullOrWhiteSpace(value) &&
               source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}

public class ChatRequest
{
    public int? SessionId { get; set; }
    public string? Message { get; set; }
}

public class ChatResponse
{
    public int SessionId { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public AiCareerAdvice? Data { get; set; }
    public List<MatchedJob>? MatchedJobs { get; set; }
}

public class MatchedJob
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class SaveRoadmapRequest
{
    public int? SessionId { get; set; }
    public string? DanhGiaChung { get; set; }
    public string? KyNangCanBoSung { get; set; }
    public string? TuKhoaMoRong { get; set; }
    public string? GoiYCongViec { get; set; }
}
