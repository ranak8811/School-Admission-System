using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models.Entities;

namespace SchoolAdmission.Api.Data;

/// <summary>
/// Root Entity Framework Core Database Context for the School Admission System.
/// Exposes Class and SeatHold tables with strict constraints and indexes.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ClassEntity> Classes => Set<ClassEntity>();
    public DbSet<SeatHoldEntity> SeatHolds => Set<SeatHoldEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
