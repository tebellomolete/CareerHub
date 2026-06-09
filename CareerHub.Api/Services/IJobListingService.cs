using CareerHub.Api.DTOs;
using CareerHub.Api.Models;

namespace CareerHub.Api.Services;

public interface IJobListingService
{
    Task<IEnumerable<JobResponse>> GetActiveListingsAsync();
    Task<PagedResponse<JobResponse>> GetActiveListingsPagedAsync(int page, int pageSize, JobListingFilterQuery filter);
    Task<IEnumerable<JobResponse>> SearchAsync(string searchTerm);
    Task<IEnumerable<JobListingStatsResponse>> GetCompanyStatsAsync(Guid companyId);
    Task<JobDetailResponse> GetListingWithDetailsAsync(Guid id);
    Task<JobResponse> CreateListingAsync(CreateJobRequest request);
    Task<JobResponse> UpdateListingAsync(Guid id, UpdateJobRequest request);
    Task DeleteListingAsync(Guid id);
    Task<JobResponse?> PatchAsync(Guid id, UpdateJobListingRequest request);
}
