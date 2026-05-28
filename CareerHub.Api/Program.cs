using Scalar.AspNetCore;
using CareerHub.Api.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Enable Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// 2. Add built-in OpenAPI generation
builder.Services.AddOpenApi();

// 3. Register our data store
builder.Services.AddSingleton<JobStore>();

// --- PART 5: Global Error Handling Services ---
// Registers the internal services needed to standardize error responses into the RFC 7807 Problem Details JSON format.
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- PART 5: Global Error Handling Middleware ---
// Catches any unhandled exceptions crashing the app and formats them safely as a 500 Internal Server Error Problem Details response.
app.UseExceptionHandler();

// Intercepts empty HTTP error responses (like a 404 Not Found from a bad route) and injects a formatted Problem Details JSON body before sending it to the client.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // 4. Map the interactive Scalar UI
    app.MapScalarApiReference();
}

// 5. Map Controller routes
app.MapControllers();



app.Run();