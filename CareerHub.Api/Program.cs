using Scalar.AspNetCore;
using CareerHub.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Enable Controllers
builder.Services.AddControllers();

// 2. Add built-in OpenAPI generation
builder.Services.AddOpenApi();

// 3. Register our data store (we will create this next)
builder.Services.AddSingleton<JobStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // 4. Map the interactive Scalar UI
    app.MapScalarApiReference(); 
}

// 5. Map Controller routes
app.MapControllers();

// We will add Minimal API routes here later...

app.Run();