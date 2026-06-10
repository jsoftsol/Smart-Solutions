// SmartSolutions.App/ViewModels/ItemsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class ItemsViewModel(ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ItemCategory> _categories = [];
    [ObservableProperty] private ItemCategory? _selectedCategory;
    [ObservableProperty] private ObservableCollection<ItemName> _itemNames = [];
    [ObservableProperty] private bool   _isDialogOpen;
    [ObservableProperty] private string _dialogTitle = "";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    private bool _dialogEditingCategory;
    private bool _dialogIsEdit;
    private int  _dialogEditingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Categories = new(await lookup.GetItemCategoriesAsync()); }
        finally { IsBusy = false; }
    }

    partial void OnSelectedCategoryChanged(ItemCategory? value)
    {
        ItemNames.Clear();
        OpenAddItemNameDialogCommand.NotifyCanExecuteChanged();
        if (value is not null) _ = LoadItemNamesAsync(value.Id);
    }

    private async Task LoadItemNamesAsync(int categoryId)
    {
        try
        {
            ItemNames = new(await lookup.GetItemNamesAsync(categoryId));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load items: {ex.Message}";
        }
    }

    // ── Category dialog ──────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddCategoryDialog()
    {
        _dialogEditingCategory = true; _dialogIsEdit = false; _dialogEditingId = 0;
        DialogTitle = "Add Category"; DialogName = ""; DialogErrorMessage = "";
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditCategoryDialog(ItemCategory cat)
    {
        _dialogEditingCategory = true; _dialogIsEdit = true; _dialogEditingId = cat.Id;
        DialogTitle = "Rename Category"; DialogName = cat.Name; DialogErrorMessage = "";
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(ItemCategory cat)
    {
        if (!DialogHelper.Confirm(
            $"Delete category '{cat.Name}'? All item names in this category will also be deleted.")) return;
        try
        {
            await lookup.DeleteItemCategoryAsync(cat.Id);
            Categories.Remove(cat);
            if (SelectedCategory?.Id == cat.Id) { SelectedCategory = null; ItemNames.Clear(); }
        }
        catch (Exception ex) { StatusMessage = $"Failed to delete: {ex.Message}"; }
    }

    // ── Item Name dialog ─────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanOpenAddItemNameDialog))]
    private void OpenAddItemNameDialog()
    {
        _dialogEditingCategory = false; _dialogIsEdit = false; _dialogEditingId = 0;
        DialogTitle = $"Add Item to '{SelectedCategory!.Name}'";
        DialogName = ""; DialogErrorMessage = ""; IsDialogOpen = true;
    }
    private bool CanOpenAddItemNameDialog() => SelectedCategory is not null;

    [RelayCommand]
    private void OpenEditItemNameDialog(ItemName item)
    {
        _dialogEditingCategory = false; _dialogIsEdit = true; _dialogEditingId = item.Id;
        DialogTitle = "Rename Item"; DialogName = item.Name; DialogErrorMessage = "";
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task DeleteItemNameAsync(ItemName item)
    {
        if (!DialogHelper.Confirm($"Delete item '{item.Name}'?")) return;
        try { await lookup.DeleteItemNameAsync(item.Id); ItemNames.Remove(item); }
        catch (Exception ex) { StatusMessage = $"Failed to delete: {ex.Message}"; }
    }

    // ── Shared dialog Save / Cancel ───────────────────────────────────────

    [RelayCommand]
    private async Task SaveDialogAsync()
    {
        if (string.IsNullOrWhiteSpace(DialogName))
        { DialogErrorMessage = "Name is required."; return; }

        try
        {
            if (_dialogEditingCategory)
            {
                if (!_dialogIsEdit)
                {
                    var added = await lookup.AddItemCategoryAsync(DialogName.Trim());
                    Categories.Add(added);
                }
                else
                {
                    await lookup.RenameItemCategoryAsync(_dialogEditingId, DialogName.Trim());
                    var cat = Categories.First(c => c.Id == _dialogEditingId);
                    var idx = Categories.IndexOf(cat);
                    cat.Name = DialogName.Trim();
                    Categories.RemoveAt(idx); Categories.Insert(idx, cat);
                    if (SelectedCategory?.Id == _dialogEditingId) SelectedCategory = cat;
                }
            }
            else
            {
                if (SelectedCategory is null) { DialogErrorMessage = "Select a category first."; return; }
                if (!_dialogIsEdit)
                {
                    var added = await lookup.AddItemNameAsync(DialogName.Trim(), SelectedCategory.Id);
                    ItemNames.Add(added);
                }
                else
                {
                    await lookup.RenameItemNameAsync(_dialogEditingId, DialogName.Trim());
                    var item = ItemNames.First(i => i.Id == _dialogEditingId);
                    var idx = ItemNames.IndexOf(item);
                    item.Name = DialogName.Trim();
                    ItemNames.RemoveAt(idx); ItemNames.Insert(idx, item);
                }
            }
            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;
}
