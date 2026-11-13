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
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SavedItem> SavedItems => Set<SavedItem>();
    public DbSet<SavedItemTag> SavedItemTags => Set<SavedItemTag>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionItem> CollectionItems => Set<CollectionItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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
            b.HasIndex(s => s.UserId);
            b.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.Role).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(t => t.Id);
            b.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.UserId);
            b.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedItem>(b =>
        {
            b.ToTable("saved_items");
            b.HasKey(s => s.Id);
            b.Property(s => s.MissionId).IsRequired().HasMaxLength(50);
            b.HasIndex(s => new { s.UserId, s.MissionId }).IsUnique();
            b.HasIndex(s => s.UserId);
            b.HasOne(s => s.User)
                .WithMany(u => u.SavedItems)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(s => s.Tags)
                .WithOne(t => t.SavedItem)
                .HasForeignKey(t => t.SavedItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedItemTag>(b =>
        {
            b.ToTable("saved_item_tags");
            b.HasKey(t => new { t.SavedItemId, t.Tag });
            b.Property(t => t.Tag).HasMaxLength(50);
        });

        modelBuilder.Entity<Collection>(b =>
        {
            b.ToTable("collections");
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).IsRequired().HasMaxLength(100);
            b.HasIndex(c => c.UserId);
            b.HasOne(c => c.User)
                .WithMany(u => u.Collections)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasMany(c => c.Items)
                .WithOne(i => i.Collection)
                .HasForeignKey(i => i.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CollectionItem>(b =>
        {
            b.ToTable("collection_items");
            b.HasKey(i => new { i.CollectionId, i.MissionId });
            b.Property(i => i.MissionId).HasMaxLength(50);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(a => a.Id);
            b.Property(a => a.Action).IsRequired().HasMaxLength(50);
            b.Property(a => a.Entity).IsRequired().HasMaxLength(50);
            b.HasIndex(a => a.CreatedAt);
            b.HasIndex(a => a.UserId);
        });
    }
}
