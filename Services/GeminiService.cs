using System.Text;
using System.Text.Json;
using JobPortal.Data;
using JobPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;

    private const string SystemPrompt = """
Ban la Co van Nghe nghiep AI tren nen tang tuyen dung JobPortal.

Yeu cau:
- Tra loi bang tieng Viet, gon gang, chuyen nghiep.
- Neu chua du thong tin thi chi hoi them 1 cau.
- Luon tra ve JSON dung cau truc duoi day.
- Neu chua du thong tin thi de cac mang rong [].

{
  "danh_gia_chung": "Noi dung danh gia hoac cau hoi tiep theo.",
  "ky_nang_can_bo_sung": [
    {
      "ten_ky_nang": "Ten ky nang",
      "ly_do": "Ly do can hoc",
      "goi_y_khoa_hoc": "Goi y khoa hoc hoac nguon hoc"
    }
  ],
  "tu_khoa_mo_rong": ["tu khoa 1", "tu khoa 2"],
  "goi_y_cong_viec": [
    {
      "chuc_danh": "Ten vi tri",
      "muc_luong_du_kien": "Khoang luong",
      "ly_do_phu_hop": "Ly do phu hop"
    }
  ]
}
""";

    private static readonly string[] FallbackModels =
    {
        "gemini-2.5-flash",
        "gemini-2.5-flash-lite",
        "gemini-2.0-flash",
        "gemini-2.5-pro"
    };

    public GeminiService(
        HttpClient httpClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<(string ApiKey, string Model, int? KeyId)> GetApiKeyAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cutoff = DateTime.Now.AddMinutes(-5);
        var dbKey = await db.GeminiApiKeys
            .Where(k => k.IsActive)
            .Where(k => k.LastErrorAt == null || k.LastErrorAt < cutoff)
            .Where(k => k.DailyLimit == null || k.UsageCount < k.DailyLimit)
            .OrderBy(k => k.Priority)
            .ThenBy(k => k.UsageCount)
            .FirstOrDefaultAsync();

        if (dbKey != null)
        {
            return (dbKey.ApiKey, dbKey.Model, dbKey.Id);
        }

        var fallbackKey = _configuration["GeminiAI:ApiKey"];
        var fallbackModel = _configuration["GeminiAI:Model"] ?? "gemini-2.0-flash";

        if (!string.IsNullOrWhiteSpace(fallbackKey))
        {
            return (fallbackKey, fallbackModel, null);
        }

        throw new InvalidOperationException("Khong co API key Gemini kha dung.");
    }

    private async Task RecordUsageAsync(int? keyId)
    {
        if (keyId == null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var key = await db.GeminiApiKeys.FindAsync(keyId.Value);
        if (key == null)
        {
            return;
        }

        key.UsageCount++;
        key.LastUsedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    private async Task RecordErrorAsync(int? keyId, string errorMessage)
    {
        if (keyId == null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var key = await db.GeminiApiKeys.FindAsync(keyId.Value);
        if (key == null)
        {
            return;
        }

        key.LastErrorAt = DateTime.Now;
        key.LastErrorMessage = errorMessage.Length > 500 ? errorMessage[..500] : errorMessage;
        await db.SaveChangesAsync();
    }

    public async Task<GeminiResponse> AnalyzeCareerAsync(string userMessage, List<ChatHistoryItem>? chatHistory = null)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            string apiKey;
            string model;
            int? keyId;

            try
            {
                (apiKey, model, keyId) = await GetApiKeyAsync();
            }
            catch (InvalidOperationException)
            {
                break;
            }

            try
            {
                var result = await CallGeminiAsync(userMessage, chatHistory, apiKey, model);
                if (result.Success)
                {
                    await RecordUsageAsync(keyId);
                    return result;
                }

                await RecordErrorAsync(keyId, result.ErrorMessage ?? "Unknown error");
                _logger.LogWarning(
                    "Gemini key {KeyId} model {Model} failed on attempt {Attempt}: {Error}",
                    keyId,
                    model,
                    attempt + 1,
                    result.ErrorMessage);
            }
            catch (Exception ex)
            {
                await RecordErrorAsync(keyId, ex.Message);
                _logger.LogWarning(ex, "Gemini key {KeyId} threw an exception on attempt {Attempt}", keyId, attempt + 1);
            }
        }

        var fallbackKey = _configuration["GeminiAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(fallbackKey))
        {
            foreach (var model in FallbackModels)
            {
                try
                {
                    _logger.LogInformation("Trying fallback Gemini model {Model}", model);
                    var result = await CallGeminiAsync(userMessage, chatHistory, fallbackKey, model);
                    if (result.Success)
                    {
                        return result;
                    }

                    _logger.LogWarning("Fallback Gemini model {Model} failed: {Error}", model, result.ErrorMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fallback Gemini model {Model} threw an exception", model);
                }
            }
        }

        return new GeminiResponse
        {
            Success = false,
            ErrorMessage = "Tat ca model AI dang ban. Vui long thu lai sau it phut."
        };
    }

    private async Task<GeminiResponse> CallGeminiAsync(
        string userMessage,
        List<ChatHistoryItem>? chatHistory,
        string apiKey,
        string model)
    {
        var contents = new List<object>();

        if (chatHistory != null)
        {
            foreach (var item in chatHistory)
            {
                contents.Add(new
                {
                    role = item.Role == "assistant" ? "model" : "user",
                    parts = new[] { new { text = item.Content } }
                });
            }
        }

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = userMessage } }
        });

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = SystemPrompt } }
            },
            contents,
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.7,
                maxOutputTokens = 4096
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error for model {Model}: {StatusCode} - {Body}", model, response.StatusCode, responseBody);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return new GeminiResponse
                {
                    Success = false,
                    ErrorMessage = $"Model {model} het quota."
                };
            }

            return new GeminiResponse
            {
                Success = false,
                ErrorMessage = $"Loi dich vu AI ({(int)response.StatusCode})."
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (!TryGetTextContent(doc.RootElement, out var textContent, out var errorMessage))
            {
                return new GeminiResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            var normalizedJson = NormalizeJsonPayload(textContent);
            var aiResult = DeserializeAdvice(normalizedJson);
            if (aiResult == null)
            {
                _logger.LogWarning("Gemini returned an invalid JSON payload: {Payload}", normalizedJson);
                return new GeminiResponse
                {
                    Success = false,
                    ErrorMessage = "AI tra ve du lieu khong dung dinh dang."
                };
            }

            return new GeminiResponse
            {
                Success = true,
                Data = aiResult,
                RawJson = normalizedJson
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Unable to parse Gemini response body: {Body}", responseBody);
            return new GeminiResponse
            {
                Success = false,
                ErrorMessage = "Khong the doc phan hoi tu dich vu AI."
            };
        }
    }

    private static bool TryGetTextContent(JsonElement root, out string textContent, out string errorMessage)
    {
        textContent = string.Empty;
        errorMessage = "AI khong tra ve noi dung.";

        if (root.TryGetProperty("candidates", out var candidates) &&
            candidates.ValueKind == JsonValueKind.Array)
        {
            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (!part.TryGetProperty("text", out var textElement))
                    {
                        continue;
                    }

                    var value = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        textContent = value;
                        errorMessage = string.Empty;
                        return true;
                    }
                }
            }
        }

        if (root.TryGetProperty("promptFeedback", out var promptFeedback) &&
            promptFeedback.TryGetProperty("blockReason", out var blockReason))
        {
            errorMessage = $"Yeu cau bi AI tu choi ({blockReason.GetString()}).";
        }

        return false;
    }

    private static string NormalizeJsonPayload(string textContent)
    {
        var normalized = textContent.Trim();

        if (normalized.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = normalized.IndexOf('\n');
            if (firstNewLine >= 0)
            {
                normalized = normalized[(firstNewLine + 1)..];
            }

            var lastFence = normalized.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
            {
                normalized = normalized[..lastFence];
            }
        }

        return normalized.Trim();
    }

    private static AiCareerAdvice? DeserializeAdvice(string textContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            return JsonSerializer.Deserialize<AiCareerAdvice>(textContent, options);
        }
        catch (JsonException)
        {
            var start = textContent.IndexOf('{');
            var end = textContent.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            return JsonSerializer.Deserialize<AiCareerAdvice>(textContent[start..(end + 1)], options);
        }
    }
}

public class ChatHistoryItem
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class GeminiResponse
{
    public bool Success { get; set; }
    public AiCareerAdvice? Data { get; set; }
    public string? RawJson { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AiCareerAdvice
{
    public string danh_gia_chung { get; set; } = string.Empty;
    public List<SkillGap> ky_nang_can_bo_sung { get; set; } = new();
    public List<string> tu_khoa_mo_rong { get; set; } = new();
    public List<JobSuggestion> goi_y_cong_viec { get; set; } = new();
}

public class SkillGap
{
    public string ten_ky_nang { get; set; } = string.Empty;
    public string ly_do { get; set; } = string.Empty;
    public string goi_y_khoa_hoc { get; set; } = string.Empty;
}

public class JobSuggestion
{
    public string chuc_danh { get; set; } = string.Empty;
    public string muc_luong_du_kien { get; set; } = string.Empty;
    public string ly_do_phu_hop { get; set; } = string.Empty;
    public string? job_url { get; set; }
    public int? job_id { get; set; }
}
