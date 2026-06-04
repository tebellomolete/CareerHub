namespace CareerHub.Api.Data;

using Microsoft.EntityFrameworkCore;
using CareerHub.Api.Models;

public class CareerHubDbContext(DbContextOptions<CareerHubDbContext> options) : DbContext(options)
{
    public DbSet<JobListing> JobListings { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Applicant> Applicants { get; set; }
    public DbSet<Application> Applications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Industry).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(255);
        });

        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.ToTable("applicants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("applications");
            entity.HasKey(e => new { e.ApplicantId, e.JobListingId });
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            
            entity.HasOne(e => e.Applicant)
                  .WithMany(a => a.Applications)
                  .HasForeignKey(e => e.ApplicantId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.JobListing)
                  .WithMany(j => j.Applications)
                  .HasForeignKey(e => e.JobListingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobListing>(entity =>
        {
            entity.ToTable("job_listings");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Company)
                  .WithMany(c => c.JobListings)
                  .HasForeignKey(e => e.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.Title, e.CompanyId }).IsUnique();
        });
    }
}
