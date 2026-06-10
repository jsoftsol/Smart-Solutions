// SmartSolutions.App/ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.App.ViewModels;

public partial class SettingsViewModel(ILookupService lookup) : ObservableObject
{
    // ── Business Info ─────────────────────────────────────────────────────
    [ObservableProperty] private BusinessInfo _businessInfo = new();
    [ObservableProperty] private bool         _isBusy;
    [ObservableProperty] private string       _statusMessage = "";

    public async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = "";
        try
        {
            BusinessInfo = await lookup.GetBusinessInfoAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveBusinessInfoAsync()
    {
        IsBusy = true;
        StatusMessage = "";
        try
        {
            await lookup.SaveBusinessInfoAsync(BusinessInfo);
            StatusMessage = "Business info saved.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save business info: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
