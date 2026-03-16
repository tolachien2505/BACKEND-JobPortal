using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models.Entities;

public class UserRoadmap
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? ChatSessionId { get; set; }

    [Required]
    public string DanhGiaChung { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of skill gaps: [{ten_ky_nang, ly_do, goi_y_khoa_hoc}]
    /// </summary>
    [Required]
    public string KyNangCanBoSung { get; set; } = "[]";

    /// <summary>
    /// JSON array of expanded keywords: ["keyword1", "keyword2", ...]
    /// </summary>
    [Required]
    public string TuKhoaMoRong { get; set; } = "[]";

    /// <summary>
    /// JSON array of job suggestions: [{chuc_danh, muc_luong_du_kien, ly_do_phu_hop, job_url}]
    /// </summary>
    [Required]
    public string GoiYCongViec { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;

    [ForeignKey("ChatSessionId")]
    public ChatSession? ChatSession { get; set; }
}
