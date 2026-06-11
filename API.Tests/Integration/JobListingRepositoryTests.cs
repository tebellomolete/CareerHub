using System;
using System.Linq;
using System.Threading.Tasks;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.Tests.Integration;

[Collection("Database collection")]
public class JobListingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private readonly CareerHub.Api.Data.CareerHubDbContext _context;
    private readonly JobListingRepository _repository;

    public JobListingRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
        _context = _fixture.CreateContext();
        _repository = new JobListingRepository(_context);
    }

    public async Task InitializeAsync()
    {
        // Clear data before each test
        await _context.Applications.ExecuteDeleteAsync();
        await _context.Applicants.ExecuteDeleteAsync();
        await _context.JobListings.ExecuteDeleteAsync();
        await _context.Companies.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task AddListingAsync_ShouldPersistToDatabase()
    {
        // Arrange
        var company = new Company { Id = Guid.NewGuid(), Name = "Tech Corp" };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var job = new JobListing
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Title = "Software Engineer",
            Description = "A great job",
            Location = "Remote",
            Type = JobType.FullTime,
            PostedAt = DateTime.UtcNow,
            ClosingDate = DateTime.UtcNow.AddDays(30),
            SalaryMin = 50000,
            SalaryMax = 100000
        };

        // Act
        await _repository.AddListingAsync(job);

        // Assert
        var savedJob = await _context.JobListings.FirstOrDefaultAsync(j => j.Id == job.Id);
        Assert.NotNull(savedJob);
        Assert.Equal("Software Engineer", savedJob.Title);
    }

    [Fact]
    public async Task GetListingsAsync_ShouldApplyPaginationCorrectly()
    {
        // Arrange
        var company = new Company { Id = Guid.NewGuid(), Name = "Tech Corp" };
        _context.Companies.Add(company);
        
        for (int i = 0; i < 10; i++)
        {
            _context.JobListings.Add(new JobListing
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Title = $"Job {i}",
                Description = "A great job",
                Location = "Remote",
                Type = JobType.FullTime,
                PostedAt = DateTime.UtcNow,
                ClosingDate = DateTime.UtcNow.AddDays(30),
                SalaryMin = 50000,
                SalaryMax = 100000
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var resultPage1 = await _repository.GetActiveListingsPagedAsync(1, 4, new CareerHub.Api.DTOs.JobListingFilterQuery());
        var resultPage2 = await _repository.GetActiveListingsPagedAsync(2, 4, new CareerHub.Api.DTOs.JobListingFilterQuery());

        // Assert
        Assert.Equal(10, resultPage1.TotalCount);
        Assert.Equal(4, resultPage1.Data.Count());

        Assert.Equal(10, resultPage2.TotalCount);
        Assert.Equal(4, resultPage2.Data.Count());
        
        var titlesPage1 = resultPage1.Data.Select(d => d.Title).ToList();
        var titlesPage2 = resultPage2.Data.Select(d => d.Title).ToList();
        
        Assert.Empty(titlesPage1.Intersect(titlesPage2));
    }

    [Fact]
    public async Task GetListingsAsync_ShouldFilterByEmploymentType()
    {
        // Arrange
        var company = new Company { Id = Guid.NewGuid(), Name = "Tech Corp" };
        _context.Companies.Add(company);

        _context.JobListings.Add(new JobListing
        {
            Id = Guid.NewGuid(), CompanyId = company.Id, Title = "FT Job", Description = "...", Location = "...", 
            Type = JobType.FullTime, PostedAt = DateTime.UtcNow, ClosingDate = DateTime.UtcNow.AddDays(30), SalaryMin = 50000, SalaryMax = 100000
        });
        
        _context.JobListings.Add(new JobListing
        {
            Id = Guid.NewGuid(), CompanyId = company.Id, Title = "PT Job", Description = "...", Location = "...", 
            Type = JobType.PartTime, PostedAt = DateTime.UtcNow, ClosingDate = DateTime.UtcNow.AddDays(30), SalaryMin = 50000, SalaryMax = 100000
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveListingsPagedAsync(1, 10, new CareerHub.Api.DTOs.JobListingFilterQuery { EmploymentType = "FullTime" });

        // Assert
        Assert.Single(result.Data);
        Assert.Equal(JobType.FullTime, result.Data.First().Type);
    }
    [Fact]
    public async Task SearchAsync_WhenSearchingForDeveloper_ReturnsMatchingJobs()
    {
        // Arrange
        var company = new Company { Id = Guid.NewGuid(), Name = "Tech Corp" };
        _context.Companies.Add(company);

        _context.JobListings.Add(new JobListing
        {
            Id = Guid.NewGuid(), CompanyId = company.Id, Title = "Senior Developer", Description = "Looking for a dev", Location = "NY", 
            Type = JobType.FullTime, PostedAt = DateTime.UtcNow, ClosingDate = DateTime.UtcNow.AddDays(30), SalaryMin = 50000, SalaryMax = 100000
        });
        
        _context.JobListings.Add(new JobListing
        {
            Id = Guid.NewGuid(), CompanyId = company.Id, Title = "Accountant", Description = "Numbers", Location = "NY", 
            Type = JobType.FullTime, PostedAt = DateTime.UtcNow, ClosingDate = DateTime.UtcNow.AddDays(30), SalaryMin = 50000, SalaryMax = 100000
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("Developer");

        // Assert
        Assert.Single(result);
        Assert.Contains(result, r => r.Title == "Senior Developer");
    }

    [Fact]
    public async Task JobListing_WhenSalaryMinIsGreaterThanSalaryMax_ThrowsDbUpdateException()
    {
        // Arrange
        var company = new Company { Id = Guid.NewGuid(), Name = "Tech Corp" };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var job = new JobListing
        {
            Id = Guid.NewGuid(), CompanyId = company.Id, Title = "FT Job", Description = "...", Location = "...", 
            Type = JobType.FullTime, PostedAt = DateTime.UtcNow, ClosingDate = DateTime.UtcNow.AddDays(30),
            SalaryMin = 100000, SalaryMax = 50000
        };

        _context.JobListings.Add(job);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }
}
