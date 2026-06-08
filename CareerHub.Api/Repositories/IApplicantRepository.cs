using CareerHub.Api.Models;

namespace CareerHub.Api.Repositories;

public interface IApplicantRepository
{
    Task<Applicant?> GetByEmailAsync(string email);
    Task AddApplicantAsync(Applicant applicant);
}
