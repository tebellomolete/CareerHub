using CareerHub.Api.Repositories;
using CareerHub.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CareerHub.Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCareerHubRepositories(this IServiceCollection services)
    {
        services.AddScoped<IJobListingRepository, JobListingRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IApplicantRepository, ApplicantRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        return services;
    }

    public static IServiceCollection AddCareerHubServices(this IServiceCollection services)
    {
        services.AddScoped<IJobListingService, JobListingService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        return services;
    }
}
