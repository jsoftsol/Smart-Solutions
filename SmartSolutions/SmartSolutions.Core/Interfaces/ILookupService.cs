// SmartSolutions.Core/Interfaces/ILookupService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface ILookupService
{
    Task<List<ItemCategory>> GetItemCategoriesAsync();
    Task<ItemCategory>       AddItemCategoryAsync(string name);
    Task                     RenameItemCategoryAsync(int id, string name);
    Task                     DeleteItemCategoryAsync(int id);

    Task<List<ItemName>> GetItemNamesAsync(int? categoryId = null);
    Task<ItemName>       AddItemNameAsync(string name, int categoryId);
    Task                 RenameItemNameAsync(int id, string name);
    Task                 DeleteItemNameAsync(int id);

    Task<List<Vendor>> GetVendorsAsync();
    Task<Vendor>       AddVendorAsync(string name, string? phone, string? notes);
    Task               UpdateVendorAsync(int id, string name, string? phone, string? notes);
    Task               DeleteVendorAsync(int id);

    Task<List<Technician>> GetTechniciansAsync();
    Task<Technician>       AddTechnicianAsync(string name, string? phone);
    Task                   UpdateTechnicianAsync(int id, string name, string? phone);
    Task                   DeleteTechnicianAsync(int id);

    Task<List<ExpenseCategory>> GetExpenseCategoriesAsync();
    Task<ExpenseCategory>       AddExpenseCategoryAsync(string name);
    Task                        RenameExpenseCategoryAsync(int id, string name);
    Task                        DeleteExpenseCategoryAsync(int id);

    Task<List<PaymentChannel>> GetPaymentChannelsAsync();
    Task<PaymentChannel>       AddPaymentChannelAsync(string name);
    Task                       RenamePaymentChannelAsync(int id, string name);
    Task                       DeletePaymentChannelAsync(int id);

    Task<BusinessInfo> GetBusinessInfoAsync();
    Task               SaveBusinessInfoAsync(BusinessInfo info);
}
