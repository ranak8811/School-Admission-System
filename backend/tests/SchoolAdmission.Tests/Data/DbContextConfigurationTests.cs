using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Api.Data;
using Xunit;

namespace SchoolAdmission.Tests.Data;

public class DbContextConfigurationTests
{
    [Fact]
    public void AppDbContext_CanBeConfiguredAndInstantiated()
    {
        // Arrange
        var services = new ServiceCollection();
        var testConnectionString = "Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(testConnectionString);
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<AppDbContext>();

        // Assert
        Assert.NotNull(dbContext);
    }
}
