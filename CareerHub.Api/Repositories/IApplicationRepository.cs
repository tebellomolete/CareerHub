using CareerHub.Api.Models;

namespace CareerHub.Api.Repositories;

public interface IApplicationRepository
{
    Task<bool> HasApplicantAppliedAsync(Guid applicantId, Guid listingId);
    Task<IEnumerable<Application>> GetApplicationsForListingAsync(Guid listingId);
    Task<IEnumerable<Application>> GetApplicationsByApplicantAsync(Guid applicantId);
    Task<Application?> GetApplicationAsync(Guid applicantId, Guid listingId);
    Task AddApplicationAsync(Application application);
    Task UpdateApplicationStatusAsync(Application application);
    Task DeleteApplicationAsync(Application application);
}
