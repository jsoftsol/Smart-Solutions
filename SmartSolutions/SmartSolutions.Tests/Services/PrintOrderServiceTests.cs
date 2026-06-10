// SmartSolutions.Tests/Services/PrintOrderServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Data.Entities;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class PrintOrderServiceTests
{
    private static async Task<(TestDbContextFactory factory, Customer customer, ItemName item)> SeedAsync()
    {
        var factory = TestDbContextFactory.Unique();
        await using var db = factory.CreateDbContext();
        var customer = new Customer { Name = "Test Customer" };
        var cat = new ItemCategory { Name = "Cat" };
        db.Customers.Add(customer);
        db.ItemCategories.Add(cat);
        await db.SaveChangesAsync();
        var item = new ItemName { Name = "Panaflex", CategoryId = cat.Id };
        db.ItemNames.Add(item);
        await db.SaveChangesAsync();
        return (factory, customer, item);
    }

    [Fact]
    public async Task CreateOrderAsync_DefaultStatusIsDraft()
    {
        var (factory, customer, _) = await SeedAsync();
        var svc = new PrintOrderService(factory, new TestSessionService());

        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);

        order.Status.Should().Be(PrintOrderStatus.Draft);
        order.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddLineAsync_PerSqftInFeet_ComputesTotalCorrectly()
    {
        var (factory, customer, item) = await SeedAsync();
        var svc = new PrintOrderService(factory, new TestSessionService());
        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);

        var line = await svc.AddLineAsync(order.Id, item.Id,
            RateType.PerSqft, DimensionUnit.Feet, 4m, 6m, 2, 100m);

        // 4 × 6 × 2 × 100 = 4800
        line.ComputeTotal().Should().Be(4800m);
    }

    [Fact]
    public async Task AddLineAsync_PerSqftInInches_ConvertsToFeet()
    {
        var (factory, customer, item) = await SeedAsync();
        var svc = new PrintOrderService(factory, new TestSessionService());
        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);

        var line = await svc.AddLineAsync(order.Id, item.Id,
            RateType.PerSqft, DimensionUnit.Inches, 12m, 12m, 1, 144m);

        // (12/12) × (12/12) × 1 × 144 = 1 × 1 × 144 = 144
        line.ComputeTotal().Should().Be(144m);
    }

    [Fact]
    public async Task AddLineAsync_PerPiece_IgnoresDimensions()
    {
        var (factory, customer, item) = await SeedAsync();
        var svc = new PrintOrderService(factory, new TestSessionService());
        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);

        var line = await svc.AddLineAsync(order.Id, item.Id,
            RateType.PerPiece, DimensionUnit.Feet, null, null, 500, 2m);

        // 500 × 2 = 1000
        line.ComputeTotal().Should().Be(1000m);
    }

    [Fact]
    public async Task PaymentDuplicateExistsAsync_ReturnsTrueForSameOrderAmountDate()
    {
        var (factory, customer, _) = await SeedAsync();
        await using var db = factory.CreateDbContext();
        var channel = new PaymentChannel { Name = "Cash" };
        db.PaymentChannels.Add(channel);
        await db.SaveChangesAsync();

        var svc = new PrintOrderService(factory, new TestSessionService());
        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);
        var date = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await svc.AddPaymentAsync(order.Id, 5000m, channel.Id, date, null);

        var isDuplicate = await svc.PaymentDuplicateExistsAsync(order.Id, 5000m, date);
        isDuplicate.Should().BeTrue();
    }
}
