# User Authentication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add username + PIN login, stamp every record with the creator's ID, and open the app maximized.

**Architecture:** A new `AppUser` entity is stored in the database. `IAuthService` handles PIN hashing and user CRUD. `ISessionService` (singleton) holds the logged-in user for the process lifetime. A `LoginWindow` shown at startup gates access to `MainWindow`. Six existing entities gain nullable `CreatedById`/`RecordedById` FK columns. Existing services receive `ISessionService` and stamp those columns on create/add operations.

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, EF Core 10, MaterialDesignInXamlToolkit, SQL Server Express, xUnit, FluentAssertions

---

## File Map

**New files:**
- `SmartSolutions/SmartSolutions.Data/Entities/AppUser.cs`
- `SmartSolutions/SmartSolutions.Core/Interfaces/IAuthService.cs`
- `SmartSolutions/SmartSolutions.Core/Interfaces/ISessionService.cs`
- `SmartSolutions/SmartSolutions.Core/Services/AuthService.cs`
- `SmartSolutions/SmartSolutions.Core/Services/SessionService.cs`
- `SmartSolutions/SmartSolutions.App/Views/LoginWindow.xaml`
- `SmartSolutions/SmartSolutions.App/Views/LoginWindow.xaml.cs`
- `SmartSolutions/SmartSolutions.App/ViewModels/LoginViewModel.cs`
- `SmartSolutions/SmartSolutions.Tests/Services/AuthServiceTests.cs`
- `SmartSolutions/SmartSolutions.Tests/Helpers/TestSessionService.cs`

**Modified files:**
- `SmartSolutions/SmartSolutions.Data/Entities/PrintOrder.cs` — add `CreatedById`
- `SmartSolutions/SmartSolutions.Data/Entities/HaierJob.cs` — add `CreatedById`
- `SmartSolutions/SmartSolutions.Data/Entities/Expense.cs` — add `CreatedById`
- `SmartSolutions/SmartSolutions.Data/Entities/PrintOrderPayment.cs` — add `RecordedById`
- `SmartSolutions/SmartSolutions.Data/Entities/HaierJobPayment.cs` — add `RecordedById`
- `SmartSolutions/SmartSolutions.Data/Entities/Customer.cs` — add `CreatedById`
- `SmartSolutions/SmartSolutions.Data/AppDbContext.cs` — add `AppUsers` DbSet + FK config
- `SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs` — register new services + windows
- `SmartSolutions/SmartSolutions.App/App.xaml.cs` — new startup flow with LoginWindow
- `SmartSolutions/SmartSolutions.App/MainWindow.xaml` — add logged-in label to sidebar
- `SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs` — inject ISessionService, expose username
- `SmartSolutions/SmartSolutions.App/ViewModels/SettingsViewModel.cs` — add Users section
- `SmartSolutions/SmartSolutions.App/Views/SettingsView.xaml` — add Users card
- `SmartSolutions/SmartSolutions.Core/Services/ExpenseService.cs` — inject session, stamp CreatedById
- `SmartSolutions/SmartSolutions.Core/Services/CustomerService.cs` — inject session, stamp CreatedById
- `SmartSolutions/SmartSolutions.Core/Services/PrintOrderService.cs` — inject session, stamp CreatedById + RecordedById
- `SmartSolutions/SmartSolutions.Core/Services/HaierJobService.cs` — inject session, stamp CreatedById + RecordedById
- `SmartSolutions/SmartSolutions.Tests/Services/PrintOrderServiceTests.cs` — pass TestSessionService
- `SmartSolutions/SmartSolutions.Tests/Services/HaierJobServiceTests.cs` — pass TestSessionService

---

## Task 1: AppUser entity + DbContext registration

**Files:**
- Create: `SmartSolutions/SmartSolutions.Data/Entities/AppUser.cs`
- Modify: `SmartSolutions/SmartSolutions.Data/AppDbContext.cs`

- [ ] **Step 1: Create AppUser entity**

```csharp
// SmartSolutions.Data/Entities/AppUser.cs
namespace SmartSolutions.Data.Entities;

public class AppUser
{
    public int    Id        { get; set; }
    public string Username  { get; set; } = "";
    public string PinHash   { get; set; } = "";
    public bool   IsActive  { get; set; } = true;
}
```

- [ ] **Step 2: Add DbSet and FK configuration to AppDbContext**

Add after the existing `DbSet` declarations (line ~23 in `AppDbContext.cs`):
```csharp
public DbSet<AppUser> AppUsers { get; set; }
```

Add inside `OnModelCreating`, after the existing enum converters block, before the `decimal(18,2)` loop:
```csharp
modelBuilder.Entity<AppUser>()
    .HasIndex(u => u.Username)
    .IsUnique();

// Nullable FK from audit-trail entities to AppUser — no cascade delete
foreach (var fk in modelBuilder.Model.GetEntityTypes()
    .SelectMany(e => e.GetForeignKeys())
    .Where(fk => fk.PrincipalEntityType.ClrType == typeof(AppUser)))
{
    fk.DeleteBehavior = DeleteBehavior.NoAction;
}
```

- [ ] **Step 3: Build the project to confirm no errors**

Run from `SmartSolutions/` directory:
```
dotnet build SmartSolutions.sln
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 4: Commit**

```
git add SmartSolutions/SmartSolutions.Data/Entities/AppUser.cs SmartSolutions/SmartSolutions.Data/AppDbContext.cs
git commit -m "feat: add AppUser entity and DbContext registration"
```

---

## Task 2: ISessionService + SessionService

**Files:**
- Create: `SmartSolutions/SmartSolutions.Core/Interfaces/ISessionService.cs`
- Create: `SmartSolutions/SmartSolutions.Core/Services/SessionService.cs`

- [ ] **Step 1: Create ISessionService interface**

```csharp
// SmartSolutions.Core/Interfaces/ISessionService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface ISessionService
{
    bool      IsLoggedIn  { get; }
    AppUser   CurrentUser { get; }
    void      Login(AppUser user);
}
```

- [ ] **Step 2: Create SessionService implementation**

```csharp
// SmartSolutions.Core/Services/SessionService.cs
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class SessionService : ISessionService
{
    private AppUser? _currentUser;

    public bool IsLoggedIn => _currentUser is not null;

    public AppUser CurrentUser => _currentUser
        ?? throw new InvalidOperationException("No user is logged in.");

    public void Login(AppUser user)
    {
        if (_currentUser is not null)
            throw new InvalidOperationException("A user is already logged in.");
        _currentUser = user;
    }
}
```

- [ ] **Step 3: Build to confirm no errors**

```
dotnet build SmartSolutions.sln
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 4: Commit**

```
git add SmartSolutions/SmartSolutions.Core/Interfaces/ISessionService.cs SmartSolutions/SmartSolutions.Core/Services/SessionService.cs
git commit -m "feat: add ISessionService and SessionService"
```

---

## Task 3: IAuthService + AuthService with tests

**Files:**
- Create: `SmartSolutions/SmartSolutions.Core/Interfaces/IAuthService.cs`
- Create: `SmartSolutions/SmartSolutions.Core/Services/AuthService.cs`
- Create: `SmartSolutions/SmartSolutions.Tests/Services/AuthServiceTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// SmartSolutions.Tests/Services/AuthServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsUserWithHashedPin()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());

        await svc.CreateAsync("owner", "1234");

        var all = await svc.GetAllAsync();
        all.Should().ContainSingle(u => u.Username == "owner");
        all[0].PinHash.Should().NotBe("1234");
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenUsernameAlreadyExists()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("owner", "1234");

        var act = async () => await svc.CreateAsync("owner", "9999");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsUser_WhenCredentialsMatch()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("boy1", "0000");

        var result = await svc.ValidateAsync("boy1", "0000");

        result.Should().NotBeNull();
        result!.Username.Should().Be("boy1");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenPinWrong()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("boy1", "0000");

        var result = await svc.ValidateAsync("boy1", "9999");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenUsernameNotFound()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());

        var result = await svc.ValidateAsync("nobody", "0000");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenUserInactive()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("boy1", "0000");
        var all = await svc.GetAllAsync();
        await svc.SetActiveAsync(all[0].Id, false);

        var result = await svc.ValidateAsync("boy1", "0000");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePinAsync_AllowsLoginWithNewPin()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("owner", "0000");
        var user = (await svc.GetAllAsync())[0];

        await svc.UpdatePinAsync(user.Id, "5678");

        (await svc.ValidateAsync("owner", "5678")).Should().NotBeNull();
        (await svc.ValidateAsync("owner", "0000")).Should().BeNull();
    }

    [Fact]
    public async Task SetActiveAsync_TogglesIsActive()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("owner", "0000");
        var user = (await svc.GetAllAsync())[0];

        await svc.SetActiveAsync(user.Id, false);
        var afterDeactivate = (await svc.GetAllAsync())[0];
        afterDeactivate.IsActive.Should().BeFalse();

        await svc.SetActiveAsync(user.Id, true);
        var afterReactivate = (await svc.GetAllAsync())[0];
        afterReactivate.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AnyUserExistsAsync_ReturnsFalse_WhenEmpty()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());

        (await svc.AnyUserExistsAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task AnyUserExistsAsync_ReturnsTrue_WhenUsersExist()
    {
        var svc = new AuthService(TestDbContextFactory.Unique());
        await svc.CreateAsync("owner", "0000");

        (await svc.AnyUserExistsAsync()).Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test SmartSolutions/SmartSolutions.Tests/SmartSolutions.Tests.csproj --filter "FullyQualifiedName~AuthServiceTests" -v n
```
Expected: compilation failure (`AuthService` not defined yet).

- [ ] **Step 3: Create IAuthService interface**

```csharp
// SmartSolutions.Core/Interfaces/IAuthService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface IAuthService
{
    Task<AppUser?>       ValidateAsync(string username, string pin);
    Task<IList<AppUser>> GetAllAsync();
    Task                 CreateAsync(string username, string pin);
    Task                 UpdatePinAsync(int userId, string newPin);
    Task                 SetActiveAsync(int userId, bool isActive);
    Task<bool>           AnyUserExistsAsync();
}
```

- [ ] **Step 4: Create AuthService implementation**

```csharp
// SmartSolutions.Core/Services/AuthService.cs
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class AuthService(IDbContextFactory<AppDbContext> factory) : IAuthService
{
    public async Task<AppUser?> ValidateAsync(string username, string pin)
    {
        await using var db = factory.CreateDbContext();
        var user = await db.AppUsers
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        if (user is null) return null;
        return VerifyPin(pin, user.PinHash) ? user : null;
    }

    public async Task<IList<AppUser>> GetAllAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.AppUsers.OrderBy(u => u.Username).ToListAsync();
    }

    public async Task CreateAsync(string username, string pin)
    {
        await using var db = factory.CreateDbContext();
        if (await db.AppUsers.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException($"Username '{username}' already exists.");
        db.AppUsers.Add(new AppUser { Username = username, PinHash = HashPin(pin) });
        await db.SaveChangesAsync();
    }

    public async Task UpdatePinAsync(int userId, string newPin)
    {
        await using var db = factory.CreateDbContext();
        var user = await db.AppUsers.FindAsync(userId)
            ?? throw new InvalidOperationException($"AppUser {userId} not found.");
        user.PinHash = HashPin(newPin);
        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int userId, bool isActive)
    {
        await using var db = factory.CreateDbContext();
        var user = await db.AppUsers.FindAsync(userId)
            ?? throw new InvalidOperationException($"AppUser {userId} not found.");
        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task<bool> AnyUserExistsAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.AppUsers.AnyAsync();
    }

    private static string HashPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            pin, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var combined = new byte[48]; // 16 salt + 32 hash
        salt.CopyTo(combined, 0);
        hash.CopyTo(combined, 16);
        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPin(string pin, string storedHash)
    {
        byte[] combined;
        try { combined = Convert.FromBase64String(storedHash); }
        catch { return false; }
        if (combined.Length != 48) return false;
        var salt = combined[..16];
        var stored = combined[16..];
        var computed = Rfc2898DeriveBytes.Pbkdf2(
            pin, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(stored, computed);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```
dotnet test SmartSolutions/SmartSolutions.Tests/SmartSolutions.Tests.csproj --filter "FullyQualifiedName~AuthServiceTests" -v n
```
Expected: all 9 tests pass.

- [ ] **Step 6: Commit**

```
git add SmartSolutions/SmartSolutions.Core/Interfaces/IAuthService.cs SmartSolutions/SmartSolutions.Core/Services/AuthService.cs SmartSolutions/SmartSolutions.Tests/Services/AuthServiceTests.cs
git commit -m "feat: add IAuthService and AuthService with PBKDF2 PIN hashing"
```

---

## Task 4: Audit trail FK columns on existing entities

**Files:**
- Modify: `SmartSolutions/SmartSolutions.Data/Entities/PrintOrder.cs`
- Modify: `SmartSolutions/SmartSolutions.Data/Entities/HaierJob.cs`
- Modify: `SmartSolutions/SmartSolutions.Data/Entities/Expense.cs`
- Modify: `SmartSolutions/SmartSolutions.Data/Entities/PrintOrderPayment.cs`
- Modify: `SmartSolutions/SmartSolutions.Data/Entities/HaierJobPayment.cs`
- Modify: `SmartSolutions/SmartSolutions.Data/Entities/Customer.cs`

- [ ] **Step 1: Add CreatedById to PrintOrder**

Add these two lines after `public string? Notes { get; set; }` in `PrintOrder.cs`:
```csharp
public int?     CreatedById { get; set; }
public AppUser? CreatedBy   { get; set; }
```

Full file after change:
```csharp
// SmartSolutions.Data/Entities/PrintOrder.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrder
{
    public int    Id                    { get; set; }
    public int    CustomerId            { get; set; }
    public Customer Customer            { get; set; } = null!;
    public DateTime Date                { get; set; }
    public PrintOrderStatus Status      { get; set; } = PrintOrderStatus.Draft;
    public decimal? TransportationCharges { get; set; }
    public string? Notes                { get; set; }
    public int?    CreatedById          { get; set; }
    public AppUser? CreatedBy           { get; set; }

    public ICollection<PrintOrderLine>             Lines             { get; set; } = [];
    public ICollection<PrintOrderVendorAssignment> VendorAssignments { get; set; } = [];
    public ICollection<PrintOrderPayment>          Payments          { get; set; } = [];
}
```

- [ ] **Step 2: Add CreatedById to HaierJob**

Add after `public string? Notes { get; set; }` in `HaierJob.cs`:
```csharp
public int?     CreatedById { get; set; }
public AppUser? CreatedBy   { get; set; }
```

- [ ] **Step 3: Add CreatedById to Expense**

Add after `public DateTime Date { get; set; }` in `Expense.cs`:
```csharp
public int?     CreatedById { get; set; }
public AppUser? CreatedBy   { get; set; }
```

Full file after change:
```csharp
// SmartSolutions.Data/Entities/Expense.cs
namespace SmartSolutions.Data.Entities;

public class Expense
{
    public int    Id          { get; set; }
    public int    CategoryId  { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Amount     { get; set; }
    public int    ChannelId   { get; set; }
    public PaymentChannel Channel { get; set; } = null!;
    public DateTime Date      { get; set; }
    public int?    CreatedById { get; set; }
    public AppUser? CreatedBy  { get; set; }
}
```

- [ ] **Step 4: Add RecordedById to PrintOrderPayment**

Add after `public string? Notes { get; set; }` in `PrintOrderPayment.cs`:
```csharp
public int?     RecordedById { get; set; }
public AppUser? RecordedBy   { get; set; }
```

- [ ] **Step 5: Add RecordedById to HaierJobPayment**

Add after `public string? Notes { get; set; }` in `HaierJobPayment.cs`:
```csharp
public int?     RecordedById { get; set; }
public AppUser? RecordedBy   { get; set; }
```

- [ ] **Step 6: Add CreatedById to Customer**

Read `Customer.cs` first to find the last field, then add:
```csharp
public int?     CreatedById { get; set; }
public AppUser? CreatedBy   { get; set; }
```

- [ ] **Step 7: Build to confirm no errors**

```
dotnet build SmartSolutions.sln
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 8: Commit**

```
git add SmartSolutions/SmartSolutions.Data/Entities/PrintOrder.cs SmartSolutions/SmartSolutions.Data/Entities/HaierJob.cs SmartSolutions/SmartSolutions.Data/Entities/Expense.cs SmartSolutions/SmartSolutions.Data/Entities/PrintOrderPayment.cs SmartSolutions/SmartSolutions.Data/Entities/HaierJobPayment.cs SmartSolutions/SmartSolutions.Data/Entities/Customer.cs
git commit -m "feat: add CreatedById/RecordedById audit columns to entities"
```

---

## Task 5: EF migration

**Files:**
- Create: `SmartSolutions/SmartSolutions.Data/Migrations/` (auto-generated)

- [ ] **Step 1: Run the migration**

From the solution root (`SmartSolutions/SmartSolutions.sln` directory — i.e., `SmartSolutions/`):
```
dotnet ef migrations add AddAuthAndAuditTrail --project SmartSolutions.Data --startup-project SmartSolutions.App
```
Expected: `Build succeeded` followed by `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 2: Verify the generated migration**

Open the new file `SmartSolutions/SmartSolutions.Data/Migrations/<timestamp>_AddAuthAndAuditTrail.cs` and confirm it contains:
- A `CreateTable` for `AppUsers` with columns `Id`, `Username`, `PinHash`, `IsActive`
- `AddColumn` operations adding `CreatedById` to `PrintOrders`, `HaierJobs`, `Expenses`, `Customers`
- `AddColumn` operations adding `RecordedById` to `PrintOrderPayments`, `HaierJobPayments`
- `CreateIndex` for the unique index on `AppUsers.Username`

- [ ] **Step 3: Build to confirm no errors**

```
dotnet build SmartSolutions.sln
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 4: Commit**

```
git add SmartSolutions/SmartSolutions.Data/Migrations/
git commit -m "feat: EF migration AddAuthAndAuditTrail"
```

---

## Task 6: LoginWindow + LoginViewModel

**Files:**
- Create: `SmartSolutions/SmartSolutions.App/Views/LoginWindow.xaml`
- Create: `SmartSolutions/SmartSolutions.App/Views/LoginWindow.xaml.cs`
- Create: `SmartSolutions/SmartSolutions.App/ViewModels/LoginViewModel.cs`

- [ ] **Step 1: Create LoginViewModel**

```csharp
// SmartSolutions.App/ViewModels/LoginViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;

namespace SmartSolutions.App.ViewModels;

public partial class LoginViewModel(IAuthService auth) : ObservableObject
{
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = "";

    [ObservableProperty]
    private string _errorMessage = "";

    public string Pin { get; set; } = "";

    public event Action? LoginSucceeded;

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
```

Wait — `LoginSucceeded` must pass the user back so `App.xaml.cs` can call `session.Login(user)`. Update the event signature:

```csharp
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
```

- [ ] **Step 2: Create LoginWindow XAML**

```xml
<!-- SmartSolutions.App/Views/LoginWindow.xaml -->
<Window x:Class="SmartSolutions.App.Views.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="Smart Solutions — Login"
        Width="360" Height="300"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        TextElement.Foreground="{DynamicResource MaterialDesignBody}"
        Background="{DynamicResource MaterialDesignPaper}"
        FontFamily="{md:MaterialDesignFont}">

    <StackPanel Margin="40,32">
        <TextBlock Text="Smart Solutions"
                   Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                   HorizontalAlignment="Center" Margin="0,0,0,24"/>

        <TextBox x:Name="UsernameBox"
                 md:HintAssist.Hint="Username"
                 Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,16"/>

        <PasswordBox x:Name="PinBox"
                     md:HintAssist.Hint="PIN"
                     Margin="0,0,0,8"/>

        <TextBlock Text="{Binding ErrorMessage}"
                   Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                   FontSize="12" Margin="0,0,0,16"
                   Visibility="{Binding ErrorMessage, Converter={StaticResource NullOrEmptyToVisibilityConverter}}"/>

        <Button Content="Login"
                Click="OnLoginClicked"
                IsEnabled="{Binding LoginCommand.CanExecute}"
                Style="{StaticResource MaterialDesignRaisedButton}"
                HorizontalAlignment="Stretch"/>
    </StackPanel>
</Window>
```

Note: `NullOrEmptyToVisibilityConverter` may not exist — use `BoolToVisibilityConverter` with a trigger instead. Replace the `TextBlock` with:
```xml
<TextBlock Text="{Binding ErrorMessage}"
           Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
           FontSize="12" Margin="0,0,0,16"
           Visibility="{Binding ErrorMessage,
               Converter={StaticResource BoolToVisibilityConverter},
               ConverterParameter=inverse}"
           />
```

Actually, use a simpler approach — just always show the TextBlock but it'll be empty when no error:
```xml
<TextBlock Text="{Binding ErrorMessage}"
           Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
           FontSize="12" Margin="0,0,0,16"
           MinHeight="20"/>
```

Full corrected XAML:

```xml
<!-- SmartSolutions.App/Views/LoginWindow.xaml -->
<Window x:Class="SmartSolutions.App.Views.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="Smart Solutions — Login"
        Width="360" Height="280"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        TextElement.Foreground="{DynamicResource MaterialDesignBody}"
        Background="{DynamicResource MaterialDesignPaper}"
        FontFamily="{md:MaterialDesignFont}">

    <StackPanel Margin="40,32">
        <TextBlock Text="Smart Solutions"
                   Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                   HorizontalAlignment="Center" Margin="0,0,0,24"/>

        <TextBox x:Name="UsernameBox"
                 md:HintAssist.Hint="Username"
                 Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,16"/>

        <PasswordBox x:Name="PinBox"
                     md:HintAssist.Hint="PIN"
                     Margin="0,0,0,4"/>

        <TextBlock Text="{Binding ErrorMessage}"
                   Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                   FontSize="12" MinHeight="20" Margin="0,0,0,12"/>

        <Button Content="Login"
                Click="OnLoginClicked"
                IsEnabled="{Binding LoginCommand.CanExecute}"
                Style="{StaticResource MaterialDesignRaisedButton}"
                HorizontalAlignment="Stretch"/>
    </StackPanel>
</Window>
```

- [ ] **Step 3: Create LoginWindow code-behind**

```csharp
// SmartSolutions.App/Views/LoginWindow.xaml.cs
using System.Windows;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow() => InitializeComponent();

    private async void OnLoginClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm) return;
        vm.Pin = PinBox.Password;
        await vm.LoginCommand.ExecuteAsync(null);
        PinBox.Clear();
    }
}
```

- [ ] **Step 4: Build to confirm no errors**

```
dotnet build SmartSolutions.sln
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 5: Commit**

```
git add SmartSolutions/SmartSolutions.App/Views/LoginWindow.xaml SmartSolutions/SmartSolutions.App/Views/LoginWindow.xaml.cs SmartSolutions/SmartSolutions.App/ViewModels/LoginViewModel.cs
git commit -m "feat: add LoginWindow and LoginViewModel"
```

---

## Task 7: ServiceConfiguration + App.xaml.cs startup flow

**Files:**
- Modify: `SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs`
- Modify: `SmartSolutions/SmartSolutions.App/App.xaml.cs`

- [ ] **Step 1: Update ServiceConfiguration**

Replace the entire file:
```csharp
// SmartSolutions.App/ServiceConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutions.App.Services;
using SmartSolutions.App.Views;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Core.Services;
using SmartSolutions.Data;

namespace SmartSolutions.App;

public static class ServiceConfiguration
{
    public static IHostBuilder ConfigureSmartSolutions(this IHostBuilder builder) =>
        builder.ConfigureServices((context, services) =>
        {
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(
                    context.Configuration.GetConnectionString("Default")));

            // Auth — ISessionService is the one process-lifetime singleton
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<IAuthService,    AuthService>();

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
            services.AddTransient<ViewModels.LoginViewModel>();

            services.AddSingleton<MainWindow>();
            services.AddTransient<LoginWindow>();
        });
}
```

- [ ] **Step 2: Update App.xaml.cs**

Replace the entire file:
```csharp
// SmartSolutions.App/App.xaml.cs
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutions.App.ViewModels;
using SmartSolutions.App.Views;
using SmartSolutions.Core.Interfaces;

namespace SmartSolutions.App;

public partial class App : Application
{
    private IHost _host = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _host = Host.CreateDefaultBuilder()
            .ConfigureSmartSolutions()
            .Build();

        await _host.StartAsync();

        // Run EF migrations
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<SmartSolutions.Data.AppDbContext>>()
                .CreateDbContext();
            await db.Database.MigrateAsync();
        }

        // Bootstrap default admin user on first run
        var auth = _host.Services.GetRequiredService<IAuthService>();
        if (!await auth.AnyUserExistsAsync())
            await auth.CreateAsync("admin", "0000");

        // Show login window before main window
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var loginVm = _host.Services.GetRequiredService<LoginViewModel>();
        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        loginWindow.DataContext = loginVm;

        loginVm.LoginSucceeded += user =>
        {
            _host.Services.GetRequiredService<ISessionService>().Login(user);
            loginWindow.Close();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
            mainWindow.WindowState = WindowState.Maximized;
            mainWindow.Show();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        };

        loginWindow.Closed += (_, _) =>
        {
            if (!_host.Services.GetRequiredService<ISessionService>().IsLoggedIn)
                Shutdown();
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

- [ ] **Step 3: Build to confirm no errors**

```
dotnet build SmartSolutions.sln
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 4: Commit**

```
git add SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs SmartSolutions/SmartSolutions.App/App.xaml.cs
git commit -m "feat: wire auth into DI and add LoginWindow startup flow"
```

---

## Task 8: Show logged-in username in MainWindow sidebar

**Files:**
- Modify: `SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions/SmartSolutions.App/MainWindow.xaml`

- [ ] **Step 1: Update MainViewModel to inject ISessionService**

Replace the entire file:
```csharp
// SmartSolutions.App/ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.Core.Interfaces;

namespace SmartSolutions.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly ISessionService  _session;

    public MainViewModel(IServiceProvider services, ISessionService session)
    {
        _services = services;
        _session  = session;
        NavigateToDashboard();
    }

    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private string _currentSection = "Dashboard";

    public string LoggedInUsername => _session.CurrentUser.Username;

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentSection = "Dashboard";
        CurrentView = _services.GetRequiredService<DashboardViewModel>();
        _ = ((DashboardViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToPrintOrders()
    {
        CurrentSection = "Print Orders";
        CurrentView = _services.GetRequiredService<PrintOrdersViewModel>();
        _ = ((PrintOrdersViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToHaierJobs()
    {
        CurrentSection = "Haier Jobs";
        CurrentView = _services.GetRequiredService<HaierJobsViewModel>();
        _ = ((HaierJobsViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToExpenses()
    {
        CurrentSection = "Expenses";
        CurrentView = _services.GetRequiredService<ExpensesViewModel>();
        _ = ((ExpensesViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        CurrentSection = "Reports";
        CurrentView = _services.GetRequiredService<ReportsViewModel>();
        _ = ((ReportsViewModel)CurrentView!).LoadAsync();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentSection = "Settings";
        CurrentView = _services.GetRequiredService<SettingsViewModel>();
        _ = ((SettingsViewModel)CurrentView!).LoadAsync();
    }
}
```

- [ ] **Step 2: Add logged-in label to MainWindow sidebar**

In `MainWindow.xaml`, inside the `<StackPanel Margin="0,16,0,0">` that makes up the sidebar, add this block after the `<Separator>` and `Settings` button, at the bottom of the StackPanel:

```xml
<TextBlock Text="{Binding LoggedInUsername, StringFormat='👤 {0}'}"
           Foreground="White" Opacity="0.75" FontSize="11"
           Margin="16,16,8,16"/>
```

The sidebar StackPanel should end like this:
```xml
                <Separator Background="White" Opacity="0.3" Margin="16,16" />
                <Button Content="Settings"     Command="{Binding NavigateToSettingsCommand}"
                        Style="{StaticResource MaterialDesignFlatButton}"
                        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2" />
                <TextBlock Text="{Binding LoggedInUsername, StringFormat='Logged in as: {0}'}"
                           Foreground="White" Opacity="0.75" FontSize="11"
                           Margin="16,16,8,16"/>
            </StackPanel>
```

- [ ] **Step 3: Build to confirm no errors**

```
dotnet build SmartSolutions.sln
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 4: Commit**

```
git add SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs SmartSolutions/SmartSolutions.App/MainWindow.xaml
git commit -m "feat: show logged-in username in MainWindow sidebar"
```

---

## Task 9: Settings — Users section

**Files:**
- Modify: `SmartSolutions/SmartSolutions.App/ViewModels/SettingsViewModel.cs`
- Modify: `SmartSolutions/SmartSolutions.App/Views/SettingsView.xaml`
- Modify: `SmartSolutions/SmartSolutions.App/Views/SettingsView.xaml.cs`

- [ ] **Step 1: Add Users section to SettingsViewModel**

Change the class declaration to inject `IAuthService` and `ISessionService`:
```csharp
public partial class SettingsViewModel(
    ILookupService lookup,
    IAuthService auth,
    ISessionService session) : ObservableObject
```

Add the using statements at the top of the file:
```csharp
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
```

Add these observable properties in the Users region (add near the top with the other properties):
```csharp
// ── Users ────────────────────────────────────────────────────────────
[ObservableProperty] private ObservableCollection<AppUser> _users = [];
[ObservableProperty] private AppUser? _selectedUser;
[ObservableProperty] private string _newUserUsername = "";
public string NewUserPin   { get; set; } = ""; // set from code-behind before command
public string ResetUserPin { get; set; } = ""; // set from code-behind before command
```

In `LoadAsync`, add inside the `try` block after `BusinessInfo = ...`:
```csharp
Users = new(await auth.GetAllAsync());
```

Add these commands at the end of the class, before the closing brace:
```csharp
// ── User commands ─────────────────────────────────────────────────────

[RelayCommand]
private async Task AddUserAsync()
{
    if (string.IsNullOrWhiteSpace(NewUserUsername) || string.IsNullOrWhiteSpace(NewUserPin)) return;
    try
    {
        await auth.CreateAsync(NewUserUsername.Trim(), NewUserPin);
        Users = new(await auth.GetAllAsync());
        NewUserUsername = "";
        StatusMessage = "User added.";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Failed to add user: {ex.Message}";
    }
}

[RelayCommand]
private async Task ResetUserPinAsync()
{
    if (SelectedUser is null || string.IsNullOrWhiteSpace(ResetUserPin)) return;
    try
    {
        await auth.UpdatePinAsync(SelectedUser.Id, ResetUserPin);
        StatusMessage = "PIN reset successfully.";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Failed to reset PIN: {ex.Message}";
    }
}

[RelayCommand]
private async Task ToggleUserActiveAsync()
{
    if (SelectedUser is null) return;
    if (SelectedUser.Id == session.CurrentUser.Id)
    {
        StatusMessage = "Cannot deactivate the currently logged-in user.";
        return;
    }
    var action = SelectedUser.IsActive ? "Deactivate" : "Reactivate";
    if (!DialogHelper.Confirm($"{action} user '{SelectedUser.Username}'?")) return;
    try
    {
        await auth.SetActiveAsync(SelectedUser.Id, !SelectedUser.IsActive);
        Users = new(await auth.GetAllAsync());
        SelectedUser = null;
    }
    catch (Exception ex)
    {
        StatusMessage = $"Failed to update user: {ex.Message}";
    }
}
```

- [ ] **Step 2: Add Users card to SettingsView.xaml**

Append this card to the `SettingsView.xaml` `<StackPanel>`, after the existing Business Info card and before the `</StackPanel>`:

```xml
<!-- Users -->
<md:Card Margin="0,0,0,16" Padding="16">
  <StackPanel>
    <TextBlock Text="Users" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
    <ListBox ItemsSource="{Binding Users}"
             SelectedItem="{Binding SelectedUser}"
             Height="140" Margin="0,0,0,8">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <StackPanel Orientation="Horizontal">
            <TextBlock Text="{Binding Username}" FontWeight="SemiBold" Margin="0,0,8,0"/>
            <TextBlock Text="(inactive)" Foreground="Gray"
                       Visibility="{Binding IsActive,
                           Converter={StaticResource InverseBoolToVisibilityConverter}}"/>
          </StackPanel>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>

    <Grid Margin="0,0,0,8">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="8"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="8"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <TextBox Grid.Column="0" x:Name="NewUsernameBox"
               md:HintAssist.Hint="New username"
               Text="{Binding NewUserUsername, UpdateSourceTrigger=PropertyChanged}"/>
      <PasswordBox Grid.Column="2" x:Name="NewUserPinBox"
                   md:HintAssist.Hint="PIN" Width="100"/>
      <Button Grid.Column="4" Content="Add User"
              Click="OnAddUserClicked"
              Style="{StaticResource MaterialDesignOutlinedButton}"/>
    </Grid>

    <Grid Margin="0,0,0,8">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="8"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="8"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <PasswordBox Grid.Column="0" x:Name="ResetPinBox"
                   md:HintAssist.Hint="New PIN for selected user" Width="200"/>
      <Button Grid.Column="2" Content="Reset PIN"
              Click="OnResetPinClicked"
              Style="{StaticResource MaterialDesignOutlinedButton}"/>
      <Button Grid.Column="4" Content="Deactivate / Reactivate"
              Command="{Binding ToggleUserActiveCommand}"
              Style="{StaticResource MaterialDesignOutlinedButton}"
              Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
    </Grid>
  </StackPanel>
</md:Card>
```

- [ ] **Step 3: Update SettingsView code-behind**

Replace `SettingsView.xaml.cs`:
```csharp
// SmartSolutions.App/Views/SettingsView.xaml.cs
using System.Windows.Controls;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private async void OnAddUserClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;
        vm.NewUserPin = NewUserPinBox.Password;
        await vm.AddUserCommand.ExecuteAsync(null);
        NewUserPinBox.Clear();
    }

    private async void OnResetPinClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;
        vm.ResetUserPin = ResetPinBox.Password;
        await vm.ResetUserPinCommand.ExecuteAsync(null);
        ResetPinBox.Clear();
    }
}
```

- [ ] **Step 4: Build to confirm no errors**

```
dotnet build SmartSolutions.sln
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 5: Commit**

```
git add SmartSolutions/SmartSolutions.App/ViewModels/SettingsViewModel.cs SmartSolutions/SmartSolutions.App/Views/SettingsView.xaml SmartSolutions/SmartSolutions.App/Views/SettingsView.xaml.cs
git commit -m "feat: add Users section to Settings"
```

---

## Task 10: Stamp CreatedById/RecordedById in existing services + fix tests

**Files:**
- Create: `SmartSolutions/SmartSolutions.Tests/Helpers/TestSessionService.cs`
- Modify: `SmartSolutions/SmartSolutions.Core/Services/ExpenseService.cs`
- Modify: `SmartSolutions/SmartSolutions.Core/Services/CustomerService.cs`
- Modify: `SmartSolutions/SmartSolutions.Core/Services/PrintOrderService.cs`
- Modify: `SmartSolutions/SmartSolutions.Core/Services/HaierJobService.cs`
- Modify: `SmartSolutions/SmartSolutions.Tests/Services/PrintOrderServiceTests.cs`
- Modify: `SmartSolutions/SmartSolutions.Tests/Services/HaierJobServiceTests.cs`

- [ ] **Step 1: Create TestSessionService test helper**

```csharp
// SmartSolutions.Tests/Helpers/TestSessionService.cs
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Tests.Helpers;

public class TestSessionService(int userId = 1, string username = "testuser") : ISessionService
{
    public bool IsLoggedIn => true;
    public AppUser CurrentUser => new() { Id = userId, Username = username, PinHash = "", IsActive = true };
    public void Login(AppUser user) { }
}
```

- [ ] **Step 2: Update ExpenseService to stamp CreatedById**

Change the constructor signature:
```csharp
public class ExpenseService(IDbContextFactory<AppDbContext> factory, ISessionService session) : IExpenseService
```

In `AddExpenseAsync`, add `CreatedById = session.CurrentUser.Id` to the entity initialiser:
```csharp
public async Task<Expense> AddExpenseAsync(int categoryId, string? description,
    decimal amount, int channelId, DateTime date)
{
    await using var db = factory.CreateDbContext();
    var entity = new Expense
    {
        CategoryId  = categoryId,
        Description = description,
        Amount      = amount,
        ChannelId   = channelId,
        Date        = date.ToUniversalTime(),
        CreatedById = session.CurrentUser.Id
    };
    db.Expenses.Add(entity);
    await db.SaveChangesAsync();
    return entity;
}
```

- [ ] **Step 3: Update CustomerService to stamp CreatedById**

Read `CustomerService.cs` first. Change its constructor to add `ISessionService session`. In `AddCustomerAsync`, add `CreatedById = session.CurrentUser.Id` to the new `Customer` object.

The pattern is identical to ExpenseService:
```csharp
public class CustomerService(IDbContextFactory<AppDbContext> factory, ISessionService session) : ICustomerService
```

In `AddCustomerAsync`:
```csharp
var entity = new Customer
{
    Name    = name,
    Phone   = phone,
    Address = address,
    Notes   = notes,
    CreatedById = session.CurrentUser.Id
};
```

- [ ] **Step 4: Update PrintOrderService to stamp CreatedById and RecordedById**

Read `PrintOrderService.cs` first. Change its constructor to add `ISessionService session`. Make two changes:

In `CreateOrderAsync`, add `CreatedById = session.CurrentUser.Id` to the new `PrintOrder`:
```csharp
var entity = new PrintOrder
{
    CustomerId  = customerId,
    Date        = date.ToUniversalTime(),
    Notes       = notes,
    CreatedById = session.CurrentUser.Id
};
```

In `AddPaymentAsync`, add `RecordedById = session.CurrentUser.Id` to the new `PrintOrderPayment`:
```csharp
var payment = new PrintOrderPayment
{
    OrderId     = orderId,
    Amount      = amount,
    ChannelId   = channelId,
    Date        = date.ToUniversalTime(),
    Notes       = notes,
    RecordedById = session.CurrentUser.Id
};
```

- [ ] **Step 5: Update HaierJobService to stamp CreatedById and RecordedById**

Read `HaierJobService.cs` first. Change its constructor to add `ISessionService session`.

In `CreateJobAsync`, add `CreatedById = session.CurrentUser.Id` to the new `HaierJob`.

In `AddPaymentAsync`, add `RecordedById = session.CurrentUser.Id` to the new `HaierJobPayment`.

- [ ] **Step 6: Run all tests — expect failures in PrintOrderServiceTests and HaierJobServiceTests**

```
dotnet test SmartSolutions/SmartSolutions.Tests/SmartSolutions.Tests.csproj -v n
```
Expected: PrintOrderServiceTests and HaierJobServiceTests fail (missing ISessionService argument), others pass.

- [ ] **Step 7: Fix PrintOrderServiceTests — pass TestSessionService**

Open `PrintOrderServiceTests.cs`. Everywhere `PrintOrderService` is instantiated (in each test method), add `new TestSessionService()` as the second argument:
```csharp
// Before:
var svc = new PrintOrderService(factory);
// After:
var svc = new PrintOrderService(factory, new TestSessionService());
```

- [ ] **Step 8: Fix HaierJobServiceTests — pass TestSessionService**

Same fix in `HaierJobServiceTests.cs`:
```csharp
// Before:
var svc = new HaierJobService(factory);
// After:
var svc = new HaierJobService(factory, new TestSessionService());
```

- [ ] **Step 9: Run all tests — all should pass**

```
dotnet test SmartSolutions/SmartSolutions.Tests/SmartSolutions.Tests.csproj -v n
```
Expected: all tests pass, 0 failures.

- [ ] **Step 10: Commit**

```
git add SmartSolutions/SmartSolutions.Tests/Helpers/TestSessionService.cs SmartSolutions/SmartSolutions.Core/Services/ExpenseService.cs SmartSolutions/SmartSolutions.Core/Services/CustomerService.cs SmartSolutions/SmartSolutions.Core/Services/PrintOrderService.cs SmartSolutions/SmartSolutions.Core/Services/HaierJobService.cs SmartSolutions/SmartSolutions.Tests/Services/PrintOrderServiceTests.cs SmartSolutions/SmartSolutions.Tests/Services/HaierJobServiceTests.cs
git commit -m "feat: stamp CreatedById/RecordedById in services; fix tests"
```

---

## Self-Review

**Spec coverage check:**

| Spec requirement | Covered by |
|---|---|
| AppUser entity: Id, Username, PinHash, IsActive | Task 1 |
| Seed default admin/0000 | Task 7 (App.xaml.cs bootstrap) |
| IAuthService: Validate, GetAll, Create, UpdatePin, SetActive | Task 3 |
| ISessionService singleton: CurrentUser, IsLoggedIn, Login | Task 2 |
| PBKDF2-SHA256, 100k iterations, salt embedded | Task 3 AuthService |
| LoginWindow shown before MainWindow | Task 6 + 7 |
| Closing LoginWindow without login exits app | Task 7 App.xaml.cs Closed handler |
| MainWindow opens maximized | Task 7 App.xaml.cs |
| "Logged in as" label in sidebar | Task 8 |
| User management in Settings (add/reset PIN/toggle active) | Task 9 |
| Cannot deactivate currently logged-in user | Task 9 SettingsViewModel |
| CreatedById on PrintOrder, HaierJob, Expense, Customer | Task 4 |
| RecordedById on PrintOrderPayment, HaierJobPayment | Task 4 |
| All nullable (existing rows not broken) | Task 4 |
| EF migration | Task 5 |
| Services stamp creator on create/add | Task 10 |
| Existing tests updated | Task 10 |

All spec requirements covered. No gaps found.
