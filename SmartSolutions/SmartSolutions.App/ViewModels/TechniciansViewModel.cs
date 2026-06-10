// SmartSolutions.App/ViewModels/TechniciansViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class TechniciansViewModel(ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<Technician> _technicians = [];
    [ObservableProperty] private bool   _isDialogOpen;
    [ObservableProperty] private string _dialogTitle = "";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogPhone = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Technicians = new(await lookup.GetTechniciansAsync()); }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode = false; _editingId = 0;
        DialogTitle = "Add Technician"; DialogName = ""; DialogPhone = "";
        DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Technician tech)
    {
        _isEditMode = true; _editingId = tech.Id;
        DialogTitle = "Edit Technician"; DialogName = tech.Name; DialogPhone = tech.Phone ?? "";
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
                var added = await lookup.AddTechnicianAsync(DialogName.Trim(), NullIfEmpty(DialogPhone));
                Technicians.Add(added);
            }
            else
            {
                await lookup.UpdateTechnicianAsync(_editingId, DialogName.Trim(), NullIfEmpty(DialogPhone));
                var t = Technicians.First(x => x.Id == _editingId);
                var idx = Technicians.IndexOf(t);
                t.Name = DialogName.Trim(); t.Phone = NullIfEmpty(DialogPhone);
                Technicians.RemoveAt(idx); Technicians.Insert(idx, t);
            }
            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task DeleteAsync(Technician tech)
    {
        if (!DialogHelper.Confirm($"Delete technician '{tech.Name}'?")) return;
        try { await lookup.DeleteTechnicianAsync(tech.Id); Technicians.Remove(tech); }
        catch (Exception ex) { StatusMessage = $"Failed to delete: {ex.Message}"; }
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
