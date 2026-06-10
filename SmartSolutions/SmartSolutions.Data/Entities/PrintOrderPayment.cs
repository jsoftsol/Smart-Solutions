// SmartSolutions.Data/Entities/PrintOrderPayment.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrderPayment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PrintOrder Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public int ChannelId { get; set; }
    public PaymentChannel Channel { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public int?     RecordedById { get; set; }
    public AppUser? RecordedBy   { get; set; }
}
