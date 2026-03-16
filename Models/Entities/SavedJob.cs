using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models.Entities;

public class SavedJob
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int JobId { get; set; }

    [Required]
    public int UserId { get; set; }

    public DateTime SavedDate { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("JobId")]
    public Job Job { get; set; } = null!;

    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
}
