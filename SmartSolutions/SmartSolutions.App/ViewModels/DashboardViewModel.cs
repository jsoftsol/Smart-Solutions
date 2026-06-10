// SmartSolutions.App/ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class DashboardViewModel(IDashboardService dashboard) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<DayBookEntry> _dayBookEntries = [];
    [ObservableProperty] private DateTime _dayBookDate = DateTime.Today;
    [ObservableProperty] private BalanceSummary _balanceSummary = new(0, 0, 0);
    [ObservableProperty] private ObservableCollection<OutstandingItem> _outstandingItems = [];
    [ObservableProperty] private MonthlySummary? _monthlySummary;
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private int _selectedYear  = DateTime.Today.Year;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _hasErrorMessage;

    partial void OnErrorMessageChanged(string value) => HasErrorMessage = !string.IsNullOrEmpty(value);

    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            DayBookEntries   = new(await dashboard.GetDayBookAsync(DayBookDate));
            BalanceSummary   = await dashboard.GetBalanceSummaryAsync();
            OutstandingItems = new(await dashboard.GetOutstandingItemsAsync());
            MonthlySummary   = await dashboard.GetMonthlySummaryAsync(SelectedYear, SelectedMonth);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Dashboard load failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshDayBookAsync()
    {
        try
        {
            DayBookEntries = new(await dashboard.GetDayBookAsync(DayBookDate));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Day book refresh failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshMonthlyAsync()
    {
        try
        {
            MonthlySummary = await dashboard.GetMonthlySummaryAsync(SelectedYear, SelectedMonth);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Monthly refresh failed: {ex.Message}";
        }
    }
}
