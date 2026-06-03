using Scalar.AspNetCore;
using Serilog;
using CareerHub.Api.Middleware;
using CareerHub.Api.Data;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;


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

    // Register CORS policy
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowNextJs", policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
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
    // 
    //════════════════════════════════════════════════════ 
    // PIPELINE — Configure the middleware chain. 
    // Order matters. Top to bottom. 
    // 
    //════════════════════════════════════════════════════ 
    app.UseSerilogRequestLogging(); // Logs every HTTP request + final response automatically 
    app.UseCors("AllowNextJs");
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