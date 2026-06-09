using CareerHub.Api.DTOs;
using CareerHub.Api.Models;

namespace CareerHub.Api.Repositories;

public interface IJobListingRepository
{
    Task<IEnumerable<JobResponse>> GetActiveListingsAsync();
    Task<PagedResponse<JobResponse>> GetActiveListingsPagedAsync(int page, int pageSize, JobListingFilterQuery filter);
    Task<IEnumerable<JobResponse>> SearchAsync(string searchTerm);
    Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(Guid companyId);
    Task<JobDetailResponse?> GetListingWithDetailsAsync(Guid id);
    Task<JobListing?> GetListingByIdAsync(Guid id);
    Task<bool> IsOpenForApplicationsAsync(Guid id);
    Task AddListingAsync(JobListing listing);
    Task UpdateListingAsync(JobListing listing);
    Task DeleteListingAsync(JobListing listing);
    Task<JobResponse?> PatchAsync(Guid id, UpdateJobListingRequest request);
}
