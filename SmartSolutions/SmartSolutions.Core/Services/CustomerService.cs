// SmartSolutions.Core/Services/CustomerService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class CustomerService(IDbContextFactory<AppDbContext> factory, ISessionService session) : ICustomerService
{
    public async Task<List<Customer>> SearchCustomersAsync(string? query = null)
    {
        await using var db = factory.CreateDbContext();
        var q = db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(c => c.Name.Contains(query) || (c.Phone != null && c.Phone.Contains(query)));
        return await q.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Customer> GetCustomerAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        return await db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException($"Customer {id} not found");
    }

    public async Task<Customer> AddCustomerAsync(string name, string? phone, string? address, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new Customer { Name = name, Phone = phone, Address = address, Notes = notes, CreatedById = session.CurrentUser.Id };
        db.Customers.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateCustomerAsync(int id, string name, string? phone, string? address, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException($"Customer {id} not found");
        entity.Name = name; entity.Phone = phone; entity.Address = address; entity.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteCustomerAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException($"Customer {id} not found");
        db.Customers.Remove(entity);
        await db.SaveChangesAsync();
    }
}
