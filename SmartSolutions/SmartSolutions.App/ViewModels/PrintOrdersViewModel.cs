// SmartSolutions.App/ViewModels/PrintOrdersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class PrintOrdersViewModel(
    IPrintOrderService orders,
    IServiceProvider services) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<PrintOrderSummaryRow> _orderList = [];
    [ObservableProperty] private PrintOrderSummaryRow? _selectedOrder;
    [ObservableProperty] private PrintOrderStatus? _filterStatus;
    [ObservableProperty] private bool _filterOutstandingOnly;
    [ObservableProperty] private DateTime? _filterFrom;
    [ObservableProperty] private DateTime? _filterTo;
    [ObservableProperty] private bool _isBusy;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var results = await orders.GetOrdersAsync(
                FilterStatus, null, FilterFrom, FilterTo, FilterOutstandingOnly);
            OrderList = new(results.Select(o => new PrintOrderSummaryRow(o)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync() => await LoadAsync();

    [RelayCommand]
    private void ClearFilters()
    {
        FilterStatus = null;
        FilterOutstandingOnly = false;
        FilterFrom = FilterTo = null;
    }

    [RelayCommand]
    private void OpenNewOrder()
    {
        var vm = services.GetRequiredService<PrintOrderDetailViewModel>();
        vm.InitNew();
        NavigateTo(vm);
    }

    [RelayCommand]
    private void OpenOrder(PrintOrderSummaryRow row)
    {
        var vm = services.GetRequiredService<PrintOrderDetailViewModel>();
        vm.InitEdit(row.Order.Id);
        NavigateTo(vm);
    }

    [RelayCommand]
    private async Task DeleteOrderAsync(PrintOrderSummaryRow row)
    {
        if (!DialogHelper.Confirm($"Delete order #{row.Id}?")) return;
        await orders.DeleteOrderAsync(row.Order.Id);
        OrderList.Remove(row);
    }

    private void NavigateTo(ObservableObject vm)
    {
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Print Orders";
        main.CurrentView = vm;
    }
}
