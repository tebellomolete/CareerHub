using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;

namespace CareerHub.Api.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobListingRepository _jobRepository;
    private readonly IApplicantRepository _applicantRepository;

    private static readonly Dictionary<ApplicationStatus, HashSet<ApplicationStatus>> ValidTransitions = new()
    {
        { ApplicationStatus.Submitted, new HashSet<ApplicationStatus> { ApplicationStatus.UnderReview } },
        { ApplicationStatus.UnderReview, new HashSet<ApplicationStatus> { ApplicationStatus.Shortlisted, ApplicationStatus.Rejected } },
        { ApplicationStatus.Shortlisted, new HashSet<ApplicationStatus> { ApplicationStatus.Offered, ApplicationStatus.Rejected } },
        { ApplicationStatus.Offered, new HashSet<ApplicationStatus> { ApplicationStatus.Hired, ApplicationStatus.Rejected } },
        { ApplicationStatus.Hired, new HashSet<ApplicationStatus>() },
        { ApplicationStatus.Rejected, new HashSet<ApplicationStatus>() }
    };

    public ApplicationService(
        IApplicationRepository applicationRepository,
        IJobListingRepository jobRepository,
        IApplicantRepository applicantRepository)
    {
        _applicationRepository = applicationRepository;
        _jobRepository = jobRepository;
        _applicantRepository = applicantRepository;
    }

    public bool IsTransitionValid(ApplicationStatus currentStatus, ApplicationStatus newStatus)
    {
        return ValidTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus);
    }

    public async Task SubmitApplicationAsync(Guid listingId, SubmitApplicationRequest request)
    {
        if (!await _jobRepository.IsOpenForApplicationsAsync(listingId))
        {
            throw new ListingClosedException(listingId);
        }

        var applicant = await _applicantRepository.GetByEmailAsync(request.ApplicantEmail);
        if (applicant == null)
        {
            applicant = new Applicant
            {
                Id = Guid.NewGuid(),
                Name = request.ApplicantName,
                Email = request.ApplicantEmail
            };
            await _applicantRepository.AddApplicantAsync(applicant);
        }

        if (await _applicationRepository.HasApplicantAppliedAsync(applicant.Id, listingId))
        {
            throw new DuplicateApplicationException(applicant.Id, listingId);
        }

        var application = new Application
        {
            ApplicantId = applicant.Id,
            JobListingId = listingId,
            Status = ApplicationStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        await _applicationRepository.AddApplicationAsync(application);
    }

    public async Task UpdateApplicationStatusAsync(Guid applicantId, Guid listingId, ApplicationStatus newStatus)
    {
        var application = await _applicationRepository.GetApplicationAsync(applicantId, listingId);
        if (application == null) throw new ArgumentException("Application not found.");

        if (!IsTransitionValid(application.Status, newStatus))
        {
            throw new InvalidStatusTransitionException(application.Status, newStatus);
        }

        application.Status = newStatus;
        await _applicationRepository.UpdateApplicationStatusAsync(application);
    }

    public async Task WithdrawApplicationAsync(Guid applicantId, Guid listingId, Guid requestingApplicantId)
    {
        if (applicantId != requestingApplicantId)
        {
            throw new UnauthorizedApplicationAccessException(applicantId);
        }

        var application = await _applicationRepository.GetApplicationAsync(applicantId, listingId);
        if (application == null) throw new ArgumentException("Application not found.");

        await _applicationRepository.DeleteApplicationAsync(application);
    }

    public async Task<ApplicationResponse?> GetApplicationAsync(Guid applicantId, Guid listingId)
    {
        var application = await _applicationRepository.GetApplicationAsync(applicantId, listingId);
        if (application == null) return null;

        var applicantName = application.Applicant?.Name ?? "Unknown";
        
        return new ApplicationResponse(applicantName, application.SubmittedAt, application.Status);
    }
}
