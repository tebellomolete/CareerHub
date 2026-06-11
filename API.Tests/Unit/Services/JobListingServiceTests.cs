using System;
using System.Threading.Tasks;
using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;
using CareerHub.Api.Services;
using NSubstitute;
using Xunit;

namespace API.Tests.Unit.Services;

public class JobListingServiceTests
{
    private readonly IJobListingRepository _jobRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly JobListingService _service;

    public JobListingServiceTests()
    {
        _jobRepository = Substitute.For<IJobListingRepository>();
        _companyRepository = Substitute.For<ICompanyRepository>();
        _service = new JobListingService(_jobRepository, _companyRepository);
    }

    [Fact]
    public async Task CreateAsync_WhenSalaryMaxLessThanSalaryMin_ThrowsInvalidSalaryException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepository.ExistsAsync(companyId).Returns(true);

        var request = new CreateJobRequest
        {
            CompanyId = companyId,
            Title = "Title",
            Description = "Description",
            Location = "Location",
            Type = JobType.FullTime,
            SalaryMin = 80000,
            SalaryMax = 50000,
            ClosingDate = DateTime.UtcNow.AddDays(1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateListingAsync(request));
        Assert.Equal("SalaryMax must be greater than or equal to SalaryMin", ex.Message);
        
        await _jobRepository.DidNotReceiveWithAnyArgs().AddListingAsync(default!);
    }

    [Fact]
    public async Task CreateAsync_WhenExpiresAtIsInThePast_ThrowsInvalidListingException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepository.ExistsAsync(companyId).Returns(true);

        var request = new CreateJobRequest
        {
            CompanyId = companyId,
            Title = "Title",
            Description = "Description",
            Location = "Location",
            Type = JobType.FullTime,
            SalaryMin = 50000,
            SalaryMax = 80000,
            ClosingDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateListingAsync(request));
        
        await _jobRepository.DidNotReceiveWithAnyArgs().AddListingAsync(default!);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_CallsAddAsyncExactlyOnce()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _companyRepository.ExistsAsync(companyId).Returns(true);

        var request = new CreateJobRequest
        {
            CompanyId = companyId,
            Title = "Title",
            Description = "Description",
            Location = "Location",
            Type = JobType.FullTime,
            SalaryMin = 50000,
            SalaryMax = 80000,
            ClosingDate = DateTime.UtcNow.AddDays(10)
        };

        var fakeDetail = new JobDetailResponse(
            Guid.NewGuid(), "Title", "Company Name", "Location", "Description", JobType.FullTime, DateTime.UtcNow, "R50,000 - R80,000/month", 0, new List<ApplicationResponse>());

        _jobRepository.GetListingWithDetailsAsync(Arg.Any<Guid>()).Returns(fakeDetail);

        // Act
        await _service.CreateListingAsync(request);

        // Assert
        await _jobRepository.Received(1).AddListingAsync(Arg.Any<JobListing>());
    }

    [Fact]
    public async Task PatchAsync_WhenOnlySalaryMinChanged_CallsValidation()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var company = new Company { Id = Guid.NewGuid(), Name = "Company" };
        var existingListing = new JobListing
        {
            Id = listingId,
            CompanyId = company.Id,
            Company = company,
            Title = "Title",
            SalaryMin = 50000,
            SalaryMax = 80000,
            PostedAt = DateTime.UtcNow.AddDays(-5)
        };

        _jobRepository.GetListingByIdAsync(listingId).Returns(existingListing);

        var request = new UpdateJobListingRequest(null, null, null, null, 100000, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.PatchAsync(listingId, request));
        Assert.Equal("SalaryMax must be greater than or equal to SalaryMin", ex.Message);
        
        await _jobRepository.DidNotReceiveWithAnyArgs().UpdateListingAsync(default!);
    }

    [Fact]
    public async Task PatchAsync_WhenOnlyTitleChanged_DoesNotCallSalaryValidation()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var company = new Company { Id = Guid.NewGuid(), Name = "Company" };
        var existingListing = new JobListing
        {
            Id = listingId,
            CompanyId = company.Id,
            Company = company,
            Title = "Title",
            SalaryMin = 50000,
            SalaryMax = 80000,
            PostedAt = DateTime.UtcNow.AddDays(-5)
        };

        _jobRepository.GetListingByIdAsync(listingId).Returns(existingListing);

        var fakeDetail = new JobDetailResponse(
            listingId, "New Title", "Company", "Loc", "Desc", JobType.FullTime, DateTime.UtcNow, "Salary", 0, null);
        
        _jobRepository.GetListingWithDetailsAsync(listingId).Returns(fakeDetail);

        var request = new UpdateJobListingRequest("New Title", null, null, null, null, null, null);

        // Act
        var result = await _service.PatchAsync(listingId, request);

        // Assert
        await _jobRepository.Received(1).UpdateListingAsync(existingListing);
        Assert.Equal("New Title", existingListing.Title);
    }

    [Fact]
    public async Task PatchAsync_WhenListingNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _jobRepository.GetListingByIdAsync(listingId).Returns((JobListing?)null);

        var request = new UpdateJobListingRequest("Title", null, null, null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<JobNotFoundException>(() => _service.PatchAsync(listingId, request));
        
        await _jobRepository.DidNotReceiveWithAnyArgs().UpdateListingAsync(default!);
    }
}
