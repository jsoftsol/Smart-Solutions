// SmartSolutions.Tests/Helpers/TestDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Data;

namespace SmartSolutions.Tests.Helpers;

public class TestDbContextFactory(string dbName) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    // Convenience: unique db per test to ensure isolation
    public static TestDbContextFactory Unique() =>
        new(Guid.NewGuid().ToString());
}
