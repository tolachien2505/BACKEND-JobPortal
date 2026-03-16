using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models.Entities;

public class Application
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int JobId { get; set; }

    [Required]
    public int UserId { get; set; }

    public string? CoverLetter { get; set; }

    [StringLength(255)]
    public string? ResumePath { get; set; }

    public DateTime AppliedDate { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Reviewing, Interview, Rejected, Accepted

    public DateTime? InterviewDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("JobId")]
    public Job Job { get; set; } = null!;

    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
}
