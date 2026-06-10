// SmartSolutions.Core/Interfaces/IDashboardService.cs
namespace SmartSolutions.Core.Interfaces;

public record DayBookEntry(string Description, decimal Amount, string Channel, string Type);
public record BalanceSummary(decimal Cash, decimal Easypaisa, decimal Bank);
public record OutstandingItem(int Id, string Label, string Customer, decimal Total, decimal Paid, decimal Balance, DateTime Date);
public record MonthlySummary(decimal TotalIncome, decimal TotalExpenses, decimal Profit, BalanceSummary Balances);

public interface IDashboardService
{
    Task<List<DayBookEntry>>    GetDayBookAsync(DateTime date);
    Task<BalanceSummary>        GetBalanceSummaryAsync();
    Task<List<OutstandingItem>> GetOutstandingItemsAsync();
    Task<MonthlySummary>        GetMonthlySummaryAsync(int year, int month);
}
