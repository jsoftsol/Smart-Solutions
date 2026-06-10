// SmartSolutions.Core/Interfaces/IInvoiceService.cs
namespace SmartSolutions.Core.Interfaces;

public interface IInvoiceService
{
    Task PrintInvoiceAsync(int orderId);
    Task SaveInvoiceToPdfAsync(int orderId, string filePath);
}
