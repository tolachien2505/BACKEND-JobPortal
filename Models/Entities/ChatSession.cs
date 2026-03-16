using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models.Entities;

public class ChatSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = "Phiên tư vấn mới";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ICollection<UserRoadmap> Roadmaps { get; set; } = new List<UserRoadmap>();
}
