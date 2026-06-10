// SmartSolutions.Data/Entities/PrintOrderVendorAssignment.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrderVendorAssignment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PrintOrder Order { get; set; } = null!;
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public DateTime SentDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal VendorCost { get; set; }
    public bool VendorPaid { get; set; }
    public DateTime? VendorPaidDate { get; set; }
}
