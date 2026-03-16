namespace JobPortal.Models.ViewModels.Home;

public class HomeIndexViewModel
{
    public List<Models.Entities.Category> Categories { get; set; } = new();
    public List<HomeFeaturedJobCardViewModel> FeaturedJobs { get; set; } = new();
}

public class HomeFeaturedJobCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CompanyLogo { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? Location { get; set; }
    public string? JobType { get; set; }
    public string? ExperienceLevel { get; set; }
    public bool IsFeatured { get; set; }
    public string? CategoryName { get; set; }
    public DateTime PostedDate { get; set; }
}
