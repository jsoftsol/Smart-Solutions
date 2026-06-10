// SmartSolutions.App/ViewModels/UsersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class UsersViewModel(IAuthService auth, ISessionService session) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<AppUser> _users = [];
    [ObservableProperty] private bool    _isAddDialogOpen;
    [ObservableProperty] private bool    _isResetPinDialogOpen;
    [ObservableProperty] private AppUser? _resetPinTargetUser;
    [ObservableProperty] private string  _newUsername = "";
    [ObservableProperty] private string  _errorMessage = "";
    [ObservableProperty] private string  _statusMessage = "";
    [ObservableProperty] private bool    _isBusy;

    public bool IsDialogOpen => IsAddDialogOpen || IsResetPinDialogOpen;

    partial void OnIsAddDialogOpenChanged(bool value)     => OnPropertyChanged(nameof(IsDialogOpen));
    partial void OnIsResetPinDialogOpenChanged(bool value) => OnPropertyChanged(nameof(IsDialogOpen));

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Users = new(await auth.GetAllAsync()); }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    // ── Add User ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddDialog()
    {
        NewUsername = ""; ErrorMessage = ""; IsAddDialogOpen = true;
    }

    public async Task ConfirmAddUserAsync(string username, string pin)
    {
        ErrorMessage = "";
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pin))
        { ErrorMessage = "Username and PIN are required."; return; }
        try
        {
            await auth.CreateAsync(username.Trim(), pin);
            Users = new(await auth.GetAllAsync());
            IsAddDialogOpen = false;
        }
        catch (InvalidOperationException ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelAddDialog() { IsAddDialogOpen = false; ErrorMessage = ""; }

    // ── Reset PIN ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenResetPinDialog(AppUser user)
    {
        ResetPinTargetUser = user; ErrorMessage = ""; IsResetPinDialogOpen = true;
    }

    public async Task ConfirmResetPinAsync(string newPin)
    {
        ErrorMessage = "";
        if (ResetPinTargetUser is null) return;
        if (string.IsNullOrWhiteSpace(newPin)) { ErrorMessage = "PIN is required."; return; }
        try
        {
            await auth.UpdatePinAsync(ResetPinTargetUser.Id, newPin);
            IsResetPinDialogOpen = false;
            StatusMessage = $"PIN updated for '{ResetPinTargetUser.Username}'.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelResetPinDialog() { IsResetPinDialogOpen = false; ErrorMessage = ""; }

    // ── Toggle Active ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ToggleActiveAsync(AppUser user)
    {
        StatusMessage = "";
        if (user.Id == session.CurrentUser.Id)
        { StatusMessage = "You cannot deactivate your own account."; return; }
        try
        {
            await auth.SetActiveAsync(user.Id, !user.IsActive);
            Users = new(await auth.GetAllAsync());
        }
        catch (Exception ex) { StatusMessage = $"Failed: {ex.Message}"; }
    }
}
