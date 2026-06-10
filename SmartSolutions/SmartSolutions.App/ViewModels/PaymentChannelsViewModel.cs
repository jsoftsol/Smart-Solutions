// SmartSolutions.App/ViewModels/PaymentChannelsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class PaymentChannelsViewModel(ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<PaymentChannel> _channels = [];
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
        try { Channels = new(await lookup.GetPaymentChannelsAsync()); }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode = false; _editingId = 0;
        DialogTitle = "Add Channel"; DialogName = ""; DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(PaymentChannel channel)
    {
        _isEditMode = true; _editingId = channel.Id;
        DialogTitle = "Rename Channel"; DialogName = channel.Name; DialogErrorMessage = ""; IsDialogOpen = true;
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
                var added = await lookup.AddPaymentChannelAsync(DialogName.Trim());
                Channels.Add(added);
            }
            else
            {
                await lookup.RenamePaymentChannelAsync(_editingId, DialogName.Trim());
                var ch = Channels.First(c => c.Id == _editingId);
                var idx = Channels.IndexOf(ch);
                ch.Name = DialogName.Trim();
                Channels.RemoveAt(idx); Channels.Insert(idx, ch);
            }
            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task DeleteAsync(PaymentChannel channel)
    {
        if (!DialogHelper.Confirm($"Delete payment channel '{channel.Name}'?")) return;
        try { await lookup.DeletePaymentChannelAsync(channel.Id); Channels.Remove(channel); }
        catch (Exception ex) { StatusMessage = $"Failed to delete: {ex.Message}"; }
    }
}
