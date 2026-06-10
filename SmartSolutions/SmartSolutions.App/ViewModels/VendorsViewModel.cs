// SmartSolutions.App/ViewModels/VendorsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class VendorsViewModel(ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<Vendor> _vendors = [];
    [ObservableProperty] private bool   _isDialogOpen;
    [ObservableProperty] private string _dialogTitle = "";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogPhone = "";
    [ObservableProperty] private string _dialogNotes = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Vendors = new(await lookup.GetVendorsAsync()); }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode = false; _editingId = 0;
        DialogTitle = "Add Vendor"; DialogName = ""; DialogPhone = ""; DialogNotes = "";
        DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Vendor vendor)
    {
        _isEditMode = true; _editingId = vendor.Id;
        DialogTitle = "Edit Vendor"; DialogName = vendor.Name;
        DialogPhone = vendor.Phone ?? ""; DialogNotes = vendor.Notes ?? "";
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
                var added = await lookup.AddVendorAsync(DialogName.Trim(), NullIfEmpty(DialogPhone), NullIfEmpty(DialogNotes));
                Vendors.Add(added);
            }
            else
            {
                await lookup.UpdateVendorAsync(_editingId, DialogName.Trim(), NullIfEmpty(DialogPhone), NullIfEmpty(DialogNotes));
                var v = Vendors.First(x => x.Id == _editingId);
                var idx = Vendors.IndexOf(v);
                v.Name = DialogName.Trim(); v.Phone = NullIfEmpty(DialogPhone); v.Notes = NullIfEmpty(DialogNotes);
                Vendors.RemoveAt(idx); Vendors.Insert(idx, v);
            }
            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task DeleteAsync(Vendor vendor)
    {
        if (!DialogHelper.Confirm($"Delete vendor '{vendor.Name}'?")) return;
        try { await lookup.DeleteVendorAsync(vendor.Id); Vendors.Remove(vendor); }
        catch (Exception ex) { StatusMessage = $"Failed to delete: {ex.Message}"; }
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
