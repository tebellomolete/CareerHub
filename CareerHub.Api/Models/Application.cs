namespace CareerHub.Api.Models;

public class Application
{
    public Guid ApplicantId { get; set; }
    public Applicant Applicant { get; set; } = null!;

    public Guid JobListingId { get; set; }
    public JobListing JobListing { get; set; } = null!;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
}
