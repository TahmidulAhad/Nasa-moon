using Microsoft.EntityFrameworkCore;
using NasaMoon.Application;
using NasaMoon.Infrastructure;
using NasaMoon.Infrastructure.SeedData;

var builder = WebApplication.CreateBuilder(args);

// Clean Architecture layers: each layer registers its own services.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The React frontend (Vite dev server + deployed Vercel site) calls this API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(
            "http://localhost:5173",
            "https://clps-agent-kit.vercel.app")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

// Create the tables on first run (fresh database, e.g. Neon) and then
// seed the 15 CLPS missions + payloads from the frontend's static data
// (only when the database is empty).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NasaMoonDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();
