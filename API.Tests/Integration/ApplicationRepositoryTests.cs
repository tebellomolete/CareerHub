using System;
using System.Threading.Tasks;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using CareerHub.Api.Repositories;
using Xunit;

namespace API.Tests.Integration;

[Collection("Database collection")]
public class ApplicationRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private readonly CareerHub.Api.Data.CareerHubDbContext _context;
    private readonly ApplicationRepository _repository;

    public ApplicationRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
        _context = _fixture.CreateContext();
        _repository = new ApplicationRepository(_context);
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
    public async Task HasApplicantAppliedAsync_WhenApplicationExists_ReturnsTrue()
    {
        // Arrange
        var company = new Company { Id = Guid.NewGuid(), Name = "Tech Corp" };
        _context.Companies.Add(company);

        var job = new JobListing
        {
            Id = Guid.NewGuid(), CompanyId = company.Id, Title = "FT Job", Description = "...", Location = "...", 
            Type = JobType.FullTime, PostedAt = DateTime.UtcNow, ClosingDate = DateTime.UtcNow.AddDays(30),
            SalaryMin = 50000, SalaryMax = 100000
        };
        _context.JobListings.Add(job);

        var applicant = new Applicant { Id = Guid.NewGuid(), Name = "John Doe", Email = "john@example.com" };
        _context.Applicants.Add(applicant);

        var application = new Application { ApplicantId = applicant.Id, JobListingId = job.Id, Status = ApplicationStatus.Submitted, SubmittedAt = DateTime.UtcNow };
        _context.Applications.Add(application);

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasApplicantAppliedAsync(applicant.Id, job.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasApplicantAppliedAsync_WhenApplicationDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var applicantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        // Act
        var result = await _repository.HasApplicantAppliedAsync(applicantId, jobId);

        // Assert
        Assert.False(result);
    }
}
