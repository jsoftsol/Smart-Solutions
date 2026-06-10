// SmartSolutions.App/ViewModels/ExpensesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class ExpensesViewModel(
    IExpenseService expenseService,
    ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<Expense> _expenses = [];
    [ObservableProperty] private ObservableCollection<ExpenseCategory> _categories = [];
    [ObservableProperty] private ObservableCollection<PaymentChannel> _channels = [];
    [ObservableProperty] private ExpenseCategory? _filterCategory;
    [ObservableProperty] private DateTime? _filterFrom;
    [ObservableProperty] private DateTime? _filterTo;

    // Add form
    [ObservableProperty] private ExpenseCategory? _newCategory;
    [ObservableProperty] private string _newDescription = "";
    [ObservableProperty] private decimal _newAmount;
    [ObservableProperty] private PaymentChannel? _newChannel;
    [ObservableProperty] private DateTime _newDate = DateTime.Today;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isBusy;

    public decimal ShowingTotal => Expenses.Sum(e => e.Amount);

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Categories = new(await lookup.GetExpenseCategoriesAsync());
            Channels   = new(await lookup.GetPaymentChannelsAsync());
            var results = await expenseService.GetExpensesAsync(FilterCategory?.Id, FilterFrom, FilterTo);
            Expenses = new(results);
            OnPropertyChanged(nameof(ShowingTotal));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private async Task ApplyFiltersAsync() => await LoadAsync();

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (NewCategory is null)  { SetError("Category is required."); return; }
        if (NewAmount <= 0)       { SetError("Amount must be > 0.");   return; }
        if (NewChannel is null)   { SetError("Channel is required.");  return; }
        ClearError();

        IsBusy = true;
        try
        {
            var expense = await expenseService.AddExpenseAsync(
                NewCategory.Id,
                string.IsNullOrWhiteSpace(NewDescription) ? null : NewDescription.Trim(),
                NewAmount, NewChannel.Id, NewDate.ToUniversalTime());
            expense.Category = NewCategory;
            expense.Channel  = NewChannel;
            Expenses.Insert(0, expense);
            NewAmount = 0;
            NewDescription = "";
            OnPropertyChanged(nameof(ShowingTotal));
        }
        catch (Exception ex)
        {
            SetError($"Add failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(Expense expense)
    {
        if (!DialogHelper.Confirm($"Delete this expense of PKR {expense.Amount:#,##0.00}?")) return;

        try
        {
            await expenseService.DeleteExpenseAsync(expense.Id);
            Expenses.Remove(expense);
            OnPropertyChanged(nameof(ShowingTotal));
        }
        catch (Exception ex)
        {
            SetError($"Delete failed: {ex.Message}");
        }
    }

    private void SetError(string msg) { ErrorMessage = msg; HasError = true; }
    private void ClearError()         { ErrorMessage = ""; HasError = false; }
}
