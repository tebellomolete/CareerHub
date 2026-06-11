using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;

namespace CareerHub.Api.Services;

public class JobListingService : IJobListingService
{
    private readonly IJobListingRepository _jobRepository;
    private readonly ICompanyRepository _companyRepository;

    public JobListingService(IJobListingRepository jobRepository, ICompanyRepository companyRepository)
    {
        _jobRepository = jobRepository;
        _companyRepository = companyRepository;
    }

    public async Task<IEnumerable<JobResponse>> GetActiveListingsAsync()
    {
        return await _jobRepository.GetActiveListingsAsync();
    }

    public async Task<IEnumerable<JobResponse>> SearchAsync(string searchTerm)
    {
        return await _jobRepository.SearchAsync(searchTerm);
    }

    public async Task<IEnumerable<JobListingStatsResponse>> GetCompanyStatsAsync(Guid companyId)
    {
        return await _jobRepository.GetApplicationStatsAsync(companyId);
    }

    public async Task<JobDetailResponse> GetListingWithDetailsAsync(Guid id)
    {
        var listing = await _jobRepository.GetListingWithDetailsAsync(id);
        if (listing == null) throw new JobNotFoundException(id);
        return listing;
    }

    public async Task<JobResponse> CreateListingAsync(CreateJobRequest request)
    {
        if (!await _companyRepository.ExistsAsync(request.CompanyId))
        {
            throw new CompanyNotFoundException(request.CompanyId);
        }

        if (request.ClosingDate <= DateTime.UtcNow)
        {
            throw new ArgumentException("Closing date must be in the future.");
        }

        if (request.SalaryMin.HasValue && request.SalaryMax.HasValue && request.SalaryMin.Value > request.SalaryMax.Value)
        {
            throw new ArgumentException("SalaryMax must be greater than or equal to SalaryMin");
        }

        var newJob = new JobListing
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            CompanyId = request.CompanyId,
            Location = request.Location,
            Type = request.Type,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            ClosingDate = request.ClosingDate,
            PostedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _jobRepository.AddListingAsync(newJob);

        // Fetch again to ensure Company name is loaded for response
        var detail = await _jobRepository.GetListingWithDetailsAsync(newJob.Id);
        
        return new JobResponse(
            detail!.Id,
            detail.Title,
            detail.CompanyName,
            detail.Location,
            detail.Description,
            detail.Type,
            detail.PostedAt,
            detail.SalaryDisplay,
            0
        );
    }

    public async Task<JobResponse> UpdateListingAsync(Guid id, UpdateJobRequest request)
    {
        var existingJob = await _jobRepository.GetListingByIdAsync(id);
        if (existingJob == null) throw new JobNotFoundException(id);

        if (existingJob.CompanyId != request.CompanyId)
        {
            throw new UnauthorizedListingUpdateException(id);
        }

        if (existingJob.ClosingDate <= DateTime.UtcNow || !existingJob.IsActive)
        {
            throw new ListingClosedException(id);
        }

        existingJob.Title = request.Title;
        existingJob.Description = request.Description;
        existingJob.Location = request.Location;
        existingJob.Type = request.Type;
        existingJob.SalaryMin = request.SalaryMin;
        existingJob.SalaryMax = request.SalaryMax;
        existingJob.ClosingDate = request.ClosingDate;

        await _jobRepository.UpdateListingAsync(existingJob);

        var detail = await _jobRepository.GetListingWithDetailsAsync(existingJob.Id);
        
        return new JobResponse(
            detail!.Id,
            detail.Title,
            detail.CompanyName,
            detail.Location,
            detail.Description,
            detail.Type,
            detail.PostedAt,
            detail.SalaryDisplay,
            detail.ApplicationCount
        );
    }

    public async Task DeleteListingAsync(Guid id)
    {
        var listing = await _jobRepository.GetListingByIdAsync(id);
        if (listing == null) throw new ArgumentException("Listing not found.");

        await _jobRepository.DeleteListingAsync(listing);
    }

    public async Task<PagedResponse<JobResponse>> GetActiveListingsPagedAsync(int page, int pageSize, JobListingFilterQuery filter)
    {
        return await _jobRepository.GetActiveListingsPagedAsync(page, pageSize, filter);
    }

    public async Task<JobResponse?> PatchAsync(Guid id, UpdateJobListingRequest request)
    {
        var listing = await _jobRepository.GetListingByIdAsync(id);
        if (listing == null) throw new JobNotFoundException(id);

        if (request.Title != null) listing.Title = request.Title;
        if (request.Description != null) listing.Description = request.Description;
        if (request.Location != null) listing.Location = request.Location;
        
        if (request.EmploymentType != null)
        {
            if (Enum.TryParse<JobType>(request.EmploymentType, true, out var parsedType))
            {
                listing.Type = parsedType;
            }
        }

        if (request.SalaryMin.HasValue || request.SalaryMax.HasValue)
        {
            var newMin = request.SalaryMin ?? listing.SalaryMin;
            var newMax = request.SalaryMax ?? listing.SalaryMax;
            
            if (newMin.HasValue && newMax.HasValue && newMin.Value > newMax.Value)
            {
                throw new ArgumentException("SalaryMax must be greater than or equal to SalaryMin");
            }
            
            if (request.SalaryMin.HasValue) listing.SalaryMin = request.SalaryMin.Value;
            if (request.SalaryMax.HasValue) listing.SalaryMax = request.SalaryMax.Value;
        }

        if (request.ExpiresAt.HasValue)
        {
            if (request.ExpiresAt.Value <= listing.PostedAt)
            {
                throw new ArgumentException("ClosingDate must be after PostedAt");
            }
            listing.ClosingDate = request.ExpiresAt.Value;
        }

        await _jobRepository.UpdateListingAsync(listing);
        
        var detail = await _jobRepository.GetListingWithDetailsAsync(listing.Id);
        
        return new JobResponse(
            detail!.Id,
            detail.Title,
            detail.CompanyName,
            detail.Location,
            detail.Description,
            detail.Type,
            detail.PostedAt,
            detail.SalaryDisplay,
            detail.ApplicationCount
        );
    }
}
