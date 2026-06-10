// SmartSolutions.Core/Services/DashboardService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class DashboardService(IDbContextFactory<AppDbContext> factory) : IDashboardService
{
    public async Task<List<DayBookEntry>> GetDayBookAsync(DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var dayStart = date.Date.ToUniversalTime();
        var dayEnd   = dayStart.AddDays(1);
        var entries = new List<DayBookEntry>();

        var printPayments = await db.PrintOrderPayments
            .Include(p => p.Order).ThenInclude(o => o.Customer)
            .Include(p => p.Channel)
            .Where(p => p.Date >= dayStart && p.Date < dayEnd).ToListAsync();
        entries.AddRange(printPayments.Select(p =>
            new DayBookEntry($"Print Order #{p.OrderId} — {p.Order.Customer.Name}",
                p.Amount, p.Channel.Name, "Income")));

        var jobPayments = await db.HaierJobPayments
            .Include(p => p.Job).ThenInclude(j => j.Customer)
            .Include(p => p.Channel)
            .Where(p => p.Date >= dayStart && p.Date < dayEnd).ToListAsync();
        entries.AddRange(jobPayments.Select(p =>
            new DayBookEntry($"Haier Job #{p.JobId} — {p.Job.Customer.Name}",
                p.Amount, p.Channel.Name, "Income")));

        var expenses = await db.Expenses
            .Include(e => e.Category).Include(e => e.Channel)
            .Where(e => e.Date >= dayStart && e.Date < dayEnd).ToListAsync();
        entries.AddRange(expenses.Select(e =>
            new DayBookEntry(e.Description ?? e.Category.Name,
                e.Amount, e.Channel.Name, "Expense")));

        return entries;
    }

    public async Task<BalanceSummary> GetBalanceSummaryAsync()
    {
        await using var db = factory.CreateDbContext();
        var channels = await db.PaymentChannels.ToListAsync();

        var printByChannel = await db.PrintOrderPayments
            .GroupBy(p => p.ChannelId)
            .Select(g => new { g.Key, Total = g.Sum(p => p.Amount) }).ToListAsync();
        var jobByChannel = await db.HaierJobPayments
            .GroupBy(p => p.ChannelId)
            .Select(g => new { g.Key, Total = g.Sum(p => p.Amount) }).ToListAsync();
        var expenseByChannel = await db.Expenses
            .GroupBy(e => e.ChannelId)
            .Select(g => new { g.Key, Total = g.Sum(e => e.Amount) }).ToListAsync();

        decimal Balance(int id) =>
            (printByChannel.FirstOrDefault(x => x.Key == id)?.Total ?? 0) +
            (jobByChannel.FirstOrDefault(x => x.Key == id)?.Total ?? 0) -
            (expenseByChannel.FirstOrDefault(x => x.Key == id)?.Total ?? 0);

        var cash      = channels.FirstOrDefault(c => c.Name == "Cash");
        var easypaisa = channels.FirstOrDefault(c => c.Name == "Easypaisa");
        var bank      = channels.FirstOrDefault(c => c.Name == "Bank");

        return new BalanceSummary(
            cash      is null ? 0 : Balance(cash.Id),
            easypaisa is null ? 0 : Balance(easypaisa.Id),
            bank      is null ? 0 : Balance(bank.Id));
    }

    public async Task<List<OutstandingItem>> GetOutstandingItemsAsync()
    {
        await using var db = factory.CreateDbContext();
        var items = new List<OutstandingItem>();

        var orders = await db.PrintOrders
            .Include(o => o.Customer).Include(o => o.Lines).Include(o => o.Payments)
            .ToListAsync();
        foreach (var o in orders)
        {
            var total = o.Lines.Sum(l => l.ComputeTotal()) + (o.TransportationCharges ?? 0);
            var paid  = o.Payments.Sum(p => p.Amount);
            if (paid < total)
                items.Add(new OutstandingItem(o.Id, $"Print Order #{o.Id}",
                    o.Customer.Name, total, paid, total - paid, o.Date));
        }

        var jobs = await db.HaierJobs
            .Include(j => j.Customer).Include(j => j.Payments)
            .Where(j => j.JobType == HaierJobType.OutOfWarranty).ToListAsync();
        foreach (var j in jobs)
        {
            var paid = j.Payments.Sum(p => p.Amount);
            if (paid < j.PartsCost)
                items.Add(new OutstandingItem(j.Id, $"Haier Job #{j.Id}",
                    j.Customer.Name, j.PartsCost, paid, j.PartsCost - paid, j.Date));
        }

        return items.OrderBy(i => i.Date).ToList();
    }

    public async Task<MonthlySummary> GetMonthlySummaryAsync(int year, int month)
    {
        await using var db = factory.CreateDbContext();
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = from.AddMonths(1);

        var printIncome = await db.PrintOrderPayments
            .Where(p => p.Date >= from && p.Date < to).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var jobIncome = await db.HaierJobPayments
            .Where(p => p.Date >= from && p.Date < to).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var expenses = await db.Expenses
            .Where(e => e.Date >= from && e.Date < to).SumAsync(e => (decimal?)e.Amount) ?? 0;

        var balances = await GetBalanceSummaryAsync();
        return new MonthlySummary(printIncome + jobIncome, expenses,
            printIncome + jobIncome - expenses, balances);
    }
}
