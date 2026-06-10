// SmartSolutions.Core/Services/LookupService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class LookupService(IDbContextFactory<AppDbContext> factory) : ILookupService
{
    public async Task<List<ItemCategory>> GetItemCategoriesAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.ItemCategories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<ItemCategory> AddItemCategoryAsync(string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = new ItemCategory { Name = name };
        db.ItemCategories.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task RenameItemCategoryAsync(int id, string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ItemCategories.FindAsync(id)
            ?? throw new InvalidOperationException($"ItemCategory {id} not found");
        entity.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeleteItemCategoryAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ItemCategories.FindAsync(id)
            ?? throw new InvalidOperationException($"ItemCategory {id} not found");
        db.ItemCategories.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<ItemName>> GetItemNamesAsync(int? categoryId = null)
    {
        await using var db = factory.CreateDbContext();
        var q = db.ItemNames.Include(n => n.Category).AsQueryable();
        if (categoryId.HasValue) q = q.Where(n => n.CategoryId == categoryId.Value);
        return await q.OrderBy(n => n.Name).ToListAsync();
    }

    public async Task<ItemName> AddItemNameAsync(string name, int categoryId)
    {
        await using var db = factory.CreateDbContext();
        var entity = new ItemName { Name = name, CategoryId = categoryId };
        db.ItemNames.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task RenameItemNameAsync(int id, string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ItemNames.FindAsync(id)
            ?? throw new InvalidOperationException($"ItemName {id} not found");
        entity.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeleteItemNameAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ItemNames.FindAsync(id)
            ?? throw new InvalidOperationException($"ItemName {id} not found");
        db.ItemNames.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<Vendor>> GetVendorsAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.Vendors.OrderBy(v => v.Name).ToListAsync();
    }

    public async Task<Vendor> AddVendorAsync(string name, string? phone, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new Vendor { Name = name, Phone = phone, Notes = notes };
        db.Vendors.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateVendorAsync(int id, string name, string? phone, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Vendors.FindAsync(id)
            ?? throw new InvalidOperationException($"Vendor {id} not found");
        entity.Name = name; entity.Phone = phone; entity.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteVendorAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Vendors.FindAsync(id)
            ?? throw new InvalidOperationException($"Vendor {id} not found");
        db.Vendors.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<Technician>> GetTechniciansAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.Technicians.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<Technician> AddTechnicianAsync(string name, string? phone)
    {
        await using var db = factory.CreateDbContext();
        var entity = new Technician { Name = name, Phone = phone };
        db.Technicians.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateTechnicianAsync(int id, string name, string? phone)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Technicians.FindAsync(id)
            ?? throw new InvalidOperationException($"Technician {id} not found");
        entity.Name = name; entity.Phone = phone;
        await db.SaveChangesAsync();
    }

    public async Task DeleteTechnicianAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Technicians.FindAsync(id)
            ?? throw new InvalidOperationException($"Technician {id} not found");
        db.Technicians.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.ExpenseCategories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<ExpenseCategory> AddExpenseCategoryAsync(string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = new ExpenseCategory { Name = name };
        db.ExpenseCategories.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task RenameExpenseCategoryAsync(int id, string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ExpenseCategories.FindAsync(id)
            ?? throw new InvalidOperationException($"ExpenseCategory {id} not found");
        entity.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeleteExpenseCategoryAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ExpenseCategories.FindAsync(id)
            ?? throw new InvalidOperationException($"ExpenseCategory {id} not found");
        db.ExpenseCategories.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<PaymentChannel>> GetPaymentChannelsAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.PaymentChannels.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<PaymentChannel> AddPaymentChannelAsync(string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = new PaymentChannel { Name = name };
        db.PaymentChannels.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task RenamePaymentChannelAsync(int id, string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PaymentChannels.FindAsync(id)
            ?? throw new InvalidOperationException($"PaymentChannel {id} not found");
        entity.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeletePaymentChannelAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PaymentChannels.FindAsync(id)
            ?? throw new InvalidOperationException($"PaymentChannel {id} not found");
        db.PaymentChannels.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<BusinessInfo> GetBusinessInfoAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.BusinessInfos.FindAsync(1) ?? new BusinessInfo { Id = 1 };
    }

    public async Task SaveBusinessInfoAsync(BusinessInfo info)
    {
        await using var db = factory.CreateDbContext();
        info.Id = 1;
        if (await db.BusinessInfos.AnyAsync(b => b.Id == 1))
            db.BusinessInfos.Update(info);
        else
            db.BusinessInfos.Add(info);
        await db.SaveChangesAsync();
    }
}
