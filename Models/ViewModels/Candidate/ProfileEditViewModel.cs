using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models.ViewModels.Candidate;

public class ProfileEditViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}

