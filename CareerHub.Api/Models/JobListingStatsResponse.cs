namespace CareerHub.Api.Models;

public class JobListingStatsResponse
{
    public Guid JobListingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalApplications { get; set; }
    public int InterviewingCount { get; set; }
    public int RejectedCount { get; set; }
    public int OfferedCount { get; set; }
    public int Rank { get; set; }
}
