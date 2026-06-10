// SmartSolutions.App/ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.Core.Interfaces;

namespace SmartSolutions.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly ISessionService  _session;

    public MainViewModel(IServiceProvider services, ISessionService session)
    {
        _services = services;
        _session  = session;
        NavigateToDashboard();
    }

    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private string _currentSection = "Dashboard";

    public string LoggedInUsername => _session.CurrentUser.Username;

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentSection = "Dashboard";
        CurrentView = _services.GetRequiredService<DashboardViewModel>();
        _ = ((DashboardViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToPrintOrders()
    {
        CurrentSection = "Print Orders";
        CurrentView = _services.GetRequiredService<PrintOrdersViewModel>();
        _ = ((PrintOrdersViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToHaierJobs()
    {
        CurrentSection = "Haier Jobs";
        CurrentView = _services.GetRequiredService<HaierJobsViewModel>();
        _ = ((HaierJobsViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToExpenses()
    {
        CurrentSection = "Expenses";
        CurrentView = _services.GetRequiredService<ExpensesViewModel>();
        _ = ((ExpensesViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        CurrentSection = "Reports";
        CurrentView = _services.GetRequiredService<ReportsViewModel>();
        _ = ((ReportsViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToItems()
    {
        CurrentSection = "Items";
        CurrentView = _services.GetRequiredService<ItemsViewModel>();
        _ = ((ItemsViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToCustomers()
    {
        CurrentSection = "Customers";
        CurrentView = _services.GetRequiredService<CustomersViewModel>();
        _ = ((CustomersViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToVendors()
    {
        CurrentSection = "Vendors";
        CurrentView = _services.GetRequiredService<VendorsViewModel>();
        _ = ((VendorsViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToTechnicians()
    {
        CurrentSection = "Technicians";
        CurrentView = _services.GetRequiredService<TechniciansViewModel>();
        _ = ((TechniciansViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToExpenseCategories()
    {
        CurrentSection = "Expense Categories";
        CurrentView = _services.GetRequiredService<ExpenseCategoriesViewModel>();
        _ = ((ExpenseCategoriesViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToPaymentChannels()
    {
        CurrentSection = "Payment Channels";
        CurrentView = _services.GetRequiredService<PaymentChannelsViewModel>();
        _ = ((PaymentChannelsViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToUsers()
    {
        CurrentSection = "Users";
        CurrentView = _services.GetRequiredService<UsersViewModel>();
        _ = ((UsersViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentSection = "Settings";
        CurrentView = _services.GetRequiredService<SettingsViewModel>();
        _ = ((SettingsViewModel)CurrentView!).LoadAsync();
    }
}
