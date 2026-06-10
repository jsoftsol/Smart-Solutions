# MSIX Packaging & First-Run Wizard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Package Smart Solutions as a sideloadable MSIX and show a three-step first-run wizard (DB connection → business info → admin PIN) before the DI host is built.

**Architecture:** A `SettingsManager` static class owns `%LOCALAPPDATA%\SmartSolutions\appsettings.json`. `App.xaml.cs` checks `IsSetupRequired()` before building the host; if true, shows `SetupWizardWindow` (plain `Window`, no DI) which writes the connection string and `FirstRunData` on completion. After migrations run, the startup seed reads `FirstRunData`, updates `BusinessInfo` and admin PIN, then removes the section from the settings file.

**Tech Stack:** .NET 10 WPF, CommunityToolkit.Mvvm, MaterialDesignThemes, Microsoft.Data.SqlClient (transitive via EF Core SqlServer), System.Text.Json, single-project MSIX via `<WindowsPackageType>MSIX</WindowsPackageType>`, xunit + FluentAssertions for tests.

---

## File Map

| Status | Path | Purpose |
|---|---|---|
| **Create** | `SmartSolutions.Core/Helpers/ConnectionStringBuilder.cs` | Pure connection string assembly — testable from Tests project |
| **Create** | `SmartSolutions.App/Models/FirstRunData.cs` | POCO for wizard → startup seed handoff |
| **Create** | `SmartSolutions.App/Models/AppSettings.cs` | POCO matching `appsettings.json` structure |
| **Create** | `SmartSolutions.App/Helpers/SettingsManager.cs` | LocalAppData file I/O |
| **Create** | `SmartSolutions.App/Converters/EqualToVisibilityConverter.cs` | Shows step panel when current step matches |
| **Create** | `SmartSolutions.App/Converters/InverseBoolConverter.cs` | Used for IsEnabled on Test Connection button |
| **Create** | `SmartSolutions.App/ViewModels/SetupWizardViewModel.cs` | Wizard state, navigation, connection test, save |
| **Create** | `SmartSolutions.App/Views/Steps/DatabaseStepControl.xaml` + `.cs` | Step 1 UI |
| **Create** | `SmartSolutions.App/Views/Steps/BusinessInfoStepControl.xaml` + `.cs` | Step 2 UI |
| **Create** | `SmartSolutions.App/Views/Steps/AdminPinStepControl.xaml` + `.cs` | Step 3 UI |
| **Create** | `SmartSolutions.App/Views/SetupWizardWindow.xaml` + `.cs` | Wizard shell window |
| **Create** | `SmartSolutions.App/Package.appxmanifest` | MSIX manifest |
| **Create** | `SmartSolutions.App/Assets/*.png` | Placeholder tile images |
| **Create** | `SmartSolutions.Tests/Helpers/ConnectionStringBuilderTests.cs` | Unit tests |
| **Create** | `docs/INSTALL.md` | Per-PC certificate install guide |
| **Modify** | `SmartSolutions.App/SmartSolutions.App.csproj` | MSIX property, cert, asset items |
| **Modify** | `SmartSolutions.App/App.xaml` | Register two new converters |
| **Modify** | `SmartSolutions.App/ServiceConfiguration.cs` | Load config from LocalAppData |
| **Modify** | `SmartSolutions.App/App.xaml.cs` | First-run check + seeding |
| **Modify** | `CLAUDE.md` | Deployment + wizard notes |
| **Modify** | `docs/superpowers/specs/2026-06-09-smart-solutions-design.md` | Sections 6 and 8.5 |

---

## Task 1: ConnectionStringBuilder + tests

**Files:**
- Create: `SmartSolutions/SmartSolutions.Core/Helpers/ConnectionStringBuilder.cs`
- Create: `SmartSolutions/SmartSolutions.Tests/Helpers/ConnectionStringBuilderTests.cs`

- [ ] **Step 1.1: Write failing tests**

```csharp
// SmartSolutions.Tests/Helpers/ConnectionStringBuilderTests.cs
using FluentAssertions;
using SmartSolutions.Core.Helpers;

namespace SmartSolutions.Tests.Helpers;

public class ConnectionStringBuilderTests
{
    [Fact]
    public void Build_WindowsAuth_ProducesCorrectString()
    {
        var result = ConnectionStringBuilder.Build(
            server: @"SERVER\SQLEXPRESS",
            database: "SmartSolutions",
            windowsAuth: true);

        result.Should().Be(
            @"Server=SERVER\SQLEXPRESS;Database=SmartSolutions;Trusted_Connection=True;TrustServerCertificate=True");
    }

    [Fact]
    public void Build_SqlAuth_IncludesCredentials()
    {
        var result = ConnectionStringBuilder.Build(
            server: "192.168.1.10",
            database: "SmartSolutions",
            windowsAuth: false,
            username: "sa",
            password: "pass123");

        result.Should().Be(
            "Server=192.168.1.10;Database=SmartSolutions;User Id=sa;Password=pass123;TrustServerCertificate=True");
    }

    [Fact]
    public void Build_WindowsAuth_IgnoresCredentials()
    {
        var result = ConnectionStringBuilder.Build(
            server: "SERVER",
            database: "db",
            windowsAuth: true,
            username: "ignored",
            password: "ignored");

        result.Should().NotContain("User Id");
        result.Should().NotContain("Password");
    }
}
```

- [ ] **Step 1.2: Run tests — expect failure**

```
dotnet test SmartSolutions/SmartSolutions.Tests --filter "ConnectionStringBuilderTests" --no-build 2>&1 | tail -5
```

Expected: compile error — `ConnectionStringBuilder` not found.

- [ ] **Step 1.3: Implement**

```csharp
// SmartSolutions/SmartSolutions.Core/Helpers/ConnectionStringBuilder.cs
namespace SmartSolutions.Core.Helpers;

public static class ConnectionStringBuilder
{
    public static string Build(
        string server,
        string database,
        bool windowsAuth,
        string? username = null,
        string? password = null)
    {
        if (windowsAuth)
            return $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True";

        return $"Server={server};Database={database};User Id={username ?? ""};Password={password ?? ""};TrustServerCertificate=True";
    }
}
```

- [ ] **Step 1.4: Run tests — expect pass**

```
dotnet test SmartSolutions/SmartSolutions.Tests --filter "ConnectionStringBuilderTests"
```

Expected: 3 passed.

- [ ] **Step 1.5: Commit**

```
git add SmartSolutions/SmartSolutions.Core/Helpers/ConnectionStringBuilder.cs SmartSolutions/SmartSolutions.Tests/Helpers/ConnectionStringBuilderTests.cs
git commit -m "feat: add ConnectionStringBuilder helper with tests"
```

---

## Task 2: Settings models + SettingsManager

**Files:**
- Create: `SmartSolutions/SmartSolutions.App/Models/FirstRunData.cs`
- Create: `SmartSolutions/SmartSolutions.App/Models/AppSettings.cs`
- Create: `SmartSolutions/SmartSolutions.App/Helpers/SettingsManager.cs`

- [ ] **Step 2.1: Create FirstRunData**

```csharp
// SmartSolutions/SmartSolutions.App/Models/FirstRunData.cs
namespace SmartSolutions.App.Models;

public class FirstRunData
{
    public string BusinessName { get; set; } = "";
    public string Ntn          { get; set; } = "";
    public string Address      { get; set; } = "";
    public string Phone1       { get; set; } = "";
    public string Phone2       { get; set; } = "";
    public string Email        { get; set; } = "";
    public string AdminPin     { get; set; } = "";
}
```

- [ ] **Step 2.2: Create AppSettings**

```csharp
// SmartSolutions/SmartSolutions.App/Models/AppSettings.cs
namespace SmartSolutions.App.Models;

public class AppSettings
{
    public ConnectionStringsSection ConnectionStrings { get; set; } = new();
    public FirstRunData?             FirstRunData     { get; set; }

    public class ConnectionStringsSection
    {
        public string Default { get; set; } = "";
    }
}
```

- [ ] **Step 2.3: Create SettingsManager**

```csharp
// SmartSolutions/SmartSolutions.App/Helpers/SettingsManager.cs
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutions.App.Models;

namespace SmartSolutions.App.Helpers;

public static class SettingsManager
{
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string SettingsFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartSolutions",
            "appsettings.json");

    public static bool IsSetupRequired() => !File.Exists(SettingsFilePath);

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
            return new AppSettings();

        var json = File.ReadAllText(SettingsFilePath);
        return JsonSerializer.Deserialize<AppSettings>(json, _json) ?? new AppSettings();
    }

    public static void SaveWithFirstRunData(string connectionString, FirstRunData data)
    {
        Write(new AppSettings
        {
            ConnectionStrings = new AppSettings.ConnectionStringsSection { Default = connectionString },
            FirstRunData = data
        });
    }

    public static void ClearFirstRunData()
    {
        var settings = Load();
        settings.FirstRunData = null;
        Write(settings);
    }

    private static void Write(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings, _json));
    }
}
```

- [ ] **Step 2.4: Build to confirm no compile errors**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 2.5: Commit**

```
git add SmartSolutions/SmartSolutions.App/Models/ SmartSolutions/SmartSolutions.App/Helpers/SettingsManager.cs
git commit -m "feat: add SettingsManager and settings models for LocalAppData config"
```

---

## Task 3: Update ServiceConfiguration to read from LocalAppData

**Files:**
- Modify: `SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs`

- [ ] **Step 3.1: Update ServiceConfiguration**

Replace the entire file:

```csharp
// SmartSolutions.App/ServiceConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutions.App.Helpers;
using SmartSolutions.App.Services;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Core.Services;
using SmartSolutions.Data;

namespace SmartSolutions.App;

public static class ServiceConfiguration
{
    public static IHostBuilder ConfigureSmartSolutions(this IHostBuilder builder) =>
        builder
            .ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddJsonFile(SettingsManager.SettingsFilePath, optional: false, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContextFactory<AppDbContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("Default")));

                services.AddSingleton<IAuthService,       AuthService>();
                services.AddSingleton<ISessionService,    SessionService>();
                services.AddSingleton<ILookupService,     LookupService>();
                services.AddSingleton<ICustomerService,   CustomerService>();
                services.AddSingleton<IPrintOrderService, PrintOrderService>();
                services.AddSingleton<IHaierJobService,   HaierJobService>();
                services.AddSingleton<IExpenseService,    ExpenseService>();
                services.AddSingleton<IDashboardService,  DashboardService>();
                services.AddSingleton<IInvoiceService,    InvoiceService>();

                services.AddSingleton<ViewModels.MainViewModel>();
                services.AddTransient<ViewModels.DashboardViewModel>();
                services.AddTransient<ViewModels.PrintOrdersViewModel>();
                services.AddTransient<ViewModels.PrintOrderDetailViewModel>();
                services.AddTransient<ViewModels.HaierJobsViewModel>();
                services.AddTransient<ViewModels.HaierJobDetailViewModel>();
                services.AddTransient<ViewModels.ExpensesViewModel>();
                services.AddTransient<ViewModels.SettingsViewModel>();
                services.AddTransient<ViewModels.ReportsViewModel>();
                services.AddTransient<ViewModels.ItemsViewModel>();
                services.AddTransient<ViewModels.CustomersViewModel>();
                services.AddTransient<ViewModels.VendorsViewModel>();
                services.AddTransient<ViewModels.TechniciansViewModel>();
                services.AddTransient<ViewModels.ExpenseCategoriesViewModel>();
                services.AddTransient<ViewModels.PaymentChannelsViewModel>();
                services.AddTransient<ViewModels.UsersViewModel>();

                services.AddSingleton<MainWindow>();
                services.AddTransient<ViewModels.LoginViewModel>();
                services.AddTransient<Views.LoginWindow>();
            });
}
```

- [ ] **Step 3.2: Build**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 3.3: Commit**

```
git add SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs
git commit -m "feat: load IConfiguration exclusively from LocalAppData appsettings.json"
```

---

## Task 4: Add EqualToVisibilityConverter and InverseBoolConverter

**Files:**
- Create: `SmartSolutions/SmartSolutions.App/Converters/EqualToVisibilityConverter.cs`
- Create: `SmartSolutions/SmartSolutions.App/Converters/InverseBoolConverter.cs`
- Modify: `SmartSolutions/SmartSolutions.App/App.xaml`

- [ ] **Step 4.1: Create EqualToVisibilityConverter**

```csharp
// SmartSolutions/SmartSolutions.App/Converters/EqualToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartSolutions.App.Converters;

public class EqualToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString() ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

- [ ] **Step 4.2: Create InverseBoolConverter**

```csharp
// SmartSolutions/SmartSolutions.App/Converters/InverseBoolConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace SmartSolutions.App.Converters;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : value;
}
```

- [ ] **Step 4.3: Register in App.xaml**

Add two lines inside the existing `<ResourceDictionary>` block, after the existing converter entries:

```xml
<converters:EqualToVisibilityConverter x:Key="EqualToVisibility" />
<converters:InverseBoolConverter       x:Key="InverseBoolConverter" />
```

The full App.xaml Resources section becomes:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <materialDesign:BundledTheme BaseTheme="Light"
                                         PrimaryColor="DeepPurple"
                                         SecondaryColor="Lime" />
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />
        </ResourceDictionary.MergedDictionaries>

        <converters:BoolToVisibilityConverter        x:Key="BoolToVisibility" />
        <converters:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibility" />
        <converters:RateTypeToVisibilityConverter    x:Key="RateTypeToVisibility" />
        <converters:CurrencyConverter                x:Key="CurrencyConverter" />
        <converters:BoolToActiveConverter            x:Key="BoolToActiveConverter" />
        <converters:EqualToVisibilityConverter       x:Key="EqualToVisibility" />
        <converters:InverseBoolConverter             x:Key="InverseBoolConverter" />
    </ResourceDictionary>
</Application.Resources>
```

- [ ] **Step 4.4: Build**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 4.5: Commit**

```
git add SmartSolutions/SmartSolutions.App/Converters/ SmartSolutions/SmartSolutions.App/App.xaml
git commit -m "feat: add EqualToVisibilityConverter and InverseBoolConverter"
```

---

## Task 5: SetupWizardViewModel

**Files:**
- Create: `SmartSolutions/SmartSolutions.App/ViewModels/SetupWizardViewModel.cs`

- [ ] **Step 5.1: Create ViewModel**

```csharp
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
```

- [ ] **Step 5.2: Build**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 5.3: Commit**

```
git add SmartSolutions/SmartSolutions.App/ViewModels/SetupWizardViewModel.cs
git commit -m "feat: add SetupWizardViewModel"
```

---

## Task 6: DatabaseStepControl (Step 1)

**Files:**
- Create: `SmartSolutions/SmartSolutions.App/Views/Steps/DatabaseStepControl.xaml`
- Create: `SmartSolutions/SmartSolutions.App/Views/Steps/DatabaseStepControl.xaml.cs`

- [ ] **Step 6.1: Create XAML**

```xml
<!-- SmartSolutions/SmartSolutions.App/Views/Steps/DatabaseStepControl.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.Steps.DatabaseStepControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <StackPanel>
        <TextBlock Text="Database Connection"
                   Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                   FontWeight="Medium" Margin="0,0,0,16" />

        <TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 materialDesign:HintAssist.Hint="Server\Instance   e.g. SERVER-PC\SQLEXPRESS"
                 Text="{Binding Server, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,12" />

        <TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 materialDesign:HintAssist.Hint="Database Name"
                 Text="{Binding DatabaseName, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,12" />

        <TextBlock Text="Authentication"
                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                   Margin="0,0,0,4" />
        <RadioButton Content="Windows Authentication (recommended)"
                     IsChecked="{Binding UseWindowsAuth}"
                     Margin="0,0,0,4" />
        <RadioButton Content="SQL Server Authentication"
                     IsChecked="{Binding UseSqlAuth}"
                     Margin="0,0,0,8" />

        <StackPanel Visibility="{Binding UseSqlAuth, Converter={StaticResource BoolToVisibility}}">
            <TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     materialDesign:HintAssist.Hint="SQL Username"
                     Text="{Binding SqlUsername, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,0,0,12" />
            <PasswordBox x:Name="SqlPasswordBox"
                         Style="{StaticResource MaterialDesignOutlinedPasswordBox}"
                         materialDesign:HintAssist.Hint="SQL Password"
                         PasswordChanged="SqlPasswordBox_PasswordChanged"
                         Margin="0,0,0,12" />
        </StackPanel>

        <StackPanel Orientation="Horizontal" Margin="0,4,0,0">
            <Button Command="{Binding TestConnectionCommand}"
                    Content="Test Connection"
                    Style="{StaticResource MaterialDesignOutlinedButton}"
                    IsEnabled="{Binding IsTesting, Converter={StaticResource InverseBoolConverter}}" />
            <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}"
                         Width="24" Height="24"
                         IsIndeterminate="True"
                         Margin="12,0,0,0"
                         Visibility="{Binding IsTesting, Converter={StaticResource BoolToVisibility}}" />
        </StackPanel>

        <TextBlock Text="{Binding ConnectionResult}"
                   TextWrapping="Wrap"
                   Margin="0,8,0,0">
            <TextBlock.Style>
                <Style TargetType="TextBlock">
                    <Setter Property="Foreground" Value="Green" />
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding ConnectionTestPassed}" Value="False">
                            <Setter Property="Foreground" Value="Red" />
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </TextBlock.Style>
        </TextBlock>
    </StackPanel>
</UserControl>
```

- [ ] **Step 6.2: Create code-behind**

```csharp
// SmartSolutions/SmartSolutions.App/Views/Steps/DatabaseStepControl.xaml.cs
using System.Windows.Controls;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views.Steps;

public partial class DatabaseStepControl : UserControl
{
    public DatabaseStepControl() => InitializeComponent();

    private void SqlPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
            vm.SqlPassword = SqlPasswordBox.Password;
    }
}
```

- [ ] **Step 6.3: Build**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 6.4: Commit**

```
git add SmartSolutions/SmartSolutions.App/Views/Steps/
git commit -m "feat: add DatabaseStepControl (wizard step 1)"
```

---

## Task 7: BusinessInfoStepControl (Step 2)

**Files:**
- Create: `SmartSolutions/SmartSolutions.App/Views/Steps/BusinessInfoStepControl.xaml`
- Create: `SmartSolutions/SmartSolutions.App/Views/Steps/BusinessInfoStepControl.xaml.cs`

- [ ] **Step 7.1: Create XAML**

```xml
<!-- SmartSolutions/SmartSolutions.App/Views/Steps/BusinessInfoStepControl.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.Steps.BusinessInfoStepControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <StackPanel>
        <TextBlock Text="Business Information"
                   Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                   FontWeight="Medium" Margin="0,0,0,4" />
        <TextBlock Text="This information appears on all printed invoices."
                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                   Margin="0,0,0,16" />

        <TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 materialDesign:HintAssist.Hint="Business Name *"
                 Text="{Binding BusinessName, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,12" />

        <TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 materialDesign:HintAssist.Hint="NTN   e.g. 7569020-2"
                 Text="{Binding Ntn, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,12" />

        <TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 materialDesign:HintAssist.Hint="Address"
                 Text="{Binding Address, UpdateSourceTrigger=PropertyChanged}"
                 AcceptsReturn="True" MinLines="2" MaxLines="3"
                 Margin="0,0,0,12" />

        <Grid Margin="0,0,0,12">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="8" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <TextBox Grid.Column="0"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     materialDesign:HintAssist.Hint="Phone 1"
                     Text="{Binding Phone1, UpdateSourceTrigger=PropertyChanged}" />
            <TextBox Grid.Column="2"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     materialDesign:HintAssist.Hint="Phone 2"
                     Text="{Binding Phone2, UpdateSourceTrigger=PropertyChanged}" />
        </Grid>

        <TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
                 materialDesign:HintAssist.Hint="Email"
                 Text="{Binding Email, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,12" />
    </StackPanel>
</UserControl>
```

- [ ] **Step 7.2: Create code-behind**

```csharp
// SmartSolutions/SmartSolutions.App/Views/Steps/BusinessInfoStepControl.xaml.cs
using System.Windows.Controls;

namespace SmartSolutions.App.Views.Steps;

public partial class BusinessInfoStepControl : UserControl
{
    public BusinessInfoStepControl() => InitializeComponent();
}
```

- [ ] **Step 7.3: Build + commit**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
git add SmartSolutions/SmartSolutions.App/Views/Steps/BusinessInfoStepControl.xaml SmartSolutions/SmartSolutions.App/Views/Steps/BusinessInfoStepControl.xaml.cs
git commit -m "feat: add BusinessInfoStepControl (wizard step 2)"
```

---

## Task 8: AdminPinStepControl (Step 3)

**Files:**
- Create: `SmartSolutions/SmartSolutions.App/Views/Steps/AdminPinStepControl.xaml`
- Create: `SmartSolutions/SmartSolutions.App/Views/Steps/AdminPinStepControl.xaml.cs`

- [ ] **Step 8.1: Create XAML**

```xml
<!-- SmartSolutions/SmartSolutions.App/Views/Steps/AdminPinStepControl.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.Steps.AdminPinStepControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <StackPanel>
        <TextBlock Text="Admin PIN"
                   Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                   FontWeight="Medium" Margin="0,0,0,8" />
        <TextBlock TextWrapping="Wrap" Margin="0,0,0,20"
                   Text="The default admin PIN is 0000. Set a new PIN now, or skip and change it later from the Users page." />

        <PasswordBox x:Name="PinBox"
                     Style="{StaticResource MaterialDesignOutlinedPasswordBox}"
                     materialDesign:HintAssist.Hint="New PIN (4 digits)"
                     MaxLength="4"
                     PasswordChanged="PinBox_PasswordChanged"
                     Margin="0,0,0,12" />

        <PasswordBox x:Name="ConfirmPinBox"
                     Style="{StaticResource MaterialDesignOutlinedPasswordBox}"
                     materialDesign:HintAssist.Hint="Confirm PIN"
                     MaxLength="4"
                     PasswordChanged="ConfirmPinBox_PasswordChanged"
                     Margin="0,0,0,8" />

        <TextBlock Text="PINs do not match"
                   Foreground="Red"
                   Visibility="{Binding PinMismatch, Converter={StaticResource BoolToVisibility}}" />
    </StackPanel>
</UserControl>
```

- [ ] **Step 8.2: Create code-behind**

```csharp
// SmartSolutions/SmartSolutions.App/Views/Steps/AdminPinStepControl.xaml.cs
using System.Windows.Controls;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views.Steps;

public partial class AdminPinStepControl : UserControl
{
    public AdminPinStepControl() => InitializeComponent();

    private void PinBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
            vm.AdminPin = PinBox.Password;
    }

    private void ConfirmPinBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
            vm.AdminPinConfirm = ConfirmPinBox.Password;
    }

    public void ClearPins()
    {
        PinBox.Password        = "";
        ConfirmPinBox.Password = "";
    }
}
```

- [ ] **Step 8.3: Build + commit**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
git add SmartSolutions/SmartSolutions.App/Views/Steps/AdminPinStepControl.xaml SmartSolutions/SmartSolutions.App/Views/Steps/AdminPinStepControl.xaml.cs
git commit -m "feat: add AdminPinStepControl (wizard step 3)"
```

---

## Task 9: SetupWizardWindow

**Files:**
- Create: `SmartSolutions/SmartSolutions.App/Views/SetupWizardWindow.xaml`
- Create: `SmartSolutions/SmartSolutions.App/Views/SetupWizardWindow.xaml.cs`

- [ ] **Step 9.1: Create XAML**

```xml
<!-- SmartSolutions/SmartSolutions.App/Views/SetupWizardWindow.xaml -->
<Window x:Class="SmartSolutions.App.Views.SetupWizardWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        xmlns:steps="clr-namespace:SmartSolutions.App.Views.Steps"
        Title="Smart Solutions — Setup"
        Width="520" SizeToContent="Height"
        MinHeight="460"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterScreen"
        TextElement.Foreground="{DynamicResource MaterialDesignBody}"
        Background="{DynamicResource MaterialDesignPaper}"
        FontFamily="{DynamicResource MaterialDesignFont}">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="{DynamicResource PrimaryHueMidBrush}" Padding="24,18">
            <StackPanel>
                <TextBlock Text="Smart Solutions"
                           Foreground="White" FontWeight="Bold" FontSize="20" />
                <TextBlock Text="First-Time Setup"
                           Foreground="White" Opacity="0.85" FontSize="13" Margin="0,2,0,0" />
            </StackPanel>
        </Border>

        <!-- Step indicator -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,20,0,4">

            <!-- Step 1 -->
            <StackPanel Width="90" HorizontalAlignment="Center">
                <Border Width="36" Height="36" CornerRadius="18" HorizontalAlignment="Center">
                    <Border.Style>
                        <Style TargetType="Border">
                            <Setter Property="Background" Value="#BDBDBD" />
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding CurrentStep}" Value="1">
                                    <Setter Property="Background" Value="{DynamicResource PrimaryHueMidBrush}" />
                                </DataTrigger>
                                <DataTrigger Binding="{Binding IsStep1Done}" Value="True">
                                    <Setter Property="Background" Value="{DynamicResource PrimaryHueLightBrush}" />
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Border.Style>
                    <Grid>
                        <TextBlock Text="1" HorizontalAlignment="Center" VerticalAlignment="Center"
                                   Foreground="White" FontWeight="Bold"
                                   Visibility="{Binding IsStep1Done, Converter={StaticResource InverseBoolToVisibility}}" />
                        <TextBlock Text="✓" HorizontalAlignment="Center" VerticalAlignment="Center"
                                   Foreground="White" FontWeight="Bold"
                                   Visibility="{Binding IsStep1Done, Converter={StaticResource BoolToVisibility}}" />
                    </Grid>
                </Border>
                <TextBlock Text="Database" HorizontalAlignment="Center" FontSize="11" Margin="0,4,0,0"
                           Foreground="{DynamicResource MaterialDesignBodyLight}" />
            </StackPanel>

            <Rectangle Width="40" Height="2" VerticalAlignment="Top" Margin="0,17,0,0" Fill="#E0E0E0" />

            <!-- Step 2 -->
            <StackPanel Width="90" HorizontalAlignment="Center">
                <Border Width="36" Height="36" CornerRadius="18" HorizontalAlignment="Center">
                    <Border.Style>
                        <Style TargetType="Border">
                            <Setter Property="Background" Value="#BDBDBD" />
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding CurrentStep}" Value="2">
                                    <Setter Property="Background" Value="{DynamicResource PrimaryHueMidBrush}" />
                                </DataTrigger>
                                <DataTrigger Binding="{Binding IsStep2Done}" Value="True">
                                    <Setter Property="Background" Value="{DynamicResource PrimaryHueLightBrush}" />
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Border.Style>
                    <Grid>
                        <TextBlock Text="2" HorizontalAlignment="Center" VerticalAlignment="Center"
                                   Foreground="White" FontWeight="Bold"
                                   Visibility="{Binding IsStep2Done, Converter={StaticResource InverseBoolToVisibility}}" />
                        <TextBlock Text="✓" HorizontalAlignment="Center" VerticalAlignment="Center"
                                   Foreground="White" FontWeight="Bold"
                                   Visibility="{Binding IsStep2Done, Converter={StaticResource BoolToVisibility}}" />
                    </Grid>
                </Border>
                <TextBlock Text="Business Info" HorizontalAlignment="Center" FontSize="11" Margin="0,4,0,0"
                           Foreground="{DynamicResource MaterialDesignBodyLight}" />
            </StackPanel>

            <Rectangle Width="40" Height="2" VerticalAlignment="Top" Margin="0,17,0,0" Fill="#E0E0E0" />

            <!-- Step 3 -->
            <StackPanel Width="90" HorizontalAlignment="Center">
                <Border Width="36" Height="36" CornerRadius="18" HorizontalAlignment="Center">
                    <Border.Style>
                        <Style TargetType="Border">
                            <Setter Property="Background" Value="#BDBDBD" />
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding CurrentStep}" Value="3">
                                    <Setter Property="Background" Value="{DynamicResource PrimaryHueMidBrush}" />
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Border.Style>
                    <TextBlock Text="3" HorizontalAlignment="Center" VerticalAlignment="Center"
                               Foreground="White" FontWeight="Bold" />
                </Border>
                <TextBlock Text="Admin PIN" HorizontalAlignment="Center" FontSize="11" Margin="0,4,0,0"
                           Foreground="{DynamicResource MaterialDesignBodyLight}" />
            </StackPanel>

        </StackPanel>

        <!-- Step content -->
        <ScrollViewer Grid.Row="2" VerticalScrollBarVisibility="Auto" Padding="28,12,28,4">
            <Grid>
                <steps:DatabaseStepControl
                    Visibility="{Binding CurrentStep, Converter={StaticResource EqualToVisibility}, ConverterParameter=1}" />
                <steps:BusinessInfoStepControl
                    Visibility="{Binding CurrentStep, Converter={StaticResource EqualToVisibility}, ConverterParameter=2}" />
                <steps:AdminPinStepControl x:Name="AdminPinStep"
                    Visibility="{Binding CurrentStep, Converter={StaticResource EqualToVisibility}, ConverterParameter=3}" />
            </Grid>
        </ScrollViewer>

        <!-- Button row -->
        <Border Grid.Row="3"
                BorderThickness="0,1,0,0"
                BorderBrush="{DynamicResource MaterialDesignDivider}"
                Padding="24,12">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="8" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>

                <Button Grid.Column="0"
                        Command="{Binding BackCommand}"
                        Content="← Back"
                        Style="{StaticResource MaterialDesignOutlinedButton}"
                        Visibility="{Binding IsFirstStep, Converter={StaticResource InverseBoolToVisibility}}" />

                <Button Grid.Column="2"
                        x:Name="SkipButton"
                        Click="SkipButton_Click"
                        Content="Skip for now"
                        Style="{StaticResource MaterialDesignFlatButton}"
                        Visibility="{Binding IsLastStep, Converter={StaticResource BoolToVisibility}}" />

                <Button Grid.Column="4"
                        Command="{Binding NextCommand}"
                        Content="Next →"
                        Style="{StaticResource MaterialDesignRaisedButton}"
                        Visibility="{Binding IsLastStep, Converter={StaticResource InverseBoolToVisibility}}" />

                <Button Grid.Column="4"
                        Command="{Binding FinishCommand}"
                        Content="Finish"
                        Style="{StaticResource MaterialDesignRaisedButton}"
                        Visibility="{Binding IsLastStep, Converter={StaticResource BoolToVisibility}}" />
            </Grid>
        </Border>

    </Grid>
</Window>
```

- [ ] **Step 9.2: Create code-behind**

```csharp
// SmartSolutions/SmartSolutions.App/Views/SetupWizardWindow.xaml.cs
using System.Windows;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views;

public partial class SetupWizardWindow : Window
{
    public SetupWizardWindow()
    {
        InitializeComponent();
        var vm = new SetupWizardViewModel();
        vm.SetupCompleted += () => DialogResult = true;
        DataContext = vm;
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        AdminPinStep.ClearPins();
        if (DataContext is SetupWizardViewModel vm)
            vm.FinishWithSkip();
    }
}
```

- [ ] **Step 9.3: Build**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 9.4: Commit**

```
git add SmartSolutions/SmartSolutions.App/Views/SetupWizardWindow.xaml SmartSolutions/SmartSolutions.App/Views/SetupWizardWindow.xaml.cs
git commit -m "feat: add SetupWizardWindow with three-step wizard shell"
```

---

## Task 10: Update App.xaml.cs startup flow

**Files:**
- Modify: `SmartSolutions/SmartSolutions.App/App.xaml.cs`

- [ ] **Step 10.1: Replace OnStartup**

```csharp
// SmartSolutions.App/App.xaml.cs
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutions.App.Helpers;
using SmartSolutions.App.ViewModels;
using SmartSolutions.App.Views;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;

namespace SmartSolutions.App;

public partial class App : Application
{
    private IHost _host = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Show wizard before building the host if this is a first run
        if (SettingsManager.IsSetupRequired())
        {
            var wizard = new SetupWizardWindow();
            bool completed = wizard.ShowDialog() == true;
            if (!completed)
            {
                Application.Current.Shutdown();
                return;
            }
        }

        _host = Host.CreateDefaultBuilder()
            .ConfigureSmartSolutions()
            .Build();

        await _host.StartAsync();

        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Database.MigrateAsync();

            var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
            if (!await auth.AnyUserExistsAsync())
                await auth.CreateAsync("admin", "0000");

            // Apply first-run wizard data if present
            var settings = SettingsManager.Load();
            if (settings.FirstRunData is { } frd)
            {
                var bi = await db.BusinessInfos.FindAsync(1);
                if (bi != null)
                {
                    bi.Name    = frd.BusinessName;
                    bi.Ntn     = frd.Ntn;
                    bi.Address = frd.Address;
                    bi.Phone1  = frd.Phone1;
                    bi.Phone2  = string.IsNullOrEmpty(frd.Phone2) ? null : frd.Phone2;
                    bi.Email   = string.IsNullOrEmpty(frd.Email)  ? null : frd.Email;
                    await db.SaveChangesAsync();
                }

                if (!string.IsNullOrEmpty(frd.AdminPin))
                {
                    var users = await auth.GetAllAsync();
                    var admin = users.FirstOrDefault(u => u.Username == "admin");
                    if (admin != null)
                        await auth.UpdatePinAsync(admin.Id, frd.AdminPin);
                }

                SettingsManager.ClearFirstRunData();
            }
        }

        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        var vm = (LoginViewModel)loginWindow.DataContext;

        bool loginSucceeded = false;

        vm.LoginSucceeded += user =>
        {
            loginSucceeded = true;
            var session = _host.Services.GetRequiredService<ISessionService>();
            session.Login(user);
            loginWindow.Close();

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.WindowState = WindowState.Maximized;
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        };

        loginWindow.Closing += (_, _) =>
        {
            if (!loginSucceeded)
                Application.Current.Shutdown();
        };

        loginWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 10.2: Build**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
```

Expected: Build succeeded.

- [ ] **Step 10.3: Smoke-test the wizard**

Delete `%LOCALAPPDATA%\SmartSolutions\appsettings.json` if it exists, then run the app. The wizard should appear before login. Complete all three steps with a real SQL Express connection string. Confirm the app proceeds to login, then to main window. Verify `%LOCALAPPDATA%\SmartSolutions\appsettings.json` exists and contains only `ConnectionStrings` (no `FirstRunData`).

- [ ] **Step 10.4: Commit**

```
git add SmartSolutions/SmartSolutions.App/App.xaml.cs
git commit -m "feat: wire first-run wizard into startup flow with BusinessInfo + PIN seeding"
```

---

## Task 11: Add MSIX packaging

**Files:**
- Modify: `SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj`
- Create: `SmartSolutions/SmartSolutions.App/Package.appxmanifest`
- Create: `SmartSolutions/SmartSolutions.App/Assets/*.png` (5 files)

- [ ] **Step 11.1: Generate placeholder PNG assets**

Run from the repo root (`D:\Documents\Programs\Visual Studio 2026\Smart Solutions`):

```powershell
Add-Type -AssemblyName System.Drawing

$assetsDir = "SmartSolutions\SmartSolutions.App\Assets"
New-Item -ItemType Directory -Force $assetsDir | Out-Null

$assets = [ordered]@{
    "Square44x44Logo.png"   = @(44,  44)
    "Square150x150Logo.png" = @(150, 150)
    "Wide310x150Logo.png"   = @(310, 150)
    "StoreLogo.png"         = @(50,  50)
    "SplashScreen.png"      = @(620, 300)
}

foreach ($name in $assets.Keys) {
    $w = $assets[$name][0]; $h = $assets[$name][1]
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(103, 58, 183))
    $sz   = [Math]::Max(8, [Math]::Min($w, $h) / 4)
    $font = New-Object System.Drawing.Font "Arial", $sz, ([System.Drawing.FontStyle]::Bold)
    $sf   = New-Object System.Drawing.StringFormat
    $sf.Alignment     = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = [System.Drawing.RectangleF]::new(0, 0, $w, $h)
    $g.DrawString("SS", $font, [System.Drawing.Brushes]::White, $rect, $sf)
    $g.Dispose(); $font.Dispose()
    $bmp.Save("$assetsDir\$name", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Created $name"
}
```

Expected output: five "Created ..." lines.

- [ ] **Step 11.2: Generate self-signed certificate**

```powershell
$cert = New-SelfSignedCertificate `
    -Subject "CN=SmartSolutions" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -Type CodeSigningCert `
    -HashAlgorithm SHA256 `
    -FriendlyName "SmartSolutions Package Signing"

$pfxPwd = ConvertTo-SecureString -String "SS2024Dev!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert `
    -FilePath "SmartSolutions\SmartSolutions.App\SmartSolutions.pfx" `
    -Password $pfxPwd

Export-Certificate -Cert $cert `
    -FilePath "SmartSolutions\SmartSolutions.App\SmartSolutions.cer" `
    -Type CERT

Write-Host "Certificate thumbprint: $($cert.Thumbprint)"
```

Expected: two files created, thumbprint printed.

- [ ] **Step 11.3: Create Package.appxmanifest**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity
    Name="SmartSolutions.App"
    Publisher="CN=SmartSolutions"
    Version="1.0.0.0" />

  <Properties>
    <DisplayName>Smart Solutions</DisplayName>
    <PublisherDisplayName>Smart Solutions</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.26200.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-US" />
  </Resources>

  <Applications>
    <Application Id="App"
                 Executable="$targetnametoken$.exe"
                 EntryPoint="$targetentrypoint$">
      <uap:VisualElements
        DisplayName="Smart Solutions"
        Description="Smart Solutions Record Keeping App"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>

</Package>
```

Save to `SmartSolutions/SmartSolutions.App/Package.appxmanifest`.

- [ ] **Step 11.4: Update SmartSolutions.App.csproj**

Replace the entire file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <WindowsPackageType>MSIX</WindowsPackageType>
    <PackageCertificateKeyFile>SmartSolutions.pfx</PackageCertificateKeyFile>
    <PackageCertificatePassword>SS2024Dev!</PackageCertificatePassword>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\SmartSolutions.Core\SmartSolutions.Core.csproj" />
    <ProjectReference Include="..\SmartSolutions.Data\SmartSolutions.Data.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
    <PackageReference Include="FastReport.OpenSource" Version="2026.2.1" />
    <PackageReference Include="FastReport.OpenSource.Export.PdfSimple" Version="2026.2.1" />
    <PackageReference Include="MaterialDesignThemes" Version="5.3.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.8">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.8" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.8" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="Reports\Invoice.frx">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <ItemGroup>
    <Content Include="Assets\Square44x44Logo.png" />
    <Content Include="Assets\Square150x150Logo.png" />
    <Content Include="Assets\Wide310x150Logo.png" />
    <Content Include="Assets\StoreLogo.png" />
    <Content Include="Assets\SplashScreen.png" />
  </ItemGroup>

</Project>
```

- [ ] **Step 11.5: Add .pfx to .gitignore**

Open `SmartSolutions/.gitignore` and add at the end:

```
# MSIX signing certificate (private key — do not commit)
*.pfx
```

- [ ] **Step 11.6: Build**

```
dotnet build SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj -c Release
```

Expected: Build succeeded. (A `.msix` is produced in the output folder when building via VS → Package & Publish; `dotnet build` alone confirms the packaging configuration is valid.)

- [ ] **Step 11.7: Commit**

```
git add SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj
git add SmartSolutions/SmartSolutions.App/Package.appxmanifest
git add SmartSolutions/SmartSolutions.App/Assets/
git add SmartSolutions/SmartSolutions.App/SmartSolutions.cer
git add SmartSolutions/.gitignore
git commit -m "feat: add MSIX packaging with self-signed cert and placeholder assets"
```

---

## Task 12: Create INSTALL.md

**Files:**
- Create: `docs/INSTALL.md`

- [ ] **Step 12.1: Create guide**

```markdown
# Smart Solutions — Installation Guide

## Prerequisites (one-time per PC)

Before installing the `.msix` package, the signing certificate must be trusted on the target machine.

### Install the signing certificate

1. Copy `SmartSolutions.cer` to the target PC.
2. Open PowerShell **as Administrator** and run:

```powershell
Import-Certificate -FilePath "SmartSolutions.cer" -CertStoreLocation Cert:\LocalMachine\Root
```

Or manually:
1. Double-click `SmartSolutions.cer`
2. Click **Install Certificate**
3. Select **Local Machine** → Next
4. Select **Place all certificates in the following store** → Browse → **Trusted Root Certification Authorities**
5. Finish

## Install the app

Double-click `SmartSolutions.App_1.0.0.0_x64.msix` and follow the prompts.

## First-run setup

On first launch the Setup Wizard will appear:

1. **Database** — enter the server name (e.g. `SERVER-PC\SQLEXPRESS`), confirm the database name (`SmartSolutions`), choose authentication, and click **Test Connection**. Proceed when the connection test passes.
2. **Business Info** — enter your business name (required), NTN, address, and phone numbers. These appear on all printed invoices.
3. **Admin PIN** — set a new PIN for the `admin` account (4 digits), or click **Skip for now** to keep the default `0000` and change it later from the Users page.

## Connection string location

The connection string is stored at:

```
%LOCALAPPDATA%\SmartSolutions\appsettings.json
```

To reconfigure the database connection (e.g. after moving SQL Server to a different PC), delete this file and relaunch the app — the Setup Wizard will reappear.
```

- [ ] **Step 12.2: Commit**

```
git add docs/INSTALL.md
git commit -m "docs: add MSIX installation guide"
```

---

## Task 13: Update PRD and CLAUDE.md

**Files:**
- Modify: `docs/superpowers/specs/2026-06-09-smart-solutions-design.md`
- Modify: `CLAUDE.md`

- [ ] **Step 13.1: Update PRD Section 6 (Deployment)**

Replace Section 6 content with:

```markdown
## 6. Deployment

- The app is distributed as a sideloadable `.msix` package signed with a self-signed certificate.
- Each PC must have the `SmartSolutions.cer` certificate installed to the Trusted Root store before installing (one-time, see `docs/INSTALL.md`).
- On first launch, a **Setup Wizard** runs before the login screen. It collects: SQL Server connection details, business information (name, NTN, address, phone — used on invoices), and a new admin PIN.
- The connection string is written to `%LOCALAPPDATA%\SmartSolutions\appsettings.json` on each PC. The MSIX install directory is read-only; this path is always writable.
- To reconfigure the database on a PC, delete `%LOCALAPPDATA%\SmartSolutions\appsettings.json` and relaunch.
- No internet connection required.
- App runs on Windows 10 (build 17763+) and Windows 11.
```

- [ ] **Step 13.2: Update PRD Section 8.5 (Settings)**

Replace the Settings section body with:

```markdown
### 8.5 Settings

Settings contains only:

| Section | Managed Items |
|---------|--------------|
| Business Info | Name, NTN, address, phones, email, logo (used on invoices) |

> **Note:** Database connection string is configured via the first-run Setup Wizard (stored in `%LOCALAPPDATA%\SmartSolutions\appsettings.json`). It is not editable from within the app after first run. To reconfigure, delete the file and relaunch.
```

- [ ] **Step 13.3: Update CLAUDE.md — Deployment section**

In CLAUDE.md, find the line:
```
- All other PCs install the app and connect via a connection string stored in `appsettings.json`.
```

Replace the entire Deployment section of CLAUDE.md (under `## 6. Deployment` if present, or add near the top under a relevant heading) to include:

```markdown
## Deployment

- Distributed as `.msix` (single-project MSIX via `<WindowsPackageType>MSIX</WindowsPackageType>` in the `.csproj`).
- Self-signed cert (`SmartSolutions.pfx` / `.cer`) in `SmartSolutions.App/`. The `.pfx` is gitignored (private key); `.cer` is committed (public, safe to distribute).
- On first launch: `SettingsManager.IsSetupRequired()` detects missing `%LOCALAPPDATA%\SmartSolutions\appsettings.json` → shows `SetupWizardWindow` before building the DI host.
- Wizard writes the connection string + `FirstRunData` to LocalAppData. After migrations, `App.xaml.cs` seeds `BusinessInfo` (row Id=1, always present via `HasData`) and admin PIN from `FirstRunData`, then calls `ClearFirstRunData()`.
- `ServiceConfiguration.cs` clears default config sources and loads exclusively from `SettingsManager.SettingsFilePath`.
```

- [ ] **Step 13.4: Add Design Decision to PRD**

In Section 13 (Design Decisions Log) of the PRD, append:

```markdown
| MSIX packaging | Single-project MSIX; sideloading with self-signed cert; `runFullTrust` for SQL Server + LocalAppData access | 2026-06-10 |
| First-run wizard | Three steps (DB connection, business info, admin PIN); runs before DI host is built; uses raw `SqlConnection` for test | 2026-06-10 |
| Settings file location | `%LOCALAPPDATA%\SmartSolutions\appsettings.json` — writable under MSIX; `FirstRunData` section removed after first-launch seed | 2026-06-10 |
```

- [ ] **Step 13.5: Commit**

```
git add docs/superpowers/specs/2026-06-09-smart-solutions-design.md CLAUDE.md
git commit -m "docs: update PRD and CLAUDE.md with MSIX and first-run wizard details"
```

---

## Self-Review Checklist

| Spec requirement | Task |
|---|---|
| Single-project MSIX | Task 11 |
| `runFullTrust` capability | Task 11 (manifest) |
| Self-signed cert, sideloading | Task 11 |
| `SettingsManager` owns LocalAppData file | Task 2 |
| `ServiceConfiguration` reads from LocalAppData | Task 3 |
| Wizard runs before host | Task 10 |
| Step 1: field-based CS assembly + live test + Next locked until pass | Tasks 5, 6 |
| Step 2: business info, Next requires BusinessName | Tasks 5, 7 |
| Step 3: PIN fields, mismatch indicator, Skip | Tasks 5, 8, 9 |
| Wizard shell: step indicator, back/next/finish/skip buttons | Task 9 |
| Seed `BusinessInfo` (update row Id=1) | Task 10 |
| Seed admin PIN via `UpdatePinAsync` | Task 10 |
| `ClearFirstRunData` after seed | Task 10 |
| Re-run safety (idempotent seed) | Task 10 |
| Install guide | Task 12 |
| PRD + CLAUDE.md updated | Task 13 |
