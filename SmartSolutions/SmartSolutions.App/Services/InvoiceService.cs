// SmartSolutions.App/Services/InvoiceService.cs
using FastReport;
using FastReport.Export.PdfSimple;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace SmartSolutions.App.Services;

public class InvoiceService(
    IPrintOrderService orderService,
    ILookupService lookupService) : IInvoiceService
{
    private static string TemplatePath =>
        Path.Combine(AppContext.BaseDirectory, "Reports", "Invoice.frx");

    public async Task PrintInvoiceAsync(int orderId)
    {
        // OpenSource edition has no GUI preview window — save to temp PDF and open with system viewer
        var tempPath = Path.Combine(Path.GetTempPath(), $"Invoice_{orderId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        await SaveInvoiceToPdfAsync(orderId, tempPath);
        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
    }

    public async Task SaveInvoiceToPdfAsync(int orderId, string filePath)
    {
        using var report = await BuildReportAsync(orderId);
        var export = new PDFSimpleExport();
        report.Export(export, filePath);
    }

    private async Task<Report> BuildReportAsync(int orderId)
    {
        var order   = await orderService.GetOrderWithDetailsAsync(orderId);
        var bizInfo = await lookupService.GetBusinessInfoAsync();

        var report = new Report();
        report.Load(TemplatePath);

        report.SetParameterValue("OrderId",          order.Id);
        report.SetParameterValue("OrderDate",         order.Date.ToLocalTime().ToString("dd/MM/yyyy"));
        report.SetParameterValue("CustomerName",      order.Customer.Name);
        report.SetParameterValue("CustomerAddress",   order.Customer.Address ?? "");
        report.SetParameterValue("BizName",    bizInfo.Name);
        report.SetParameterValue("BizNtn",     bizInfo.Ntn);
        report.SetParameterValue("BizAddress", bizInfo.Address ?? "");
        report.SetParameterValue("BizPhone1",  bizInfo.Phone1 ?? "");
        report.SetParameterValue("BizPhone2",  bizInfo.Phone2 ?? "");
        report.SetParameterValue("BizEmail",   bizInfo.Email  ?? "");

        var subtotal  = order.Lines.Sum(l => l.ComputeTotal());
        var transport = order.TransportationCharges ?? 0;
        var total     = subtotal + transport;
        var paid      = order.Payments.Sum(p => p.Amount);

        report.SetParameterValue("Subtotal",              subtotal);
        report.SetParameterValue("TransportationCharges", transport);
        report.SetParameterValue("OrderTotal",            total);
        report.SetParameterValue("TotalPaid",             paid);
        report.SetParameterValue("Balance",               total - paid);

        var dt = new DataTable("Lines");
        dt.Columns.Add("No",          typeof(int));
        dt.Columns.Add("Description", typeof(string));
        dt.Columns.Add("Qty",         typeof(int));
        dt.Columns.Add("Rate",        typeof(decimal));
        dt.Columns.Add("Total",       typeof(decimal));

        int i = 1;
        foreach (var line in order.Lines)
        {
            var desc = line.RateType == RateType.PerSqft
                ? $"{line.ItemName.Name} {line.Height}x{line.Width} {line.Unit}"
                : line.ItemName.Name;
            dt.Rows.Add(i++, desc, line.Quantity, line.Rate, line.ComputeTotal());
        }
        if (transport > 0)
            dt.Rows.Add(i, "Transportation Charges", 1, transport, transport);

        report.RegisterData(dt, "Lines");
        await report.PrepareAsync();
        return report;
    }
}
