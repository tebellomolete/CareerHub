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
        try
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

        var company4Id = Guid.NewGuid();
        var company5Id = Guid.NewGuid();

        var moreCompanies = new List<Company>
        {
            new Company { Id = company4Id, Name = "DataFlow Inc", Industry = "Data Analytics", Website = "https://dataflow.example.com" },
            new Company { Id = company5Id, Name = "HealthPlus", Industry = "Healthcare", Website = "https://healthplus.example.com" }
        };
        await db.Companies.AddRangeAsync(moreCompanies);

        // ── JobListings ──────────────────────────────────────
        var jobListings = new List<JobListing>();
        var random = new Random(42); // Seed for consistency
        var companyIds = new[] { company1Id, company2Id, company3Id, company4Id, company5Id };
        var titles = new[] { "Engineer", "Developer", "Analyst", "Consultant", "Manager", "Designer", "Architect", "Specialist" };
        var adjectives = new[] { "Senior", "Junior", "Lead", "Principal", "Staff", "Frontend", "Backend", "Fullstack", "Data", "Cloud" };
        var searchTerms = new[] { "sprint", "sprinting", "agile", "react", "cloud", "aws", "azure", "docker" };

        // Assignment 2.1 (mobile) — cities used to compose on-site and
        // hybrid location strings so the Flutter Job.inferLocationType
        // heuristic ("remote" / "hybrid" substring match) actually
        // populates all three buckets of its Location dropdown.
        var cities = new[] { "Cape Town, ZA", "Johannesburg, ZA", "Durban, ZA", "Pretoria, ZA", "Sandton, ZA" };

        // Weighted employment-type pool. Roughly 50% Full-time, 20%
        // Part-time, 20% Contract, 10% Internship — matches the four
        // options on the Flutter Job Type dropdown and gives every
        // option non-trivial data behind it.
        var jobTypeWeights = new[]
        {
            JobType.FullTime,  JobType.FullTime,  JobType.FullTime,  JobType.FullTime,  JobType.FullTime,
            JobType.PartTime,  JobType.PartTime,
            JobType.Contract,  JobType.Contract,
            JobType.Internship
        };

        for (int i = 0; i < 200; i++)
        {
            var cid = companyIds[random.Next(companyIds.Length)];
            var title = $"{adjectives[random.Next(adjectives.Length)]} {titles[random.Next(titles.Length)]} {i}";

            // Add search terms to some descriptions
            var description = $"Looking for an experienced professional to join our team. We value {searchTerms[random.Next(searchTerms.Length)]} experience.";

            var isActive = random.Next(10) > 2; // 70% active
            var daysPosted = random.Next(1, 30);
            var daysClosing = random.Next(-5, 30); // Some might be closed by date

            // Ensure ClosingDate > PostedAt
            var posted = DateTime.UtcNow.AddDays(-daysPosted);
            var closing = DateTime.UtcNow.AddDays(daysClosing);
            if (closing <= posted)
            {
                closing = posted.AddDays(random.Next(1, 30));
            }

            // Location bucket — roughly 40% Remote, 30% Hybrid, 30% on-site.
            // The Flutter heuristic matches on the literal substrings
            // "remote" / "hybrid" (case-insensitive), so the exact
            // formats below are what actually drive the mobile filter.
            var bucket = random.Next(10);
            string location;
            if (bucket < 4)
            {
                location = "Remote";
            }
            else if (bucket < 7)
            {
                location = $"{cities[random.Next(cities.Length)]} (Hybrid)";
            }
            else
            {
                location = cities[random.Next(cities.Length)];
            }

            var jobType = jobTypeWeights[random.Next(jobTypeWeights.Length)];

            jobListings.Add(new JobListing
            {
                Id = Guid.NewGuid(),
                CompanyId = cid,
                Title = title,
                Description = description,
                Location = location,
                Type = jobType,
                SalaryMin = 80000 + random.Next(0, 40) * 1000,
                SalaryMax = 130000 + random.Next(0, 40) * 1000,
                PostedAt = posted,
                ClosingDate = closing,
                IsActive = isActive
            });
        }
        
        // Ensure at least 3 exact matches for "sprint"
        jobListings[0].Description = "We do a lot of sprint planning.";
        jobListings[1].Title = "Sprint Master";
        jobListings[2].Description = "You will be responsible for the sprint.";

        // And some for "sprinting"
        jobListings[3].Description = "We are sprinting towards our goal.";
        jobListings[4].Title = "Sprinting expert";
        
        await db.JobListings.AddRangeAsync(jobListings);

        // ── Applications ──────────────────────────────────────
        // Linking applicants to job listings to simulate real-world job applications.
        var applications = new List<Application>
        {
            new Application 
            { 
                ApplicantId = applicant1Id, 
                JobListingId = jobListings[0].Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-8), 
                Status = ApplicationStatus.Interviewing 
            },
            new Application 
            { 
                ApplicantId = applicant1Id, 
                JobListingId = jobListings[1].Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-3), 
                Status = ApplicationStatus.Submitted 
            },
            new Application 
            { 
                ApplicantId = applicant2Id, 
                JobListingId = jobListings[2].Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-1), 
                Status = ApplicationStatus.UnderReview 
            },
            new Application 
            { 
                ApplicantId = applicant3Id, 
                JobListingId = jobListings[0].Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-9), 
                Status = ApplicationStatus.Rejected 
            },
            new Application 
            { 
                ApplicantId = applicant3Id, 
                JobListingId = jobListings[3].Id, 
                SubmittedAt = DateTime.UtcNow.AddDays(-12), 
                Status = ApplicationStatus.Offered 
            }
        };
        
        await db.Applications.AddRangeAsync(applications);

        await db.SaveChangesAsync();
        Console.WriteLine("Seed completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SEED ERROR: {ex}");
            throw;
        }
    }
}
