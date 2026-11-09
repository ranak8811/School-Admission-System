using Microsoft.EntityFrameworkCore;

namespace SchoolAdmission.Api.Data;

/// <summary>
/// Root Entity Framework Core Database Context for the School Admission System.
/// Acts as the session gateway to PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
