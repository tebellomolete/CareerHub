namespace CareerHub.Api.Data;
using CareerHub.Api.Models;

public class JobStore
{
    private readonly List<JobListing> _jobs = new()
    {
        // Fixed the seed data to use JobType enums instead of strings
        new JobListing(Guid.NewGuid(), "Frontend Developer", "React/Next.js experience required.", "TechCorp", "Remote", JobType.FullTime),
        new JobListing(Guid.NewGuid(), "Backend Developer", ".NET 10 API dev.", "CareerHub", "Bloemfontein", JobType.Contract),
        new JobListing(Guid.NewGuid(), "UI/UX Designer", "Figma master needed.", "DesignStudio", "Cape Town", JobType.FullTime)
    };

    public Task<IEnumerable<JobListing>> GetAllJobsAsync() =>
        Task.FromResult<IEnumerable<JobListing>>(_jobs);

    public Task<JobListing?> GetJobByIdAsync(Guid id) =>
        Task.FromResult(_jobs.FirstOrDefault(j => j.Id == id));

    // New method for POST
    public Task AddJobAsync(JobListing job)
    {
        _jobs.Add(job);
        return Task.CompletedTask;
    }

    // New method for PUT
    public Task UpdateJobAsync(JobListing job)
    {
        var index = _jobs.FindIndex(j => j.Id == job.Id);
        if (index != -1)
        {
            _jobs[index] = job;
        }
        return Task.CompletedTask;
    }

    // New method for DELETE
    public Task DeleteJobAsync(Guid id)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == id);
        if (job != null)
        {
            _jobs.Remove(job);
        }
        return Task.CompletedTask;
    }
}