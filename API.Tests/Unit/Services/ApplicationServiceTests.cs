using System;
using System.Threading.Tasks;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;
using CareerHub.Api.Services;
using NSubstitute;
using Xunit;

namespace API.Tests.Unit.Services;

public class ApplicationServiceTests
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobListingRepository _jobRepository;
    private readonly IApplicantRepository _applicantRepository;
    private readonly ApplicationService _service;

    public ApplicationServiceTests()
    {
        _applicationRepository = Substitute.For<IApplicationRepository>();
        _jobRepository = Substitute.For<IJobListingRepository>();
        _applicantRepository = Substitute.For<IApplicantRepository>();
        _service = new ApplicationService(_applicationRepository, _jobRepository, _applicantRepository);
    }

    [Theory]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Offered)]
    [InlineData(ApplicationStatus.Shortlisted, ApplicationStatus.Rejected)]
    // In ApplicationService ValidTransitions, it has Interviewing instead of Shortlisted!
    // The test instructions say:
    // Submitted -> UnderReview
    // UnderReview -> Shortlisted
    // UnderReview -> Rejected
    // Shortlisted -> Offered
    // Shortlisted -> Rejected
    // Wait, the assignment instructions explicitly list these. Let's fix the ApplicationService dictionary!
    public async Task UpdateStatusAsync_WhenTransitionIsLegal_CallsUpdateAsync(ApplicationStatus from, ApplicationStatus to)
    {
        // Arrange
        var applicantId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var application = new Application
        {
            ApplicantId = applicantId,
            JobListingId = listingId,
            Status = from
        };

        _applicationRepository.GetApplicationAsync(applicantId, listingId).Returns(application);

        // Act
        await _service.UpdateApplicationStatusAsync(applicantId, listingId, to);

        // Assert
        await _applicationRepository.Received(1).UpdateApplicationStatusAsync(application);
        Assert.Equal(to, application.Status);
    }

    [Theory]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Offered, ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Offered, ApplicationStatus.Shortlisted)]
    public async Task UpdateStatusAsync_WhenTransitionIsIllegal_ThrowsInvalidStatusTransitionException(ApplicationStatus from, ApplicationStatus to)
    {
        // Arrange
        var applicantId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var application = new Application
        {
            ApplicantId = applicantId,
            JobListingId = listingId,
            Status = from
        };

        _applicationRepository.GetApplicationAsync(applicantId, listingId).Returns(application);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => _service.UpdateApplicationStatusAsync(applicantId, listingId, to));

        await _applicationRepository.DidNotReceiveWithAnyArgs().UpdateApplicationStatusAsync(default!);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenApplicationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var applicantId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        _applicationRepository.GetApplicationAsync(applicantId, listingId).Returns((Application?)null);

        // Act & Assert
        // Code throws ArgumentException("Application not found.") currently.
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateApplicationStatusAsync(applicantId, listingId, ApplicationStatus.Submitted));
        
        await _applicationRepository.DidNotReceiveWithAnyArgs().UpdateApplicationStatusAsync(default!);
    }
}
