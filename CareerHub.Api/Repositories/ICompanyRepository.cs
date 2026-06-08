namespace CareerHub.Api.Repositories;

public interface ICompanyRepository
{
    Task<bool> ExistsAsync(Guid id);
}
