using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Xunit;

namespace API.Tests.Integration;

public class JobsControllerTests : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly WebApplicationFactoryFixture _factory;

    public JobsControllerTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetJobs_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/jobs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_ResponseIsPagedEnvelope()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/jobs?page=1&pageSize=5");

        // Assert
        response.EnsureSuccessStatusCode();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<JobResponse>>(options);
        Assert.NotNull(pagedResponse);
        Assert.Equal(1, pagedResponse.Page);
        Assert.Equal(5, pagedResponse.PageSize);
        Assert.True(pagedResponse.TotalCount >= 0);
    }

    [Fact]
    public async Task GetJobs_ResponseIncludesXTotalCountHeader()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/jobs");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("X-Total-Count"));
    }

    [Fact]
    public async Task GetJobs_WithoutVersion_ReturnsSameStatusAsV1()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var responseV1 = await client.GetAsync("/api/v1/jobs");
        var responseUnversioned = await client.GetAsync("/api/jobs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, responseV1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseUnversioned.StatusCode);
    }

    [Fact]
    public async Task GetJobs_ResponseIncludesApiSupportedVersionsHeader()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/jobs");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("api-supported-versions"));
        var versions = response.Headers.GetValues("api-supported-versions");
        Assert.Contains("1.0", versions);
    }

    [Fact]
    public async Task PostJob_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateJobRequest
        {
            CompanyId = Guid.NewGuid(),
            Title = "Title",
            Description = "Description",
            Location = "Location",
            Type = JobType.FullTime,
            ClosingDate = DateTime.UtcNow.AddDays(10)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/jobs", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostApplication_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new SubmitApplicationRequest("Applicant", "test@test.com");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/jobs/{Guid.NewGuid()}/applications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetJobById_WithValidId_DoesNotReturn500()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/v1/jobs/{Guid.NewGuid()}");

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetJobById_ResponseIncludesETagHeader()
    {
        // Arrange
        var client = _factory.CreateClient();
        var allJobsResponse = await client.GetAsync("/api/v1/jobs");
        allJobsResponse.EnsureSuccessStatusCode();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var pagedResponse = await allJobsResponse.Content.ReadFromJsonAsync<PagedResponse<JobResponse>>(options);
        
        if (pagedResponse == null || !pagedResponse.Data.Any())
        {
            // If no data, cannot test.
            return;
        }

        var jobId = pagedResponse.Data.First().Id;

        // Act
        var response = await client.GetAsync($"/api/v1/jobs/{jobId}");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.ETag.Tag));
    }

    [Fact]
    public async Task GetJobById_WithMatchingETag_Returns304()
    {
        // Arrange
        var client = _factory.CreateClient();
        var allJobsResponse = await client.GetAsync("/api/v1/jobs");
        allJobsResponse.EnsureSuccessStatusCode();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var pagedResponse = await allJobsResponse.Content.ReadFromJsonAsync<PagedResponse<JobResponse>>(options);
        
        if (pagedResponse == null || !pagedResponse.Data.Any())
        {
            return;
        }

        var jobId = pagedResponse.Data.First().Id;
        var firstResponse = await client.GetAsync($"/api/v1/jobs/{jobId}");
        firstResponse.EnsureSuccessStatusCode();
        var etag = firstResponse.Headers.ETag!.Tag;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}");
        request.Headers.IfNoneMatch.ParseAdd(etag);

        // Act
        var secondResponse = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
    }
}
