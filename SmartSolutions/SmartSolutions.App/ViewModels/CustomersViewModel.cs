// SmartSolutions.App/ViewModels/CustomersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class CustomersViewModel(ICustomerService customerService) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<Customer> _customers = [];
    [ObservableProperty] private bool   _isDialogOpen;
    [ObservableProperty] private string _dialogTitle = "";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogPhone = "";
    [ObservableProperty] private string _dialogAddress = "";
    [ObservableProperty] private string _dialogNotes = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Customers = new(await customerService.SearchCustomersAsync()); }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode = false; _editingId = 0;
        DialogTitle = "Add Customer";
        DialogName = ""; DialogPhone = ""; DialogAddress = ""; DialogNotes = "";
        DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Customer customer)
    {
        _isEditMode = true; _editingId = customer.Id;
        DialogTitle = "Edit Customer";
        DialogName = customer.Name;
        DialogPhone = customer.Phone ?? "";
        DialogAddress = customer.Address ?? "";
        DialogNotes = customer.Notes ?? "";
        DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveDialogAsync()
    {
        if (string.IsNullOrWhiteSpace(DialogName))
        { DialogErrorMessage = "Name is required."; return; }
        try
        {
            if (!_isEditMode)
            {
                var added = await customerService.AddCustomerAsync(
                    DialogName.Trim(),
                    NullIfEmpty(DialogPhone),
                    NullIfEmpty(DialogAddress),
                    NullIfEmpty(DialogNotes));
                Customers.Add(added);
            }
            else
            {
                await customerService.UpdateCustomerAsync(
                    _editingId,
                    DialogName.Trim(),
                    NullIfEmpty(DialogPhone),
                    NullIfEmpty(DialogAddress),
                    NullIfEmpty(DialogNotes));
                var c = Customers.First(x => x.Id == _editingId);
                var idx = Customers.IndexOf(c);
                c.Name    = DialogName.Trim();
                c.Phone   = NullIfEmpty(DialogPhone);
                c.Address = NullIfEmpty(DialogAddress);
                c.Notes   = NullIfEmpty(DialogNotes);
                Customers.RemoveAt(idx); Customers.Insert(idx, c);
            }
            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task DeleteAsync(Customer customer)
    {
        if (!DialogHelper.Confirm($"Delete customer '{customer.Name}'?")) return;
        try
        {
            await customerService.DeleteCustomerAsync(customer.Id);
            Customers.Remove(customer);
        }
        catch (DbUpdateException)
        {
            StatusMessage = "Cannot delete a customer who has existing orders.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to delete: {ex.Message}";
        }
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
