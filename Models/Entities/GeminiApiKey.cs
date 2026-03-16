using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models.Entities;

public class GeminiApiKey
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string ApiKey { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Label { get; set; } // e.g. "Key chính", "Key dự phòng 1"

    [StringLength(50)]
    public string Model { get; set; } = "gemini-1.5-flash";

    public bool IsActive { get; set; } = true;

    public int UsageCount { get; set; } = 0;

    public int? DailyLimit { get; set; } // null = unlimited

    public DateTime? LastUsedAt { get; set; }

    public DateTime? LastErrorAt { get; set; }

    [StringLength(500)]
    public string? LastErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Priority order — lower number = higher priority
    /// </summary>
    public int Priority { get; set; } = 0;
}
