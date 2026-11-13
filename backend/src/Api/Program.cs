using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NasaMoon.Application;
using NasaMoon.Infrastructure;
using NasaMoon.Infrastructure.SeedData;
using NasaMoon.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Clean Architecture layers: each layer registers its own services.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT authentication. The secret is resolved once here (same JwtOptions the
// token service uses): env var Jwt__Secret on Render, user secrets locally.
// In Development a missing secret falls back to a random ephemeral one;
// in Production startup fails fast without a real secret.
var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// Brute-force guard for login/register: 10 attempts per minute per IP.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// The React frontend (Vite dev server + deployed Vercel site) calls this API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(
            "http://localhost:5173",
            "https://clps-agent-kit.vercel.app",
            "https://nasa-moon.vercel.app")
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Friendly landing page: the API root used to return 404, which looked
// like the site was down. Now it explains itself.
app.MapGet("/", () => Results.Json(new
{
    service = "NasaMoon API",
    status = "ok",
    missions = "/api/missions",
    docs = "Use GET /api/missions — try /api/missions?search=peregrine"
}));
app.MapGet("/health", () => Results.Json(new { status = "healthy" }));

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
