// SmartSolutions.App/ViewModels/PrintOrderSummaryRow.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.App.ViewModels;

public class PrintOrderSummaryRow(PrintOrder order)
{
    public PrintOrder Order   { get; } = order;
    public int        Id      => Order.Id;
    public string     Customer => Order.Customer?.Name ?? "";
    public string     DateStr  => Order.Date.ToLocalTime().ToString("dd/MM/yyyy");
    public PrintOrderStatus Status => Order.Status;
    public decimal    Total   => Order.Lines.Sum(l => l.ComputeTotal())
                                 + (Order.TransportationCharges ?? 0);
    public decimal    Paid    => Order.Payments.Sum(p => p.Amount);
    public decimal    Balance => Total - Paid;
    public string?    ExpectedDate =>
        Order.VendorAssignments?.FirstOrDefault()?.ExpectedDate?.ToLocalTime().ToString("dd/MM/yyyy");
}
