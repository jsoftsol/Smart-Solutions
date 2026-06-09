# Customers Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fully functional Customers management page with list, add, edit, and delete, wired into the sidebar navigation.

**Architecture:** Follows the identical pattern used by Vendors and Technicians pages — `md:DialogHost` inline dialog, `ObservableCollection` bound DataGrid, service injected into ViewModel via constructor. The service layer (`ICustomerService` / `CustomerService`) already exists; only `DeleteCustomerAsync` is missing. No EF migration is needed — the `Customers` table was created in `InitialCreate`.

**Tech Stack:** .NET 10, C#, WPF, CommunityToolkit.Mvvm, MaterialDesignInXamlToolkit, EF Core 10 (in-memory for tests), xUnit + FluentAssertions

---

## File Map

| Action | Path |
|--------|------|
| Modify | `SmartSolutions.Core/Interfaces/ICustomerService.cs` |
| Modify | `SmartSolutions.Core/Services/CustomerService.cs` |
| Create | `SmartSolutions.Tests/Services/CustomerServiceTests.cs` |
| Create | `SmartSolutions.App/ViewModels/CustomersViewModel.cs` |
| Create | `SmartSolutions.App/Views/CustomersView.xaml` |
| Create | `SmartSolutions.App/Views/CustomersView.xaml.cs` |
| Modify | `SmartSolutions.App/ServiceConfiguration.cs` |
| Modify | `SmartSolutions.App/ViewModels/MainViewModel.cs` |
| Modify | `SmartSolutions.App/MainWindow.xaml` |

---

## Task 1: Add `DeleteCustomerAsync` to interface and service

**Files:**
- Modify: `SmartSolutions.Core/Interfaces/ICustomerService.cs`
- Modify: `SmartSolutions.Core/Services/CustomerService.cs`
- Create: `SmartSolutions.Tests/Services/CustomerServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `SmartSolutions.Tests/Services/CustomerServiceTests.cs`:

```csharp
// SmartSolutions.Tests/Services/CustomerServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task AddCustomerAsync_PersistsAndReturnsCustomer()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());

        var result = await svc.AddCustomerAsync("Ali Khan", "03001234567", "Peshawar", null);

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Ali Khan");
        result.Phone.Should().Be("03001234567");
        result.Address.Should().Be("Peshawar");
        result.Notes.Should().BeNull();
    }

    [Fact]
    public async Task SearchCustomersAsync_NoQuery_ReturnsAll()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());
        await svc.AddCustomerAsync("Alice", null, null, null);
        await svc.AddCustomerAsync("Bob", null, null, null);

        var result = await svc.SearchCustomersAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchCustomersAsync_ByName_FiltersResults()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());
        await svc.AddCustomerAsync("Ali Khan", "03001234567", null, null);
        await svc.AddCustomerAsync("Bilal Ahmed", null, null, null);

        var result = await svc.SearchCustomersAsync("Ali");

        result.Should().ContainSingle(c => c.Name == "Ali Khan");
        result.Should().NotContain(c => c.Name == "Bilal Ahmed");
    }

    [Fact]
    public async Task UpdateCustomerAsync_ChangesFields()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());
        var customer = await svc.AddCustomerAsync("Old Name", null, null, null);

        await svc.UpdateCustomerAsync(customer.Id, "New Name", "0300", "Addr", "Note");

        var updated = await svc.GetCustomerAsync(customer.Id);
        updated.Name.Should().Be("New Name");
        updated.Phone.Should().Be("0300");
        updated.Address.Should().Be("Addr");
        updated.Notes.Should().Be("Note");
    }

    [Fact]
    public async Task DeleteCustomerAsync_RemovesCustomer()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());
        var customer = await svc.AddCustomerAsync("ToDelete", null, null, null);

        await svc.DeleteCustomerAsync(customer.Id);

        var all = await svc.SearchCustomersAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCustomerAsync_NotFound_Throws()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new CustomerService(factory, new TestSessionService());

        var act = async () => await svc.DeleteCustomerAsync(999);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*999*");
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```
cd SmartSolutions
dotnet test SmartSolutions.Tests --filter "CustomerServiceTests" --no-build 2>&1 | head -30
```

Expected: compile error — `DeleteCustomerAsync` does not exist yet.

- [ ] **Step 3: Add `DeleteCustomerAsync` to the interface**

Open `SmartSolutions.Core/Interfaces/ICustomerService.cs`. Replace the entire file with:

```csharp
// SmartSolutions.Core/Interfaces/ICustomerService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> SearchCustomersAsync(string? query = null);
    Task<Customer>       GetCustomerAsync(int id);
    Task<Customer>       AddCustomerAsync(string name, string? phone, string? address, string? notes);
    Task                 UpdateCustomerAsync(int id, string name, string? phone, string? address, string? notes);
    Task                 DeleteCustomerAsync(int id);
}
```

- [ ] **Step 4: Implement `DeleteCustomerAsync` in `CustomerService`**

Open `SmartSolutions.Core/Services/CustomerService.cs`. Add this method before the closing brace:

```csharp
    public async Task DeleteCustomerAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException($"Customer {id} not found");
        db.Customers.Remove(entity);
        await db.SaveChangesAsync();
    }
```

The full file should now read:

```csharp
// SmartSolutions.Core/Services/CustomerService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class CustomerService(IDbContextFactory<AppDbContext> factory, ISessionService session) : ICustomerService
{
    public async Task<List<Customer>> SearchCustomersAsync(string? query = null)
    {
        await using var db = factory.CreateDbContext();
        var q = db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(c => c.Name.Contains(query) || (c.Phone != null && c.Phone.Contains(query)));
        return await q.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Customer> GetCustomerAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        return await db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException($"Customer {id} not found");
    }

    public async Task<Customer> AddCustomerAsync(string name, string? phone, string? address, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new Customer { Name = name, Phone = phone, Address = address, Notes = notes, CreatedById = session.CurrentUser.Id };
        db.Customers.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateCustomerAsync(int id, string name, string? phone, string? address, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException($"Customer {id} not found");
        entity.Name = name; entity.Phone = phone; entity.Address = address; entity.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteCustomerAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Customers.FindAsync(id)
            ?? throw new InvalidOperationException($"Customer {id} not found");
        db.Customers.Remove(entity);
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: Run all customer tests — expect green**

```
cd SmartSolutions
dotnet test SmartSolutions.Tests --filter "CustomerServiceTests" -v normal
```

Expected: 6 tests pass.

- [ ] **Step 6: Run full test suite — expect no regressions**

```
dotnet test SmartSolutions.Tests -v normal
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```
git add SmartSolutions.Core/Interfaces/ICustomerService.cs SmartSolutions.Core/Services/CustomerService.cs SmartSolutions.Tests/Services/CustomerServiceTests.cs
git commit -m "feat: add DeleteCustomerAsync to CustomerService"
```

---

## Task 2: Create `CustomersViewModel`

**Files:**
- Create: `SmartSolutions.App/ViewModels/CustomersViewModel.cs`

- [ ] **Step 1: Create the file**

Create `SmartSolutions.App/ViewModels/CustomersViewModel.cs`:

```csharp
// SmartSolutions.App/ViewModels/CustomersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class CustomersViewModel(ICustomerService customerService) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<Customer> _customers = [];
    [ObservableProperty] private bool   _isDialogOpen;
    [ObservableProperty] private string _dialogTitle = "";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogPhone = "";
    [ObservableProperty] private string _dialogAddress = "";
    [ObservableProperty] private string _dialogNotes = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Customers = new(await customerService.SearchCustomersAsync()); }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode = false; _editingId = 0;
        DialogTitle = "Add Customer";
        DialogName = ""; DialogPhone = ""; DialogAddress = ""; DialogNotes = "";
        DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Customer customer)
    {
        _isEditMode = true; _editingId = customer.Id;
        DialogTitle = "Edit Customer";
        DialogName = customer.Name;
        DialogPhone = customer.Phone ?? "";
        DialogAddress = customer.Address ?? "";
        DialogNotes = customer.Notes ?? "";
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
                var added = await customerService.AddCustomerAsync(
                    DialogName.Trim(),
                    NullIfEmpty(DialogPhone),
                    NullIfEmpty(DialogAddress),
                    NullIfEmpty(DialogNotes));
                Customers.Add(added);
            }
            else
            {
                await customerService.UpdateCustomerAsync(
                    _editingId,
                    DialogName.Trim(),
                    NullIfEmpty(DialogPhone),
                    NullIfEmpty(DialogAddress),
                    NullIfEmpty(DialogNotes));
                var c = Customers.First(x => x.Id == _editingId);
                var idx = Customers.IndexOf(c);
                c.Name    = DialogName.Trim();
                c.Phone   = NullIfEmpty(DialogPhone);
                c.Address = NullIfEmpty(DialogAddress);
                c.Notes   = NullIfEmpty(DialogNotes);
                Customers.RemoveAt(idx); Customers.Insert(idx, c);
            }
            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task DeleteAsync(Customer customer)
    {
        if (!DialogHelper.Confirm($"Delete customer '{customer.Name}'?")) return;
        try
        {
            await customerService.DeleteCustomerAsync(customer.Id);
            Customers.Remove(customer);
        }
        catch (DbUpdateException)
        {
            StatusMessage = "Cannot delete a customer who has existing orders.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to delete: {ex.Message}";
        }
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
```

- [ ] **Step 2: Build to confirm no errors**

```
cd SmartSolutions
dotnet build SmartSolutions.App --no-restore 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```
git add SmartSolutions.App/ViewModels/CustomersViewModel.cs
git commit -m "feat: add CustomersViewModel"
```

---

## Task 3: Create `CustomersView.xaml`

**Files:**
- Create: `SmartSolutions.App/Views/CustomersView.xaml`
- Create: `SmartSolutions.App/Views/CustomersView.xaml.cs`

- [ ] **Step 1: Create the XAML view**

Create `SmartSolutions.App/Views/CustomersView.xaml`:

```xml
<UserControl x:Class="SmartSolutions.App.Views.CustomersView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <md:DialogHost IsOpen="{Binding IsDialogOpen}" CloseOnClickAway="False">
        <md:DialogHost.DialogContent>
            <StackPanel Margin="24" MinWidth="360">
                <TextBlock Text="{Binding DialogTitle}"
                           Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,16"/>
                <TextBox md:HintAssist.Hint="Name *"
                         Text="{Binding DialogName, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Phone"
                         Text="{Binding DialogPhone, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Address"
                         Text="{Binding DialogAddress, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Notes"
                         Text="{Binding DialogNotes, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBlock Text="{Binding DialogErrorMessage}"
                           Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                           MinHeight="20" TextWrapping="Wrap"/>
                <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                    <Button Content="Save" Command="{Binding SaveDialogCommand}"
                            Style="{StaticResource MaterialDesignRaisedButton}" Margin="0,0,8,0"/>
                    <Button Content="Cancel" Command="{Binding CancelDialogCommand}"
                            Style="{StaticResource MaterialDesignOutlinedButton}"/>
                </StackPanel>
            </StackPanel>
        </md:DialogHost.DialogContent>

        <Grid Margin="24">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <StackPanel Grid.Row="0" Margin="0,0,0,16">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="Customers" Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                               VerticalAlignment="Center" Margin="0,0,16,0"/>
                    <Button Content="+ Add Customer" Command="{Binding OpenAddDialogCommand}"
                            Style="{StaticResource MaterialDesignRaisedButton}"/>
                </StackPanel>
                <TextBlock Text="{Binding StatusMessage}"
                           Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                           MinHeight="20" TextWrapping="Wrap"/>
            </StackPanel>
            <DataGrid Grid.Row="1" ItemsSource="{Binding Customers}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Name"    Binding="{Binding Name}"    Width="*"/>
                    <DataGridTextColumn Header="Phone"   Binding="{Binding Phone}"   Width="150"/>
                    <DataGridTextColumn Header="Address" Binding="{Binding Address}" Width="200"/>
                    <DataGridTextColumn Header="Notes"   Binding="{Binding Notes}"   Width="200"/>
                    <DataGridTemplateColumn Width="130">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal">
                                    <Button Content="Edit"
                                            Command="{Binding DataContext.OpenEditDialogCommand,
                                                      RelativeSource={RelativeSource AncestorType=UserControl}}"
                                            CommandParameter="{Binding}"
                                            Style="{StaticResource MaterialDesignFlatButton}" Padding="4,0"/>
                                    <Button Content="Delete"
                                            Command="{Binding DataContext.DeleteCommand,
                                                      RelativeSource={RelativeSource AncestorType=UserControl}}"
                                            CommandParameter="{Binding}"
                                            Style="{StaticResource MaterialDesignFlatButton}"
                                            Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                                            Padding="4,0"/>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Grid>
    </md:DialogHost>
</UserControl>
```

- [ ] **Step 2: Create the code-behind**

Create `SmartSolutions.App/Views/CustomersView.xaml.cs`:

```csharp
// SmartSolutions.App/Views/CustomersView.xaml.cs
using System.Windows.Controls;

namespace SmartSolutions.App.Views;

public partial class CustomersView : UserControl
{
    public CustomersView() => InitializeComponent();
}
```

- [ ] **Step 3: Build to confirm no errors**

```
cd SmartSolutions
dotnet build SmartSolutions.App --no-restore 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add SmartSolutions.App/Views/CustomersView.xaml SmartSolutions.App/Views/CustomersView.xaml.cs
git commit -m "feat: add CustomersView"
```

---

## Task 4: Wire up navigation and DI registration

**Files:**
- Modify: `SmartSolutions.App/ServiceConfiguration.cs`
- Modify: `SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions.App/MainWindow.xaml`

- [ ] **Step 1: Register `CustomersViewModel` in DI**

Open `SmartSolutions.App/ServiceConfiguration.cs`. After the line:

```csharp
            services.AddTransient<ViewModels.ItemsViewModel>();
```

Add:

```csharp
            services.AddTransient<ViewModels.CustomersViewModel>();
```

The management ViewModel registrations block should now read:

```csharp
            services.AddTransient<ViewModels.ItemsViewModel>();
            services.AddTransient<ViewModels.CustomersViewModel>();
            services.AddTransient<ViewModels.VendorsViewModel>();
            services.AddTransient<ViewModels.TechniciansViewModel>();
            services.AddTransient<ViewModels.ExpenseCategoriesViewModel>();
            services.AddTransient<ViewModels.PaymentChannelsViewModel>();
            services.AddTransient<ViewModels.UsersViewModel>();
```

- [ ] **Step 2: Add `NavigateToCustomers` command to `MainViewModel`**

Open `SmartSolutions.App/ViewModels/MainViewModel.cs`. After the `NavigateToItems` method block, add:

```csharp
    [RelayCommand]
    private void NavigateToCustomers()
    {
        CurrentSection = "Customers";
        CurrentView = _services.GetRequiredService<CustomersViewModel>();
        _ = ((CustomersViewModel)CurrentView!).LoadAsync();
    }
```

- [ ] **Step 3: Add DataTemplate to `MainWindow.xaml`**

Open `SmartSolutions.App/MainWindow.xaml`. After:

```xml
        <DataTemplate DataType="{x:Type vm:ItemsViewModel}">
            <views:ItemsView />
        </DataTemplate>
```

Add:

```xml
        <DataTemplate DataType="{x:Type vm:CustomersViewModel}">
            <views:CustomersView />
        </DataTemplate>
```

- [ ] **Step 4: Add sidebar button to `MainWindow.xaml`**

In the sidebar `StackPanel`, find the management section separator followed by Items:

```xml
                <Separator Background="White" Opacity="0.3" Margin="16,4"/>
                <Button Content="Items"        Command="{Binding NavigateToItemsCommand}"
```

Insert a new Customers button **between the separator and Items**:

```xml
                <Separator Background="White" Opacity="0.3" Margin="16,4"/>
                <Button Content="Customers"    Command="{Binding NavigateToCustomersCommand}"
                        Style="{StaticResource MaterialDesignFlatButton}"
                        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2"/>
                <Button Content="Items"        Command="{Binding NavigateToItemsCommand}"
```

- [ ] **Step 5: Build entire solution**

```
cd SmartSolutions
dotnet build 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Error(s)  0 Warning(s)`

- [ ] **Step 6: Run full test suite**

```
dotnet test SmartSolutions.Tests -v normal
```

Expected: all tests pass (including the 6 new CustomerServiceTests).

- [ ] **Step 7: Commit**

```
git add SmartSolutions.App/ServiceConfiguration.cs SmartSolutions.App/ViewModels/MainViewModel.cs SmartSolutions.App/MainWindow.xaml
git commit -m "feat: wire up Customers page navigation and DI"
```
