// SmartSolutions.App/ViewModels/ReportsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class ReportsViewModel(
    IDashboardService dashboard,
    IDbContextFactory<AppDbContext> factory) : ObservableObject
{
    // Day Book
    [ObservableProperty] private DateTime _dayBookDate = DateTime.Today;
    [ObservableProperty] private ObservableCollection<DayBookEntry> _dayBookEntries = [];
    [ObservableProperty] private decimal _dayBookIncomeTotal;
    [ObservableProperty] private decimal _dayBookExpenseTotal;

    // Outstanding
    [ObservableProperty] private ObservableCollection<OutstandingItem> _outstandingItems = [];
    [ObservableProperty] private decimal _outstandingTotal;

    // Monthly
    [ObservableProperty] private int _reportMonth = DateTime.Today.Month;
    [ObservableProperty] private int _reportYear  = DateTime.Today.Year;
    [ObservableProperty] private MonthlySummary? _monthly;

    // Per-Order Profit
    [ObservableProperty] private int _profitOrderId;
    [ObservableProperty] private string _perOrderProfitResult = "";

    // Expense Breakdown
    [ObservableProperty] private int _expBreakMonth = DateTime.Today.Month;
    [ObservableProperty] private int _expBreakYear  = DateTime.Today.Year;
    [ObservableProperty] private ObservableCollection<ExpenseBreakdownRow> _expenseBreakdown = [];
    [ObservableProperty] private bool _isBusy;

    public Task LoadAsync() => Task.CompletedTask;

    [RelayCommand]
    private async Task LoadDayBookAsync()
    {
        IsBusy = true;
        try
        {
            var entries = await dashboard.GetDayBookAsync(DayBookDate);
            DayBookEntries      = new(entries);
            DayBookIncomeTotal  = DayBookEntries.Where(e => e.Type == "Income").Sum(e => e.Amount);
            DayBookExpenseTotal = DayBookEntries.Where(e => e.Type == "Expense").Sum(e => e.Amount);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadOutstandingAsync()
    {
        IsBusy = true;
        try
        {
            var items = await dashboard.GetOutstandingItemsAsync();
            OutstandingItems = new(items);
            OutstandingTotal  = OutstandingItems.Sum(i => i.Balance);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadMonthlyAsync()
    {
        IsBusy = true;
        try { Monthly = await dashboard.GetMonthlySummaryAsync(ReportYear, ReportMonth); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadPerOrderProfitAsync()
    {
        if (ProfitOrderId <= 0) { PerOrderProfitResult = "Enter a valid order ID."; return; }
        IsBusy = true;
        try
        {
            await using var db = factory.CreateDbContext();
            var order = await db.PrintOrders
                .Include(o => o.Lines).ThenInclude(l => l.ItemName)
                .Include(o => o.VendorAssignments)
                .FirstOrDefaultAsync(o => o.Id == ProfitOrderId);
            if (order is null)
            {
                PerOrderProfitResult = $"Order #{ProfitOrderId} not found.";
                return;
            }
            var revenue    = order.Lines.Sum(l => l.ComputeTotal()) + (order.TransportationCharges ?? 0);
            var vendorCost = order.VendorAssignments.Sum(a => a.VendorCost);
            var profit     = revenue - vendorCost;
            PerOrderProfitResult = $"Order #{ProfitOrderId}: Revenue PKR {revenue:#,##0.00} — Vendor Cost PKR {vendorCost:#,##0.00} = Profit PKR {profit:#,##0.00}";
        }
        catch (Exception ex) { PerOrderProfitResult = $"Error: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadExpenseBreakdownAsync()
    {
        IsBusy = true;
        try
        {
            await using var db = factory.CreateDbContext();
            var from = new DateTime(ExpBreakYear, ExpBreakMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var to   = from.AddMonths(1);
            var rows = await db.Expenses
                .Include(e => e.Category)
                .Where(e => e.Date >= from && e.Date < to)
                .GroupBy(e => e.Category.Name)
                .Select(g => new ExpenseBreakdownRow(g.Key, g.Sum(e => e.Amount)))
                .ToListAsync();
            ExpenseBreakdown = new(rows.OrderByDescending(r => r.Total));
        }
        finally { IsBusy = false; }
    }
}

public record ExpenseBreakdownRow(string Category, decimal Total);
