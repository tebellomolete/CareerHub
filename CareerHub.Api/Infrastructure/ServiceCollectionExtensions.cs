using CareerHub.Api.Repositories;
using CareerHub.Api.Services;
using CareerHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CareerHub.Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCareerHubRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IJobListingRepository, JobListingRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IApplicantRepository, ApplicantRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddSingleton<SlowQueryInterceptor>();

        services.AddDbContext<CareerHubDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<SlowQueryInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(interceptor);
        }); 
        return services;
    }

    public static IServiceCollection AddCareerHubServices(this IServiceCollection services)
    {
        services.AddScoped<IJobListingService, JobListingService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        return services;
    }
}
