using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CareerHub.Api.Models;

namespace CareerHub.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(CareerHubDbContext db)
    {
        if (await db.Companies.AnyAsync()) return;

        // ── Companies ──────────────────────────────────────
        // We initialize a set of professional companies spanning various industries.
        var company1Id = Guid.NewGuid();
        var company2Id = Guid.NewGuid();
        var company3Id = Guid.NewGuid();

        var companies = new List<Company>
        {
            new Company 
            { 
                Id = company1Id, 
                Name = "TechNova Solutions", 
                Industry = "Software Development", 
                Website = "https://technova.example.com" 
            },
            new Company 
            { 
                Id = company2Id, 
                Name = "Global Finance Partners", 
                Industry = "Financial Services", 
                Website = "https://globalfinance.example.com" 
            },
            new Company 
            { 
                Id = company3Id, 
                Name = "GreenEarth Energy", 
                Industry = "Renewable Energy", 
                Website = "https://greenearth.example.com" 
            }
        };
        
        await db.Companies.AddRangeAsync(companies);

        // ── Applicants ──────────────────────────────────────
        // Generating a diverse pool of applicants looking for various roles.
        var applicant1Id = Guid.NewGuid();
        var applicant2Id = Guid.NewGuid();
        var applicant3Id = Guid.NewGuid();

        var applicants = new List<Applicant>
        {
            new Applicant 
            { 
                Id = applicant1Id, 
                Name = "Alice Smith", 
                Email = "alice.smith@example.com" 
            },
            new Applicant 
            { 
                Id = applicant2Id, 
                Name = "Bob Johnson", 
                Email = "bob.johnson@example.com" 
            },
            new Applicant 
            { 
                Id = applicant3Id, 
                Name = "Charlie Davis", 
                Email = "charlie.davis@example.com" 
            }
        };
        
        await db.Applicants.AddRangeAsync(applicants);

        // ── JobListings ──────────────────────────────────────
        // Creating job listings that belong to the companies seeded above.
        var job1Id = Guid.NewGuid();
        var job2Id = Guid.NewGuid();
        var job3Id = Guid.NewGuid();
        var job4Id = Guid.NewGuid();

        var jobListings = new List<JobListing>
        {
            new JobListing 
            { 
                Id = job1Id, 
                CompanyId = company1Id, 
                Title = "Senior Backend Engineer", 
                Description = "Looking for an experienced engineer to build scalable APIs.", 
                Location = "Remote", 
                Type = JobType.FullTime, 
                SalaryMin = 120000, 
                SalaryMax = 160000, 
                PostedAt = DateTime.UtcNow.AddDays(-10), 
                IsActive = true 
            },
            new JobListing 
            { 
                Id = job2Id, 
                CompanyId = company1Id, 
                Title = "Frontend Developer", 
                Description = "Join us to craft beautiful user interfaces using React.", 
                Location = "San Francisco, CA", 
                Type = JobType.FullTime, 
                SalaryMin = 100000, 
                SalaryMax = 140000, 
                PostedAt = DateTime.UtcNow.AddDays(-5), 
                IsActive = true 
            },
            new JobListing 
            { 
                Id = job3Id, 
                CompanyId = company2Id, 
                Title = "Financial Analyst", 
                Description = "Analyze market trends and assist with strategic planning.", 
                Location = "New York, NY", 
                Type = JobType.FullTime, 
                SalaryMin = 80000, 
                SalaryMax = 110000, 
                PostedAt = DateTime.UtcNow.AddDays(-2), 
                IsActive = true 
            },
            new JobListing 
            { 
                Id = job4Id, 
                CompanyId = company3Id, 
                Title = "Sustainability Consultant", 
                Description = "Help our clients transition to renewable energy sources.", 
                Location = "Austin, TX", 
                Type = JobType.Contract, 
                SalaryMin = 90000, 
                SalaryMax = 130000, 
                PostedAt = DateTime.UtcNow.AddDays(-15), 
                IsActive = true 
            }
        };
        
        await db.JobListings.AddRangeAsync(jobListings);

        // ── Applications ──────────────────────────────────────
        // Linking applicants to job listings to simulate real-world job applications.
        var applications = new List<Application>
        {
            new Application 
            { 
                ApplicantId = applicant1Id, 
                JobListingId = job1Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-8), 
                Status = ApplicationStatus.Interviewing 
            },
            new Application 
            { 
                ApplicantId = applicant1Id, 
                JobListingId = job2Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-3), 
                Status = ApplicationStatus.Submitted 
            },
            new Application 
            { 
                ApplicantId = applicant2Id, 
                JobListingId = job3Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-1), 
                Status = ApplicationStatus.UnderReview 
            },
            new Application 
            { 
                ApplicantId = applicant3Id, 
                JobListingId = job1Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-9), 
                Status = ApplicationStatus.Rejected 
            },
            new Application 
            { 
                ApplicantId = applicant3Id, 
                JobListingId = job4Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-12), 
                Status = ApplicationStatus.Offered 
            }
        };
        
        await db.Applications.AddRangeAsync(applications);

        await db.SaveChangesAsync();
    }
}
