using CareerHub.Api.DTOs;
using CareerHub.Api.Models;

namespace CareerHub.Api.Services;

public interface IApplicationService
{
    Task SubmitApplicationAsync(Guid listingId, SubmitApplicationRequest request);
    Task UpdateApplicationStatusAsync(Guid applicantId, Guid listingId, ApplicationStatus newStatus);
    Task WithdrawApplicationAsync(Guid applicantId, Guid listingId, Guid requestingApplicantId);
}
