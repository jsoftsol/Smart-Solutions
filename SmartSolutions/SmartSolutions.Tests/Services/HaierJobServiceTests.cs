// SmartSolutions.Tests/Services/HaierJobServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Data.Entities;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class HaierJobServiceTests
{
    private static async Task<(TestDbContextFactory factory, Customer customer, Technician tech)> SeedAsync()
    {
        var factory = TestDbContextFactory.Unique();
        await using var db = factory.CreateDbContext();
        var customer = new Customer { Name = "Ahmed" };
        var tech = new Technician { Name = "Ali" };
        db.Customers.Add(customer);
        db.Technicians.Add(tech);
        await db.SaveChangesAsync();
        return (factory, customer, tech);
    }

    [Fact]
    public async Task CreateJobAsync_WarrantyJob_StoresClaimReference()
    {
        var (factory, customer, tech) = await SeedAsync();
        var svc = new HaierJobService(factory, new TestSessionService());

        var job = await svc.CreateJobAsync(customer.Id, "HSU-12", null,
            "No cooling", tech.Id, HaierJobType.Warranty, "CLM-2026-001",
            null, 0, DateTime.UtcNow, null);

        job.JobType.Should().Be(HaierJobType.Warranty);
        job.ClaimReferenceNumber.Should().Be("CLM-2026-001");
        job.Status.Should().Be(HaierJobStatus.Pending);
    }
}
