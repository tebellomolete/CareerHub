using CareerHub.Api.Data;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly CareerHubDbContext _context;

    public ApplicationRepository(CareerHubDbContext context)
    {
        _context = context;
    }

    private static readonly Func<CareerHubDbContext, Guid, Guid, Task<bool>> HasAppliedCompiledQuery =
        EF.CompileAsyncQuery((CareerHubDbContext context, Guid applicantId, Guid listingId) =>
            context.Applications.Any(a => a.ApplicantId == applicantId && a.JobListingId == listingId));

    public async Task<bool> HasApplicantAppliedAsync(Guid applicantId, Guid listingId)
    {
        return await HasAppliedCompiledQuery(_context, applicantId, listingId);
    }

    public async Task<IEnumerable<Application>> GetApplicationsForListingAsync(Guid listingId)
    {
        return await _context.Applications
            .Include(a => a.Applicant)
            .Where(a => a.JobListingId == listingId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Application>> GetApplicationsByApplicantAsync(Guid applicantId)
    {
        return await _context.Applications
            .Include(a => a.JobListing)
            .Where(a => a.ApplicantId == applicantId)
            .ToListAsync();
    }

    public async Task<Application?> GetApplicationAsync(Guid applicantId, Guid listingId)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.ApplicantId == applicantId && a.JobListingId == listingId);
    }

    public async Task AddApplicationAsync(Application application)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateApplicationStatusAsync(Application application)
    {
        _context.Applications.Update(application);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteApplicationAsync(Application application)
    {
        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();
    }
}
