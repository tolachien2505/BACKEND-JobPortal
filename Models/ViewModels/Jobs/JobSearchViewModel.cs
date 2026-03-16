using JobPortal.Models.Entities;
using PagedList.Core;

namespace JobPortal.Models.ViewModels.Jobs;

public class JobSearchViewModel
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public string? Location { get; set; }
    public string? JobType { get; set; }
    public string? ExperienceLevel { get; set; }
    public decimal? SalaryMin { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public IReadOnlyList<Category> Categories { get; set; } = [];
    public IPagedList<Job>? Results { get; set; }
}

