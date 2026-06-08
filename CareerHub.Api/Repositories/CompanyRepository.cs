using CareerHub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly CareerHubDbContext _context;

    public CompanyRepository(CareerHubDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Companies.AnyAsync(c => c.Id == id);
    }
}
