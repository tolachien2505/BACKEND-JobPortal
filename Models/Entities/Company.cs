using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models.Entities;

public class Company
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Logo { get; set; }

    [StringLength(200)]
    public string? Website { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? Industry { get; set; }

    [StringLength(50)]
    public string? CompanySize { get; set; } // Small, Medium, Large

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
