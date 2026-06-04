namespace CareerHub.Api.Models;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    public ICollection<JobListing> JobListings { get; set; } = new List<JobListing>();
}
