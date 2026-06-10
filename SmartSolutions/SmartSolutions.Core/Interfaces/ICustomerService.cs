// SmartSolutions.Core/Interfaces/ICustomerService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> SearchCustomersAsync(string? query = null);
    Task<Customer>       GetCustomerAsync(int id);
    Task<Customer>       AddCustomerAsync(string name, string? phone, string? address, string? notes);
    Task                 UpdateCustomerAsync(int id, string name, string? phone, string? address, string? notes);
    Task                 DeleteCustomerAsync(int id);
}
