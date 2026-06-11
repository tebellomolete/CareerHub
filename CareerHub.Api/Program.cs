using Scalar.AspNetCore;
using Serilog;
using CareerHub.Api.Middleware;
using CareerHub.Api.Data;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Asp.Versioning;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using CareerHub.Api.Infrastructure;


// 
//════════════════════════════════════════════════════ 
// Bootstrap Serilog before the host is built. 
// This ensures even startup exceptions are logged. 
// 
//════════════════════════════════════════════════════ 
Log.Logger = new LoggerConfiguration()
.WriteTo.Console()
.CreateLogger();
try
{
    Log.Information("Starting up the CareerHub API...");
    var builder = WebApplication.CreateBuilder(args);
    // Replace the default .NET logger with Serilog 
    builder.Host.UseSerilog();
    // 
    //════════════════════════════════════════════════════ 
    // BUILDER — Register services 
    // 
    //════════════════════════════════════════════════════ 
    builder.Host.UseDefaultServiceProvider((context, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    // DI Registration - Uses custom extension methods
    builder.Services.AddCareerHubRepositories(builder.Configuration);
    builder.Services.AddCareerHubServices();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddOpenApi();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<CareerHubDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Day 3 — typed handler
    builder.Services.AddProblemDetails();



    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 200,
                    QueueLimit = 0,
                    Window = TimeSpan.FromSeconds(60)
                }));

        options.AddSlidingWindowLimiter("search", opt =>
        {
            opt.PermitLimit = 30;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromSeconds(60);
            opt.SegmentsPerWindow = 6;
        });

        options.AddFixedWindowLimiter("apply", opt =>
        {
            opt.PermitLimit = 5;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromMinutes(60);
        });

        options.AddFixedWindowLimiter("post-listing", opt =>
        {
            opt.PermitLimit = 10;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromMinutes(60);
        });

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                await context.HttpContext.Response.WriteAsync($"Rate limit exceeded. Please retry after {(int)retryAfter.TotalSeconds} seconds.", token);
            }
            else
            {
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
            }
        };
    });

    builder.Services.AddApiVersioning(options =>

    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddMvc();

    // Register CORS policy

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowNextJs", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://careerhub-production.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .WithExposedHeaders("X-Total-Count");
        });
    });

    // Register JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
    var key = Encoding.UTF8.GetBytes(jwtSecret);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

    // Register Authorization service
    builder.Services.AddAuthorization();

    // 
    // Day 2 — standardised errors 
    //════════════════════════════════════════════════════ 
    // TRANSITION — Build() seals the DI container. 
    // Nothing can be registered after this line. 
    // 
    //════════════════════════════════════════════════════ 
    var app = builder.Build();

    // Seed Data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<CareerHubDbContext>();
            await context.Database.MigrateAsync();
            await SeedData.SeedAsync(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database.");
        }
    }

    // 
    //════════════════════════════════════════════════════ 
    // PIPELINE — Configure the middleware chain. 
    // Order matters. Top to bottom. 
    // 
    //════════════════════════════════════════════════════ 
    app.UseSerilogRequestLogging(); // Logs every HTTP request + final response automatically 
    app.UseCors("AllowNextJs");
    app.UseRateLimiter();
    app.UseExceptionHandler();  // Activates GlobalExceptionHandler — catches all thrown exceptions 
    app.UseStatusCodePages();   // Fills empty 4xx/5xx responses with Problem Details body 
    app.UseAuthentication();
    app.UseAuthorization();
    if (app.Environment.IsDevelopment())
    {
    }
    app.MapOpenApi();
    // Serves /openapi/v1.json 
    app.MapScalarApiReference();  // Serves the Scalar UI at /scalar/v1 
    app.MapControllers();  // Activates attribute routing for all [ApiController] classes 
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start correctly.");
}

finally
{
    Log.CloseAndFlush(); //Ensure all buffered log entries are flushed before application exit. 
}
public partial class Program { }
