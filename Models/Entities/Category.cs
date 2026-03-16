using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models.Entities;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public Category? Parent { get; set; }

    public ICollection<Category> SubCategories { get; set; } = new List<Category>();

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
