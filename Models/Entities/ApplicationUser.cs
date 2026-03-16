using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace JobPortal.Models.Entities;

public class ApplicationUser : IdentityUser<int>
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = "Candidate"; // Candidate, Employer, Admin

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public Company? Company { get; set; }
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    public ICollection<UserCv> UserCvs { get; set; } = new List<UserCv>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<UserRoadmap> UserRoadmaps { get; set; } = new List<UserRoadmap>();
}
