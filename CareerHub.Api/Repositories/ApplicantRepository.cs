using CareerHub.Api.Data;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public class ApplicantRepository : IApplicantRepository
{
    private readonly CareerHubDbContext _context;

    public ApplicantRepository(CareerHubDbContext context)
    {
        _context = context;
    }

    public async Task<Applicant?> GetByEmailAsync(string email)
    {
        return await _context.Applicants.FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task AddApplicantAsync(Applicant applicant)
    {
        _context.Applicants.Add(applicant);
        await _context.SaveChangesAsync();
    }
}
