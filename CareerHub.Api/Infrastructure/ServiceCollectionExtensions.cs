using CareerHub.Api.Repositories;
using CareerHub.Api.Services;
using CareerHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// Assignment 2.4 — the auth surface (users, refresh tokens, token
// issuance) is registered via `AddCareerHubAuth()` below.

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

    // Assignment 2.4 — auth surface. Users and refresh tokens are
    // process-scoped state (singletons); the token issuer is
    // scoped so it can read scoped IConfiguration through the
    // normal MVC pipeline.
    public static IServiceCollection AddCareerHubAuth(this IServiceCollection services)
    {
        services.AddSingleton<IUserAccountStore, InMemoryUserAccountStore>();
        services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        services.AddScoped<ITokenService, TokenService>();
        // Assignment 2.4 Stretch C — saved-jobs bookkeeping.
        services.AddSingleton<ISavedJobsStore, InMemorySavedJobsStore>();
        return services;
    }
}
