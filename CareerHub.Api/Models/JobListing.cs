namespace CareerHub.Api.Models;

public class JobListing
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public JobType Type { get; set; }
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public ICollection<Application> Applications { get; set; } = new List<Application>();

    // Server-owned fields initialized automatically upon creation
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}