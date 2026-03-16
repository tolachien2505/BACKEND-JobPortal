namespace JobPortal.Models.ViewModels.Admin;

public sealed class DashboardStatusItemViewModel
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
    public double Percentage { get; init; }
    public string Color { get; init; } = "#94a3b8";
    public string SoftColor { get; init; } = "#f8fafc";
    public string IconClass { get; init; } = "fas fa-circle";
}

public sealed class AdminDashboardViewModel
{
    public int TotalJobs { get; init; }
    public int PendingJobs { get; init; }
    public int ApprovedJobs { get; init; }
    public int RejectedJobs { get; init; }
    public int TotalUsers { get; init; }
    public int TotalCompanies { get; init; }
    public int TotalApplications { get; init; }
    public int TotalCategories { get; init; }
    public int PipelineApplications { get; init; }
    public int CompletedApplications { get; init; }
    public double PipelinePercentage { get; init; }
    public double CompletionPercentage { get; init; }
    public double InterviewRate { get; init; }
    public double AcceptanceRate { get; init; }
    public IReadOnlyList<DashboardStatusItemViewModel> JobModerationStats { get; init; } = Array.Empty<DashboardStatusItemViewModel>();
    public IReadOnlyList<DashboardStatusItemViewModel> ApplicationStats { get; init; } = Array.Empty<DashboardStatusItemViewModel>();
}
