// SmartSolutions.Data/Entities/HaierJob.cs
namespace SmartSolutions.Data.Entities;

public class HaierJob
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string AcModel { get; set; } = "";
    public string? AcSerial { get; set; }
    public string ProblemDescription { get; set; } = "";
    public int TechnicianId { get; set; }
    public Technician Technician { get; set; } = null!;
    public HaierJobType JobType { get; set; }
    public HaierJobStatus Status { get; set; } = HaierJobStatus.Pending;
    public string? ClaimReferenceNumber { get; set; }  // Warranty jobs only
    public string? PartsUsed { get; set; }
    public decimal PartsCost { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public int?     CreatedById { get; set; }
    public AppUser? CreatedBy   { get; set; }

    public ICollection<HaierJobPayment> Payments { get; set; } = [];
}
