// SmartSolutions.Core/Interfaces/IHaierJobService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface IHaierJobService
{
    Task<List<HaierJob>> GetJobsAsync(HaierJobStatus? status = null,
        HaierJobType? jobType = null, int? technicianId = null,
        DateTime? from = null, DateTime? to = null);
    Task<HaierJob>       GetJobWithDetailsAsync(int id);
    Task<HaierJob>       CreateJobAsync(int customerId, string acModel, string? acSerial,
        string problemDescription, int technicianId, HaierJobType jobType,
        string? claimReferenceNumber, string? partsUsed, decimal partsCost,
        DateTime date, string? notes);
    Task                 UpdateJobAsync(int id, int customerId, string acModel, string? acSerial,
        string problemDescription, int technicianId, HaierJobType jobType,
        HaierJobStatus status, string? claimReferenceNumber, string? partsUsed,
        decimal partsCost, DateTime date, string? notes);
    Task                 DeleteJobAsync(int id);

    Task<HaierJobPayment> AddPaymentAsync(int jobId, decimal amount,
        int channelId, DateTime date, string? notes);
    Task                  DeletePaymentAsync(int paymentId);
    Task<bool>            PaymentDuplicateExistsAsync(int jobId, decimal amount, DateTime date);
}
