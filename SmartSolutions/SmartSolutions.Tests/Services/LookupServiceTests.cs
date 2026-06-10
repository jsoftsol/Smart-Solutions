// SmartSolutions.Tests/Services/LookupServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class LookupServiceTests
{
    [Fact]
    public async Task AddItemCategoryAsync_PersistsAndReturnsCategory()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new LookupService(factory);

        var result = await svc.AddItemCategoryAsync("Panaflex");

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Panaflex");
        var all = await svc.GetItemCategoriesAsync();
        all.Should().ContainSingle(c => c.Name == "Panaflex");
    }

    [Fact]
    public async Task DeleteItemCategoryAsync_RemovesCategory()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new LookupService(factory);
        var cat = await svc.AddItemCategoryAsync("ToDelete");

        await svc.DeleteItemCategoryAsync(cat.Id);

        var all = await svc.GetItemCategoriesAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task RenameItemNameAsync_UpdatesName()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new LookupService(factory);
        var cat = await svc.AddItemCategoryAsync("Cat");
        var item = await svc.AddItemNameAsync("OldName", cat.Id);

        await svc.RenameItemNameAsync(item.Id, "NewName");

        var names = await svc.GetItemNamesAsync(cat.Id);
        names.Should().ContainSingle(n => n.Name == "NewName");
        names.Should().NotContain(n => n.Name == "OldName");
    }

    [Fact]
    public async Task RenameExpenseCategoryAsync_UpdatesName()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new LookupService(factory);
        var cat = await svc.AddExpenseCategoryAsync("Old");

        await svc.RenameExpenseCategoryAsync(cat.Id, "New");

        var all = await svc.GetExpenseCategoriesAsync();
        all.Should().ContainSingle(c => c.Name == "New");
    }

    [Fact]
    public async Task RenamePaymentChannelAsync_UpdatesName()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new LookupService(factory);
        var ch = await svc.AddPaymentChannelAsync("OldChannel");

        await svc.RenamePaymentChannelAsync(ch.Id, "NewChannel");

        var all = await svc.GetPaymentChannelsAsync();
        all.Should().Contain(c => c.Name == "NewChannel");
        all.Should().NotContain(c => c.Name == "OldChannel");
    }

    [Fact]
    public async Task GetPaymentChannelsAsync_ReturnsSeedDefaults()
    {
        // InMemory does not run HasData seeds — insert manually to mirror seed intent
        var factory = TestDbContextFactory.Unique();
        await using var ctx = factory.CreateDbContext();
        ctx.PaymentChannels.AddRange(
            new() { Id = 1, Name = "Cash" },
            new() { Id = 2, Name = "Easypaisa" },
            new() { Id = 3, Name = "Bank" });
        await ctx.SaveChangesAsync();

        var svc = new LookupService(factory);
        var channels = await svc.GetPaymentChannelsAsync();

        channels.Should().HaveCount(3);
        channels.Select(c => c.Name).Should().Contain(["Cash", "Easypaisa", "Bank"]);
    }
}
