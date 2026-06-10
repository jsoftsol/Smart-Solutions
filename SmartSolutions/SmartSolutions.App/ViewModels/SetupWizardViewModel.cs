// SmartSolutions/SmartSolutions.App/ViewModels/SetupWizardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using SmartSolutions.App.Helpers;
using SmartSolutions.App.Models;
using SmartSolutions.Core.Helpers;

namespace SmartSolutions.App.ViewModels;

public partial class SetupWizardViewModel : ObservableObject
{
    // ── Step 1: Database ──────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyPropertyChangedFor(nameof(UseSqlAuth))]
    private bool _useWindowsAuth = true;

    public bool UseSqlAuth => !UseWindowsAuth;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private bool _connectionTestPassed;

    [ObservableProperty] private string _server           = "";
    [ObservableProperty] private string _databaseName     = "SmartSolutions";
    [ObservableProperty] private string _sqlUsername      = "";
    [ObservableProperty] private string _sqlPassword      = "";
    [ObservableProperty] private string _connectionResult = "";
    [ObservableProperty] private bool   _isTesting;

    // ── Step 2: Business Info ─────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _businessName = "";

    [ObservableProperty] private string _ntn     = "";
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _phone1  = "";
    [ObservableProperty] private string _phone2  = "";
    [ObservableProperty] private string _email   = "";

    // ── Step 3: Admin PIN ─────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FinishCommand))]
    [NotifyPropertyChangedFor(nameof(PinMismatch))]
    private string _adminPin = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FinishCommand))]
    [NotifyPropertyChangedFor(nameof(PinMismatch))]
    private string _adminPinConfirm = "";

    public bool PinMismatch =>
        !string.IsNullOrEmpty(AdminPin)        &&
        !string.IsNullOrEmpty(AdminPinConfirm) &&
        AdminPin != AdminPinConfirm;

    // ── Navigation ────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFirstStep))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    [NotifyPropertyChangedFor(nameof(IsStep1Done))]
    [NotifyPropertyChangedFor(nameof(IsStep2Done))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private int _currentStep = 1;

    public bool IsFirstStep => CurrentStep == 1;
    public bool IsLastStep  => CurrentStep == 3;
    public bool IsStep1Done => CurrentStep > 1;
    public bool IsStep2Done => CurrentStep > 2;

    public event Action? SetupCompleted;

    // ── Commands ──────────────────────────────────────────────────────
    [RelayCommand]
    private void Back() => CurrentStep--;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => CurrentStep++;

    private bool CanGoNext() => CurrentStep switch
    {
        1 => ConnectionTestPassed,
        2 => !string.IsNullOrWhiteSpace(BusinessName),
        _ => false
    };

    [RelayCommand]
    private async Task TestConnection()
    {
        IsTesting        = true;
        ConnectionResult = "";
        ConnectionTestPassed = false;

        var cs = ConnectionStringBuilder.Build(
            Server, DatabaseName, UseWindowsAuth,
            UseWindowsAuth ? null : SqlUsername,
            UseWindowsAuth ? null : SqlPassword);

        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            ConnectionResult     = "Connected successfully";
            ConnectionTestPassed = true;
        }
        catch (Exception ex)
        {
            ConnectionResult = ex.Message;
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanFinish))]
    private void Finish() => SaveAndComplete(AdminPin);

    private bool CanFinish() =>
        string.IsNullOrEmpty(AdminPin) ||
        (AdminPin.Length == 4 && AdminPin == AdminPinConfirm && AdminPin.All(char.IsDigit));

    public void FinishWithSkip() => SaveAndComplete("");

    private void SaveAndComplete(string pin)
    {
        var cs = ConnectionStringBuilder.Build(
            Server, DatabaseName, UseWindowsAuth,
            UseWindowsAuth ? null : SqlUsername,
            UseWindowsAuth ? null : SqlPassword);

        SettingsManager.SaveWithFirstRunData(cs, new FirstRunData
        {
            BusinessName = BusinessName,
            Ntn          = Ntn,
            Address      = Address,
            Phone1       = Phone1,
            Phone2       = Phone2,
            Email        = Email,
            AdminPin     = pin
        });

        SetupCompleted?.Invoke();
    }

    // Reset test state when any connection field changes
    partial void OnServerChanged(string value)       => ResetConnectionTest();
    partial void OnDatabaseNameChanged(string value) => ResetConnectionTest();
    partial void OnUseWindowsAuthChanged(bool value) => ResetConnectionTest();
    partial void OnSqlUsernameChanged(string value)  => ResetConnectionTest();
    partial void OnSqlPasswordChanged(string value)  => ResetConnectionTest();

    private void ResetConnectionTest()
    {
        ConnectionTestPassed = false;
        ConnectionResult     = "";
    }
}
