// SmartSolutions.Core/Services/ExpenseService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class ExpenseService(IDbContextFactory<AppDbContext> factory, ISessionService session) : IExpenseService
{
    public async Task<List<Expense>> GetExpensesAsync(int? categoryId = null,
        DateTime? from = null, DateTime? to = null)
    {
        await using var db = factory.CreateDbContext();
        var q = db.Expenses.Include(e => e.Category).Include(e => e.Channel).AsQueryable();
        if (categoryId.HasValue) q = q.Where(e => e.CategoryId == categoryId.Value);
        if (from.HasValue)       q = q.Where(e => e.Date >= from.Value);
        if (to.HasValue)         q = q.Where(e => e.Date <= to.Value);
        return await q.OrderByDescending(e => e.Date).ToListAsync();
    }

    public async Task<Expense> AddExpenseAsync(int categoryId, string? description,
        decimal amount, int channelId, DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var entity = new Expense
        {
            CategoryId  = categoryId,
            Description = description,
            Amount      = amount,
            ChannelId   = channelId,
            Date        = date.ToUniversalTime(),
            CreatedById = session.CurrentUser.Id
        };
        db.Expenses.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateExpenseAsync(int id, int categoryId, string? description,
        decimal amount, int channelId, DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Expenses.FindAsync(id)
            ?? throw new InvalidOperationException($"Expense {id} not found");
        entity.CategoryId = categoryId; entity.Description = description;
        entity.Amount = amount; entity.ChannelId = channelId; entity.Date = date.ToUniversalTime();
        await db.SaveChangesAsync();
    }

    public async Task DeleteExpenseAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Expenses.FindAsync(id)
            ?? throw new InvalidOperationException($"Expense {id} not found");
        db.Expenses.Remove(entity);
        await db.SaveChangesAsync();
    }
}
