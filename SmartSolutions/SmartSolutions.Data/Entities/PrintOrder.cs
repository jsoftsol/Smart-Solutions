// SmartSolutions.Data/Entities/PrintOrder.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime Date { get; set; }
    public PrintOrderStatus Status { get; set; } = PrintOrderStatus.Draft;
    public decimal? TransportationCharges { get; set; }
    public string? Notes { get; set; }
    public int?     CreatedById { get; set; }
    public AppUser? CreatedBy   { get; set; }

    public ICollection<PrintOrderLine>             Lines             { get; set; } = [];
    public ICollection<PrintOrderVendorAssignment> VendorAssignments { get; set; } = [];
    public ICollection<PrintOrderPayment>          Payments          { get; set; } = [];
}
