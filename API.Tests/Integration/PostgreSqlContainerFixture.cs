using System;
using System.Threading.Tasks;
using CareerHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace API.Tests.Integration;

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;

    public PostgreSqlContainerFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("CareerHubTestDb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();
    }

    public CareerHubDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CareerHubDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        return new CareerHubDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(); // Ensure schema is created
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
