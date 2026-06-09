using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public class JobListingRepository : IJobListingRepository
{
    private readonly CareerHubDbContext _context;

    public JobListingRepository(CareerHubDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<JobResponse>> GetActiveListingsAsync()
    {
        var jobsData = await _context.JobListings
            .AsNoTracking()
            .Where(j => j.IsActive)
            .Select(j => new 
            {
                j.Id,
                j.Title,
                CompanyName = j.Company.Name,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                j.SalaryMin,
                j.SalaryMax,
                ApplicationCount = j.Applications.Count
            })
            .ToListAsync();

        return jobsData.Select(j => 
        {
            string salaryDisplay = "Salary not specified";
            if (j.SalaryMin.HasValue && j.SalaryMax.HasValue)
                salaryDisplay = $"R{j.SalaryMin:N0} - R{j.SalaryMax:N0}/month";
            else if (j.SalaryMin.HasValue)
                salaryDisplay = $"From R{j.SalaryMin:N0}/month";

            return new JobResponse(
                j.Id,
                j.Title,
                j.CompanyName,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                salaryDisplay,
                j.ApplicationCount
            );
        });
    }

    public async Task<IEnumerable<JobResponse>> SearchAsync(string searchTerm)
    {
        var jobsData = await _context.JobListings
            .AsNoTracking()
            .Where(j => j.IsActive && j.ClosingDate > DateTime.UtcNow && j.SearchVector!.Matches(EF.Functions.ToTsQuery("english", searchTerm)))
            .Select(j => new 
            {
                j.Id,
                j.Title,
                CompanyName = j.Company.Name,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                j.SalaryMin,
                j.SalaryMax,
                ApplicationCount = j.Applications.Count
            })
            .ToListAsync();

        return jobsData.Select(j => 
        {
            string salaryDisplay = "Salary not specified";
            if (j.SalaryMin.HasValue && j.SalaryMax.HasValue)
                salaryDisplay = $"R{j.SalaryMin:N0} - R{j.SalaryMax:N0}/month";
            else if (j.SalaryMin.HasValue)
                salaryDisplay = $"From R{j.SalaryMin:N0}/month";

            return new JobResponse(
                j.Id,
                j.Title,
                j.CompanyName,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                salaryDisplay,
                j.ApplicationCount
            );
        });
    }

    public async Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(Guid companyId)
    {
        return await _context.Database.SqlQuery<JobListingStatsResponse>($@"
            SELECT 
                jl.""Id"" AS ""JobListingId"",
                jl.""Title"",
                COUNT(a.""ApplicantId"")::int AS ""TotalApplications"",
                COUNT(a.""ApplicantId"") FILTER (WHERE a.""Status"" = 'Interviewing')::int AS ""InterviewingCount"",
                COUNT(a.""ApplicantId"") FILTER (WHERE a.""Status"" = 'Rejected')::int AS ""RejectedCount"",
                COUNT(a.""ApplicantId"") FILTER (WHERE a.""Status"" = 'Offered')::int AS ""OfferedCount"",
                RANK() OVER (ORDER BY COUNT(a.""ApplicantId"") DESC)::int AS ""Rank""
            FROM job_listings jl
            LEFT JOIN applications a ON jl.""Id"" = a.""JobListingId""
            WHERE jl.""CompanyId"" = {companyId}
            GROUP BY jl.""Id"", jl.""Title""
            ORDER BY ""Rank""
        ").ToListAsync();
    }

    public async Task<JobDetailResponse?> GetListingWithDetailsAsync(Guid id)
    {
        var jobData = await _context.JobListings
            .AsNoTracking()
            .Where(j => j.Id == id)
            .Select(j => new 
            {
                j.Id,
                j.Title,
                CompanyName = j.Company.Name,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                j.SalaryMin,
                j.SalaryMax,
                ApplicationCount = j.Applications.Count,
                Applications = j.Applications.Select(a => new ApplicationResponse(
                    a.Applicant.Name,
                    a.SubmittedAt,
                    a.Status
                )).ToList()
            })
            .FirstOrDefaultAsync();

        if (jobData == null) return null;

        string salaryDisplay = "Salary not specified";
        if (jobData.SalaryMin.HasValue && jobData.SalaryMax.HasValue)
            salaryDisplay = $"R{jobData.SalaryMin:N0} - R{jobData.SalaryMax:N0}/month";
        else if (jobData.SalaryMin.HasValue)
            salaryDisplay = $"From R{jobData.SalaryMin:N0}/month";

        return new JobDetailResponse(
            jobData.Id,
            jobData.Title,
            jobData.CompanyName,
            jobData.Location,
            jobData.Description,
            jobData.Type,
            jobData.PostedAt,
            salaryDisplay,
            jobData.ApplicationCount,
            jobData.Applications
        );
    }

    private static readonly Func<CareerHubDbContext, Guid, Task<JobListing?>> GetListingByIdCompiledQuery =
        EF.CompileAsyncQuery((CareerHubDbContext context, Guid id) =>
            context.JobListings.FirstOrDefault(j => j.Id == id));

    public async Task<JobListing?> GetListingByIdAsync(Guid id)
    {
        return await GetListingByIdCompiledQuery(_context, id);
    }

    public async Task<bool> IsOpenForApplicationsAsync(Guid id)
    {
        var listing = await _context.JobListings
            .AsNoTracking()
            .Select(j => new { j.Id, j.IsActive, j.ClosingDate })
            .FirstOrDefaultAsync(j => j.Id == id);
            
        if (listing == null) return false;
        
        return listing.IsActive && listing.ClosingDate > DateTime.UtcNow;
    }

    public async Task AddListingAsync(JobListing listing)
    {
        _context.JobListings.Add(listing);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateListingAsync(JobListing listing)
    {
        _context.JobListings.Update(listing);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteListingAsync(JobListing listing)
    {
        _context.JobListings.Remove(listing);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResponse<JobResponse>> GetActiveListingsPagedAsync(int page, int pageSize, JobListingFilterQuery filter)
    {
        var query = _context.JobListings
            .AsNoTracking()
            .Where(j => j.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Location))
            query = query.Where(j => EF.Functions.ILike(j.Location, $"%{filter.Location}%"));

        if (!string.IsNullOrWhiteSpace(filter.EmploymentType))
        {
            if (Enum.TryParse<JobType>(filter.EmploymentType, true, out var jobType))
            {
                query = query.Where(j => j.Type == jobType);
            }
        }

        if (filter.SalaryMin.HasValue)
            query = query.Where(j => j.SalaryMin >= filter.SalaryMin.Value);

        if (filter.SalaryMax.HasValue)
            query = query.Where(j => j.SalaryMax <= filter.SalaryMax.Value);

        if (filter.CompanyId.HasValue)
            query = query.Where(j => j.CompanyId == filter.CompanyId.Value);

        var totalCount = await query.CountAsync();

        bool isDesc = string.Equals(filter.Dir, "asc", StringComparison.OrdinalIgnoreCase) ? false : true;

        query = filter.Sort?.ToLowerInvariant() switch
        {
            "salarymin" => isDesc ? query.OrderByDescending(j => j.SalaryMin) : query.OrderBy(j => j.SalaryMin),
            "salarymax" => isDesc ? query.OrderByDescending(j => j.SalaryMax) : query.OrderBy(j => j.SalaryMax),
            "title" => isDesc ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
            _ => isDesc ? query.OrderByDescending(j => j.PostedAt) : query.OrderBy(j => j.PostedAt) // postedAt default
        };

        var jobsData = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new 
            {
                j.Id,
                j.Title,
                CompanyName = j.Company.Name,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                j.SalaryMin,
                j.SalaryMax,
                ApplicationCount = j.Applications.Count
            })
            .ToListAsync();

        var data = jobsData.Select(j => 
        {
            string salaryDisplay = "Salary not specified";
            if (j.SalaryMin.HasValue && j.SalaryMax.HasValue)
                salaryDisplay = $"R{j.SalaryMin:N0} - R{j.SalaryMax:N0}/month";
            else if (j.SalaryMin.HasValue)
                salaryDisplay = $"From R{j.SalaryMin:N0}/month";

            return new JobResponse(
                j.Id,
                j.Title,
                j.CompanyName,
                j.Location,
                j.Description,
                j.Type,
                j.PostedAt,
                salaryDisplay,
                j.ApplicationCount
            );
        });

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        return new PagedResponse<JobResponse>(
            Data: data,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            HasNextPage: page < totalPages,
            HasPreviousPage: page > 1
        );
    }

    public async Task<JobResponse?> PatchAsync(Guid id, UpdateJobListingRequest request)
    {
        var listing = await _context.JobListings.Include(j => j.Company).FirstOrDefaultAsync(j => j.Id == id);
        if (listing == null) return null;

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

        await _context.SaveChangesAsync();
        
        string salaryDisplay = "Salary not specified";
        if (listing.SalaryMin.HasValue && listing.SalaryMax.HasValue)
            salaryDisplay = $"R{listing.SalaryMin:N0} - R{listing.SalaryMax:N0}/month";
        else if (listing.SalaryMin.HasValue)
            salaryDisplay = $"From R{listing.SalaryMin:N0}/month";

        return new JobResponse(
            listing.Id,
            listing.Title,
            listing.Company.Name,
            listing.Location,
            listing.Description,
            listing.Type,
            listing.PostedAt,
            salaryDisplay,
            listing.Applications?.Count ?? 0
        );
    }
}
