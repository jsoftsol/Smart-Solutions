// SmartSolutions.Core/Interfaces/IExpenseService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(int? categoryId = null,
        DateTime? from = null, DateTime? to = null);
    Task<Expense>       AddExpenseAsync(int categoryId, string? description,
        decimal amount, int channelId, DateTime date);
    Task                UpdateExpenseAsync(int id, int categoryId, string? description,
        decimal amount, int channelId, DateTime date);
    Task                DeleteExpenseAsync(int id);
}
