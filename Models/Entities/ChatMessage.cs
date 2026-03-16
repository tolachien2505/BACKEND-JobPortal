using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models.Entities;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ChatSessionId { get; set; }

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = "user"; // "user" or "assistant"

    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Stores the structured JSON response from Gemini AI (only for assistant messages).
    /// </summary>
    public string? JsonData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("ChatSessionId")]
    public ChatSession ChatSession { get; set; } = null!;
}
