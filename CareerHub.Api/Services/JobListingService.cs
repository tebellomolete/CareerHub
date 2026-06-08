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
        var existingJob = await _jobRepository.GetListingByIdAsync(id);
        if (existingJob == null) throw new JobNotFoundException(id);

        await _jobRepository.DeleteListingAsync(existingJob);
    }
}
