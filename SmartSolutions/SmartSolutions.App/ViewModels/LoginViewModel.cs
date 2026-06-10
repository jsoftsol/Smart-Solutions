// SmartSolutions.App/ViewModels/LoginViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.App.ViewModels;

public partial class LoginViewModel(IAuthService auth) : ObservableObject
{
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = "";

    [ObservableProperty]
    private string _errorMessage = "";

    public string Pin { get; set; } = "";

    public event Action<AppUser>? LoginSucceeded;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = "";
        var user = await auth.ValidateAsync(Username.Trim(), Pin);
        if (user is null)
        {
            ErrorMessage = "Invalid username or PIN.";
            return;
        }
        LoginSucceeded?.Invoke(user);
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(Username);
}
