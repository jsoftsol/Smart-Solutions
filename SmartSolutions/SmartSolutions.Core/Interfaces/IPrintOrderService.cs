// SmartSolutions.Core/Interfaces/IPrintOrderService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface IPrintOrderService
{
    Task<List<PrintOrder>> GetOrdersAsync(PrintOrderStatus? status = null, int? customerId = null,
        DateTime? from = null, DateTime? to = null, bool outstandingOnly = false);
    Task<PrintOrder>       GetOrderWithDetailsAsync(int id);
    Task<PrintOrder>       CreateOrderAsync(int customerId, DateTime date, string? notes);
    Task                   UpdateOrderHeaderAsync(int id, int customerId, DateTime date,
        PrintOrderStatus status, decimal? transportationCharges, string? notes);
    Task                   DeleteOrderAsync(int id);

    Task<PrintOrderLine>   AddLineAsync(int orderId, int itemNameId, RateType rateType,
        DimensionUnit unit, decimal? height, decimal? width, int quantity, decimal rate);
    Task                   UpdateLineAsync(int lineId, int itemNameId, RateType rateType,
        DimensionUnit unit, decimal? height, decimal? width, int quantity, decimal rate);
    Task                   DeleteLineAsync(int lineId);

    Task<PrintOrderVendorAssignment> SetVendorAssignmentAsync(int orderId, int vendorId,
        DateTime sentDate, DateTime? expectedDate, decimal vendorCost);
    Task                             MarkVendorPaidAsync(int assignmentId, DateTime paidDate);

    Task<PrintOrderPayment>          AddPaymentAsync(int orderId, decimal amount,
        int channelId, DateTime date, string? notes);
    Task                             DeletePaymentAsync(int paymentId);

    Task<bool> PaymentDuplicateExistsAsync(int orderId, decimal amount, DateTime date);
}
