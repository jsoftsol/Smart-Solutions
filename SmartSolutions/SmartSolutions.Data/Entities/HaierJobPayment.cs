// SmartSolutions.Data/Entities/HaierJobPayment.cs
namespace SmartSolutions.Data.Entities;

public class HaierJobPayment
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public HaierJob Job { get; set; } = null!;
    public decimal Amount { get; set; }
    public int ChannelId { get; set; }
    public PaymentChannel Channel { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public int?     RecordedById { get; set; }
    public AppUser? RecordedBy   { get; set; }
}
