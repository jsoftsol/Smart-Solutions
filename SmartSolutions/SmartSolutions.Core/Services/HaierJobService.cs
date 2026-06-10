// SmartSolutions.Core/Services/HaierJobService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class HaierJobService(IDbContextFactory<AppDbContext> factory, ISessionService session) : IHaierJobService
{
    public async Task<List<HaierJob>> GetJobsAsync(HaierJobStatus? status = null,
        HaierJobType? jobType = null, int? technicianId = null,
        DateTime? from = null, DateTime? to = null)
    {
        await using var db = factory.CreateDbContext();
        var q = db.HaierJobs
            .Include(j => j.Customer)
            .Include(j => j.Technician)
            .Include(j => j.Payments)
            .AsQueryable();
        if (status.HasValue)       q = q.Where(j => j.Status == status.Value);
        if (jobType.HasValue)      q = q.Where(j => j.JobType == jobType.Value);
        if (technicianId.HasValue) q = q.Where(j => j.TechnicianId == technicianId.Value);
        if (from.HasValue)         q = q.Where(j => j.Date >= from.Value);
        if (to.HasValue)           q = q.Where(j => j.Date <= to.Value);
        return await q.OrderByDescending(j => j.Date).ToListAsync();
    }

    public async Task<HaierJob> GetJobWithDetailsAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        return await db.HaierJobs
            .Include(j => j.Customer)
            .Include(j => j.Technician)
            .Include(j => j.Payments).ThenInclude(p => p.Channel)
            .FirstOrDefaultAsync(j => j.Id == id)
            ?? throw new InvalidOperationException($"HaierJob {id} not found");
    }

    public async Task<HaierJob> CreateJobAsync(int customerId, string acModel, string? acSerial,
        string problemDescription, int technicianId, HaierJobType jobType,
        string? claimReferenceNumber, string? partsUsed, decimal partsCost,
        DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new HaierJob
        {
            CustomerId           = customerId, AcModel = acModel, AcSerial = acSerial,
            ProblemDescription   = problemDescription, TechnicianId = technicianId,
            JobType              = jobType, Status = HaierJobStatus.Pending,
            ClaimReferenceNumber = claimReferenceNumber,
            PartsUsed            = partsUsed, PartsCost = partsCost,
            Date                 = date.ToUniversalTime(), Notes = notes,
            CreatedById          = session.CurrentUser.Id
        };
        db.HaierJobs.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateJobAsync(int id, int customerId, string acModel, string? acSerial,
        string problemDescription, int technicianId, HaierJobType jobType,
        HaierJobStatus status, string? claimReferenceNumber, string? partsUsed,
        decimal partsCost, DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.HaierJobs.FindAsync(id)
            ?? throw new InvalidOperationException($"HaierJob {id} not found");
        entity.CustomerId = customerId; entity.AcModel = acModel; entity.AcSerial = acSerial;
        entity.ProblemDescription = problemDescription; entity.TechnicianId = technicianId;
        entity.JobType = jobType; entity.Status = status;
        entity.ClaimReferenceNumber = claimReferenceNumber;
        entity.PartsUsed = partsUsed; entity.PartsCost = partsCost;
        entity.Date = date.ToUniversalTime(); entity.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteJobAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.HaierJobs.FindAsync(id)
            ?? throw new InvalidOperationException($"HaierJob {id} not found");
        db.HaierJobs.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<HaierJobPayment> AddPaymentAsync(int jobId, decimal amount,
        int channelId, DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new HaierJobPayment
        {
            JobId        = jobId, Amount = amount, ChannelId = channelId,
            Date         = date.ToUniversalTime(), Notes = notes,
            RecordedById = session.CurrentUser.Id
        };
        db.HaierJobPayments.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeletePaymentAsync(int paymentId)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.HaierJobPayments.FindAsync(paymentId)
            ?? throw new InvalidOperationException($"HaierJobPayment {paymentId} not found");
        db.HaierJobPayments.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<bool> PaymentDuplicateExistsAsync(int jobId, decimal amount, DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var dayStart = date.Date.ToUniversalTime();
        var dayEnd   = dayStart.AddDays(1);
        return await db.HaierJobPayments.AnyAsync(p =>
            p.JobId == jobId && p.Amount == amount &&
            p.Date >= dayStart && p.Date < dayEnd);
    }
}
