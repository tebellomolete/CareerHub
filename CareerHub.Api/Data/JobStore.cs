namespace CareerHub.Api.Data;
using CareerHub.Api.Models;

public class JobStore
{
    private readonly List<JobListing> _jobs = new()
    {
        new JobListing(Guid.NewGuid(), "Frontend Developer", "React/Next.js experience required.", "TechCorp", "Remote", "Full-time"),
        new JobListing(Guid.NewGuid(), "Backend Developer", ".NET 10 API dev.", "CareerHub", "Bloemfontein", "Contract"),
        new JobListing(Guid.NewGuid(), "UI/UX Designer", "Figma master needed.", "DesignStudio", "Cape Town", "Full-time")
    };

    public Task<IEnumerable<JobListing>> GetAllJobsAsync() => 
        Task.FromResult<IEnumerable<JobListing>>(_jobs);

    public Task<JobListing?> GetJobByIdAsync(Guid id) => 
        Task.FromResult(_jobs.FirstOrDefault(j => j.Id == id));
}