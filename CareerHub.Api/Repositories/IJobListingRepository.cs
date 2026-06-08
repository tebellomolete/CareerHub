using CareerHub.Api.DTOs;
using CareerHub.Api.Models;

namespace CareerHub.Api.Repositories;

public interface IJobListingRepository
{
    Task<IEnumerable<JobResponse>> GetActiveListingsAsync();
    Task<JobDetailResponse?> GetListingWithDetailsAsync(Guid id);
    Task<JobListing?> GetListingByIdAsync(Guid id);
    Task<bool> IsOpenForApplicationsAsync(Guid id);
    Task AddListingAsync(JobListing listing);
    Task UpdateListingAsync(JobListing listing);
    Task DeleteListingAsync(JobListing listing);
}
