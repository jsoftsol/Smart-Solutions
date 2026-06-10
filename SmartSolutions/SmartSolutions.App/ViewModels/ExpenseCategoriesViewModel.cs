// SmartSolutions.App/ViewModels/ExpenseCategoriesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class ExpenseCategoriesViewModel(ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ExpenseCategory> _categories = [];
    [ObservableProperty] private bool   _isDialogOpen;
    [ObservableProperty] private string _dialogTitle = "";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Categories = new(await lookup.GetExpenseCategoriesAsync()); }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode = false; _editingId = 0;
        DialogTitle = "Add Category"; DialogName = ""; DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(ExpenseCategory cat)
    {
        _isEditMode = true; _editingId = cat.Id;
        DialogTitle = "Rename Category"; DialogName = cat.Name; DialogErrorMessage = ""; IsDialogOpen = true;
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
                var added = await lookup.AddExpenseCategoryAsync(DialogName.Trim());
                Categories.Add(added);
            }
            else
            {
                await lookup.RenameExpenseCategoryAsync(_editingId, DialogName.Trim());
                var cat = Categories.First(c => c.Id == _editingId);
                var idx = Categories.IndexOf(cat);
                cat.Name = DialogName.Trim();
                Categories.RemoveAt(idx); Categories.Insert(idx, cat);
            }
            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task DeleteAsync(ExpenseCategory cat)
    {
        if (!DialogHelper.Confirm($"Delete expense category '{cat.Name}'?")) return;
        try { await lookup.DeleteExpenseCategoryAsync(cat.Id); Categories.Remove(cat); }
        catch (Exception ex) { StatusMessage = $"Failed to delete: {ex.Message}"; }
    }
}
