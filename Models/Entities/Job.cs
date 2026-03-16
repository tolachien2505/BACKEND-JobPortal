using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models.Entities;

public class Job
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Requirements { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalaryMin { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalaryMax { get; set; }

    [StringLength(50)]
    public string? JobType { get; set; } // Full-time, Part-time, Internship, Contract

    [StringLength(50)]
    public string? ExperienceLevel { get; set; } // Fresher, Junior, Mid, Senior

    public int? Vacancies { get; set; }

    public DateTime PostedDate { get; set; } = DateTime.Now;

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFeatured { get; set; } = false;

    [Required]
    [StringLength(20)]
    public string ModerationStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

    public int? ModeratedByUserId { get; set; }

    public DateTime? ModeratedAt { get; set; }

    [StringLength(500)]
    public string? ModerationNote { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public Company Company { get; set; } = null!;

    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
}
