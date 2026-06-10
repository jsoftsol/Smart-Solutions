// SmartSolutions.Tests/Services/CustomerServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task AddCustomerAsync_PersistsAndReturnsCustomer()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());

        var result = await svc.AddCustomerAsync("Ali Khan", "03001234567", "Peshawar", null);

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Ali Khan");
        result.Phone.Should().Be("03001234567");
        result.Address.Should().Be("Peshawar");
        result.Notes.Should().BeNull();
    }

    [Fact]
    public async Task SearchCustomersAsync_NoQuery_ReturnsAll()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());
        await svc.AddCustomerAsync("Alice", null, null, null);
        await svc.AddCustomerAsync("Bob", null, null, null);

        var result = await svc.SearchCustomersAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchCustomersAsync_ByName_FiltersResults()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());
        await svc.AddCustomerAsync("Ali Khan", "03001234567", null, null);
        await svc.AddCustomerAsync("Bilal Ahmed", null, null, null);

        var result = await svc.SearchCustomersAsync("Ali");

        result.Should().ContainSingle(c => c.Name == "Ali Khan");
        result.Should().NotContain(c => c.Name == "Bilal Ahmed");
    }

    [Fact]
    public async Task UpdateCustomerAsync_ChangesFields()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());
        var customer = await svc.AddCustomerAsync("Old Name", null, null, null);

        await svc.UpdateCustomerAsync(customer.Id, "New Name", "0300", "Addr", "Note");

        var updated = await svc.GetCustomerAsync(customer.Id);
        updated.Name.Should().Be("New Name");
        updated.Phone.Should().Be("0300");
        updated.Address.Should().Be("Addr");
        updated.Notes.Should().Be("Note");
    }

    [Fact]
    public async Task DeleteCustomerAsync_RemovesCustomer()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());
        var customer = await svc.AddCustomerAsync("ToDelete", null, null, null);

        await svc.DeleteCustomerAsync(customer.Id);

        var all = await svc.SearchCustomersAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCustomerAsync_NotFound_Throws()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());

        var act = async () => await svc.DeleteCustomerAsync(999);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*999*");
    }
}
