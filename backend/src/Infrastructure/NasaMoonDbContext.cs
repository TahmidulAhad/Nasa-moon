using Microsoft.EntityFrameworkCore;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Infrastructure;

public class NasaMoonDbContext : DbContext
{
    public NasaMoonDbContext(DbContextOptions<NasaMoonDbContext> options) : base(options)
    {
    }

    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<Payload> Payloads => Set<Payload>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<DataSnapshot> Snapshots => Set<DataSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Mission>(b =>
        {
            b.ToTable("missions");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasMaxLength(50);
            b.Property(m => m.Name).IsRequired().HasMaxLength(200);
            b.Property(m => m.Status).IsRequired().HasMaxLength(20);
            b.HasIndex(m => m.Status);
            b.HasIndex(m => m.Provider);
            b.HasIndex(m => m.Region);
            b.HasMany(m => m.Payloads)
                .WithOne(p => p.Mission)
                .HasForeignKey(p => p.MissionId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(m => m.Sources)
                .WithOne(s => s.Mission)
                .HasForeignKey(s => s.MissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payload>(b =>
        {
            b.ToTable("payloads");
            b.Property(p => p.Name).IsRequired().HasMaxLength(300);
        });

        modelBuilder.Entity<Source>(b =>
        {
            b.ToTable("sources");
            b.Property(s => s.Label).IsRequired().HasMaxLength(300);
            b.Property(s => s.Url).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<DataSnapshot>(b =>
        {
            b.ToTable("data_snapshots");
            b.HasKey(s => s.Id);
            b.Property(s => s.Label).IsRequired().HasMaxLength(200);
            b.Property(s => s.Source).IsRequired().HasMaxLength(20);
            b.Property(s => s.MissionsJson).IsRequired();
            b.HasIndex(s => s.CreatedAt);
        });
    }
}
