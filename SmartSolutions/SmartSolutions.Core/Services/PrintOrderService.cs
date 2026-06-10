// SmartSolutions.Core/Services/PrintOrderService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class PrintOrderService(IDbContextFactory<AppDbContext> factory, ISessionService session) : IPrintOrderService
{
    public async Task<List<PrintOrder>> GetOrdersAsync(PrintOrderStatus? status = null,
        int? customerId = null, DateTime? from = null, DateTime? to = null,
        bool outstandingOnly = false)
    {
        await using var db = factory.CreateDbContext();
        var q = db.PrintOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .Include(o => o.Payments)
            .Include(o => o.VendorAssignments)
            .AsQueryable();
        if (status.HasValue)     q = q.Where(o => o.Status == status.Value);
        if (customerId.HasValue) q = q.Where(o => o.CustomerId == customerId.Value);
        if (from.HasValue)       q = q.Where(o => o.Date >= from.Value);
        if (to.HasValue)         q = q.Where(o => o.Date <= to.Value);
        var orders = await q.OrderByDescending(o => o.Date).ToListAsync();
        if (outstandingOnly)
            orders = orders.Where(o =>
                o.Payments.Sum(p => p.Amount) <
                o.Lines.Sum(l => l.ComputeTotal()) + (o.TransportationCharges ?? 0)).ToList();
        return orders;
    }

    public async Task<PrintOrder> GetOrderWithDetailsAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        return await db.PrintOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.ItemName).ThenInclude(n => n.Category)
            .Include(o => o.VendorAssignments).ThenInclude(a => a.Vendor)
            .Include(o => o.Payments).ThenInclude(p => p.Channel)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new InvalidOperationException($"PrintOrder {id} not found");
    }

    public async Task<PrintOrder> CreateOrderAsync(int customerId, DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new PrintOrder
        {
            CustomerId  = customerId, Date = date.ToUniversalTime(),
            Status      = PrintOrderStatus.Draft, Notes = notes,
            CreatedById = session.CurrentUser.Id
        };
        db.PrintOrders.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateOrderHeaderAsync(int id, int customerId, DateTime date,
        PrintOrderStatus status, decimal? transportationCharges, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"PrintOrder {id} not found");
        entity.CustomerId = customerId; entity.Date = date.ToUniversalTime();
        entity.Status = status; entity.TransportationCharges = transportationCharges; entity.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteOrderAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"PrintOrder {id} not found");
        db.PrintOrders.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<PrintOrderLine> AddLineAsync(int orderId, int itemNameId,
        RateType rateType, DimensionUnit unit, decimal? height, decimal? width,
        int quantity, decimal rate)
    {
        await using var db = factory.CreateDbContext();
        var entity = new PrintOrderLine
        {
            OrderId = orderId, ItemNameId = itemNameId, RateType = rateType, Unit = unit,
            Height = rateType == RateType.PerPiece ? null : height,
            Width  = rateType == RateType.PerPiece ? null : width,
            Quantity = quantity, Rate = rate
        };
        db.PrintOrderLines.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateLineAsync(int lineId, int itemNameId, RateType rateType,
        DimensionUnit unit, decimal? height, decimal? width, int quantity, decimal rate)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrderLines.FindAsync(lineId)
            ?? throw new InvalidOperationException($"PrintOrderLine {lineId} not found");
        entity.ItemNameId = itemNameId; entity.RateType = rateType; entity.Unit = unit;
        entity.Height = rateType == RateType.PerPiece ? null : height;
        entity.Width  = rateType == RateType.PerPiece ? null : width;
        entity.Quantity = quantity; entity.Rate = rate;
        await db.SaveChangesAsync();
    }

    public async Task DeleteLineAsync(int lineId)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrderLines.FindAsync(lineId)
            ?? throw new InvalidOperationException($"PrintOrderLine {lineId} not found");
        db.PrintOrderLines.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<PrintOrderVendorAssignment> SetVendorAssignmentAsync(int orderId,
        int vendorId, DateTime sentDate, DateTime? expectedDate, decimal vendorCost)
    {
        await using var db = factory.CreateDbContext();
        var existing = await db.PrintOrderVendorAssignments.FirstOrDefaultAsync(a => a.OrderId == orderId);
        if (existing is not null)
        {
            existing.VendorId = vendorId; existing.SentDate = sentDate.ToUniversalTime();
            existing.ExpectedDate = expectedDate?.ToUniversalTime(); existing.VendorCost = vendorCost;
            await db.SaveChangesAsync();
            return existing;
        }
        var entity = new PrintOrderVendorAssignment
        {
            OrderId = orderId, VendorId = vendorId, SentDate = sentDate.ToUniversalTime(),
            ExpectedDate = expectedDate?.ToUniversalTime(), VendorCost = vendorCost
        };
        db.PrintOrderVendorAssignments.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task MarkVendorPaidAsync(int assignmentId, DateTime paidDate)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrderVendorAssignments.FindAsync(assignmentId)
            ?? throw new InvalidOperationException($"VendorAssignment {assignmentId} not found");
        entity.VendorPaid = true; entity.VendorPaidDate = paidDate.ToUniversalTime();
        await db.SaveChangesAsync();
    }

    public async Task<PrintOrderPayment> AddPaymentAsync(int orderId, decimal amount,
        int channelId, DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new PrintOrderPayment
        {
            OrderId      = orderId, Amount = amount, ChannelId = channelId,
            Date         = date.ToUniversalTime(), Notes = notes,
            RecordedById = session.CurrentUser.Id
        };
        db.PrintOrderPayments.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeletePaymentAsync(int paymentId)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrderPayments.FindAsync(paymentId)
            ?? throw new InvalidOperationException($"PrintOrderPayment {paymentId} not found");
        db.PrintOrderPayments.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<bool> PaymentDuplicateExistsAsync(int orderId, decimal amount, DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var dayStart = date.Date.ToUniversalTime();
        var dayEnd   = dayStart.AddDays(1);
        return await db.PrintOrderPayments.AnyAsync(p =>
            p.OrderId == orderId && p.Amount == amount &&
            p.Date >= dayStart && p.Date < dayEnd);
    }
}
