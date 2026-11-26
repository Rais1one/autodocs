using AutoDocs.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoDocs.Storage.Data;

public class AutoDocsDbContext : DbContext
{
    public AutoDocsDbContext(
        DbContextOptions<AutoDocsDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();

    public DbSet<CheckRunEntity> CheckRuns => Set<CheckRunEntity>();

    public DbSet<DiscrepancyEntity> Discrepancies => Set<DiscrepancyEntity>();

    public DbSet<ApiSnapshotEntity> ApiSnapshots => Set<ApiSnapshotEntity>();

    public DbSet<SnapshotEndpointEntity> SnapshotEndpoints =>
        Set<SnapshotEndpointEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProjectEntity>(entity =>
        {
            entity.ToTable("Projects");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ProjectPath)
                .IsUnique();

            entity.HasMany(e => e.CheckRuns)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ApiSnapshots)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CheckRunEntity>(entity =>
        {
            entity.ToTable("CheckRuns", table =>
            {
                table.HasCheckConstraint(
                    "CK_CheckRuns_Source",
                    "Source IN ('CLI', 'WPF')");

                table.HasCheckConstraint(
                    "CK_CheckRuns_ExitCode",
                    "ExitCode IN (0, 1, 2)");
            });

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new
            {
                e.ProjectId,
                e.CheckedAt
            });

            entity.HasMany(e => e.Discrepancies)
                .WithOne(e => e.CheckRun)
                .HasForeignKey(e => e.CheckRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiscrepancyEntity>(entity =>
        {
            entity.ToTable("Discrepancies");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Kind)
                .HasConversion<string>();

            entity.Property(e => e.Severity)
                .HasConversion<string>();
        });

        modelBuilder.Entity<ApiSnapshotEntity>(entity =>
        {
            entity.ToTable("ApiSnapshots");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new
            {
                e.ProjectId,
                e.CreatedAt
            });

            entity.HasMany(e => e.Endpoints)
                .WithOne(e => e.ApiSnapshot)
                .HasForeignKey(e => e.ApiSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SnapshotEndpointEntity>(entity =>
        {
            entity.ToTable("SnapshotEndpoints");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.HttpMethod)
                .HasConversion<string>();

            entity.HasIndex(e => new
            {
                e.ApiSnapshotId,
                e.EndpointKey
            }).IsUnique();
        });
    }
}
