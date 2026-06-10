# Dedicated Management Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move Items, Vendors, Technicians, Expense Categories, Payment Channels, and Users out of the Settings page into dedicated full-page views — each with a sidebar nav entry and a MaterialDesign `DialogHost` popup for Add/Edit.

**Architecture:** Each management entity gets its own ViewModel and View following the existing patterns. Settings is stripped to Business Info only. A single `md:DialogHost` wrapping each page's content provides the popup — `IsDialogOpen` bound to ViewModel so no code-behind for open/close (except PasswordBox reading in Users page).

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, MaterialDesignInXamlToolkit, EF Core 10, xUnit + FluentAssertions + NSubstitute

---

## File Map

| Action | Path |
|--------|------|
| Modify | `SmartSolutions.Core/Interfaces/ILookupService.cs` |
| Modify | `SmartSolutions.Core/Services/LookupService.cs` |
| Modify | `SmartSolutions.Tests/Services/LookupServiceTests.cs` |
| Create | `SmartSolutions.App/ViewModels/ItemsViewModel.cs` |
| Create | `SmartSolutions.App/Views/ItemsView.xaml` |
| Create | `SmartSolutions.App/Views/ItemsView.xaml.cs` |
| Create | `SmartSolutions.App/ViewModels/VendorsViewModel.cs` |
| Create | `SmartSolutions.App/Views/VendorsView.xaml` |
| Create | `SmartSolutions.App/Views/VendorsView.xaml.cs` |
| Create | `SmartSolutions.App/ViewModels/TechniciansViewModel.cs` |
| Create | `SmartSolutions.App/Views/TechniciansView.xaml` |
| Create | `SmartSolutions.App/Views/TechniciansView.xaml.cs` |
| Create | `SmartSolutions.App/ViewModels/ExpenseCategoriesViewModel.cs` |
| Create | `SmartSolutions.App/Views/ExpenseCategoriesView.xaml` |
| Create | `SmartSolutions.App/Views/ExpenseCategoriesView.xaml.cs` |
| Create | `SmartSolutions.App/ViewModels/PaymentChannelsViewModel.cs` |
| Create | `SmartSolutions.App/Views/PaymentChannelsView.xaml` |
| Create | `SmartSolutions.App/Views/PaymentChannelsView.xaml.cs` |
| Create | `SmartSolutions.App/ViewModels/UsersViewModel.cs` |
| Create | `SmartSolutions.App/Views/UsersView.xaml` |
| Create | `SmartSolutions.App/Views/UsersView.xaml.cs` |
| Modify | `SmartSolutions.App/ViewModels/MainViewModel.cs` |
| Modify | `SmartSolutions.App/MainWindow.xaml` |
| Modify | `SmartSolutions.App/ServiceConfiguration.cs` |
| Modify | `SmartSolutions.App/ViewModels/SettingsViewModel.cs` |
| Modify | `SmartSolutions.App/Views/SettingsView.xaml` |

---

## Task 1: Add Rename Methods to ILookupService + LookupService

Currently, item names, expense categories, and payment channels only support Add/Delete. Add rename methods for a proper Edit capability in the new dedicated pages.

**Files:**
- Modify: `SmartSolutions.Core/Interfaces/ILookupService.cs`
- Modify: `SmartSolutions.Core/Services/LookupService.cs`
- Modify: `SmartSolutions.Tests/Services/LookupServiceTests.cs`

- [ ] **Step 1: Write three failing tests**

Add to `SmartSolutions.Tests/Services/LookupServiceTests.cs`:

```csharp
[Fact]
public async Task RenameItemNameAsync_UpdatesName()
{
    var factory = TestDbContextFactory.Unique();
    var svc = new LookupService(factory);
    var cat = await svc.AddItemCategoryAsync("Cat");
    var item = await svc.AddItemNameAsync("OldName", cat.Id);

    await svc.RenameItemNameAsync(item.Id, "NewName");

    var names = await svc.GetItemNamesAsync(cat.Id);
    names.Should().ContainSingle(n => n.Name == "NewName");
    names.Should().NotContain(n => n.Name == "OldName");
}

[Fact]
public async Task RenameExpenseCategoryAsync_UpdatesName()
{
    var factory = TestDbContextFactory.Unique();
    var svc = new LookupService(factory);
    var cat = await svc.AddExpenseCategoryAsync("Old");

    await svc.RenameExpenseCategoryAsync(cat.Id, "New");

    var all = await svc.GetExpenseCategoriesAsync();
    all.Should().ContainSingle(c => c.Name == "New");
}

[Fact]
public async Task RenamePaymentChannelAsync_UpdatesName()
{
    var factory = TestDbContextFactory.Unique();
    var svc = new LookupService(factory);
    var ch = await svc.AddPaymentChannelAsync("OldChannel");

    await svc.RenamePaymentChannelAsync(ch.Id, "NewChannel");

    var all = await svc.GetPaymentChannelsAsync();
    all.Should().Contain(c => c.Name == "NewChannel");
    all.Should().NotContain(c => c.Name == "OldChannel");
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```
dotnet test SmartSolutions.Tests --filter "RenameItemNameAsync_UpdatesName|RenameExpenseCategoryAsync_UpdatesName|RenamePaymentChannelAsync_UpdatesName" -v minimal
```

Expected: 3 × FAIL — `ILookupService` does not have these methods yet.

- [ ] **Step 3: Add method signatures to ILookupService**

In `SmartSolutions.Core/Interfaces/ILookupService.cs`, add after the existing item name methods:

```csharp
Task<ItemName>        RenameItemNameAsync(int id, string name);
Task<ExpenseCategory> RenameExpenseCategoryAsync(int id, string name);
Task<PaymentChannel>  RenamePaymentChannelAsync(int id, string name);
```

- [ ] **Step 4: Implement in LookupService**

In `SmartSolutions.Core/Services/LookupService.cs`, add:

```csharp
public async Task<ItemName> RenameItemNameAsync(int id, string name)
{
    await using var db = _factory.CreateDbContext();
    var item = await db.ItemNames.FindAsync(id)
        ?? throw new InvalidOperationException("Item name not found.");
    item.Name = name;
    await db.SaveChangesAsync();
    return item;
}

public async Task<ExpenseCategory> RenameExpenseCategoryAsync(int id, string name)
{
    await using var db = _factory.CreateDbContext();
    var cat = await db.ExpenseCategories.FindAsync(id)
        ?? throw new InvalidOperationException("Expense category not found.");
    cat.Name = name;
    await db.SaveChangesAsync();
    return cat;
}

public async Task<PaymentChannel> RenamePaymentChannelAsync(int id, string name)
{
    await using var db = _factory.CreateDbContext();
    var ch = await db.PaymentChannels.FindAsync(id)
        ?? throw new InvalidOperationException("Payment channel not found.");
    ch.Name = name;
    await db.SaveChangesAsync();
    return ch;
}
```

- [ ] **Step 5: Run tests to confirm they pass**

```
dotnet test SmartSolutions.Tests --filter "RenameItemNameAsync_UpdatesName|RenameExpenseCategoryAsync_UpdatesName|RenamePaymentChannelAsync_UpdatesName" -v minimal
```

Expected: 3 × PASS.

- [ ] **Step 6: Run full test suite to confirm nothing broke**

```
dotnet test SmartSolutions.Tests -v minimal
```

Expected: All passing.

- [ ] **Step 7: Commit**

```
git add SmartSolutions/SmartSolutions.Core/Interfaces/ILookupService.cs
git add SmartSolutions/SmartSolutions.Core/Services/LookupService.cs
git add SmartSolutions/SmartSolutions.Tests/Services/LookupServiceTests.cs
git commit -m "feat: add RenameItemName, RenameExpenseCategory, RenamePaymentChannel to ILookupService"
```

---

## Task 2: Items Management Page

Split-panel page managing Item Categories (left) and Item Names (right). One shared `DialogHost` — the `DialogTitle` changes based on which Add/Edit was triggered.

**Files:**
- Create: `SmartSolutions.App/ViewModels/ItemsViewModel.cs`
- Create: `SmartSolutions.App/Views/ItemsView.xaml`
- Create: `SmartSolutions.App/Views/ItemsView.xaml.cs`
- Modify: `SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions.App/MainWindow.xaml`
- Modify: `SmartSolutions.App/ServiceConfiguration.cs`

- [ ] **Step 1: Create ItemsViewModel.cs**

```csharp
// SmartSolutions.App/ViewModels/ItemsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class ItemsViewModel(ILookupService lookup) : ObservableObject
{
    // ── Categories ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ItemCategory> _categories = [];
    [ObservableProperty] private ItemCategory? _selectedCategory;

    // ── Item Names ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ItemName> _itemNames = [];

    // ── Shared dialog ────────────────────────────────────────────────────
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private string _dialogTitle = "";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogErrorMessage = "";

    private bool   _dialogEditingCategory;    // true = category op, false = item name op
    private bool   _dialogIsEdit;             // true = rename, false = add
    private int    _dialogEditingId;

    [ObservableProperty] private bool _isBusy;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Categories = new(await lookup.GetItemCategoriesAsync()); }
        finally { IsBusy = false; }
    }

    partial void OnSelectedCategoryChanged(ItemCategory? value)
    {
        ItemNames.Clear();
        if (value is not null) _ = LoadItemNamesAsync(value.Id);
    }

    private async Task LoadItemNamesAsync(int categoryId)
    {
        ItemNames = new(await lookup.GetItemNamesAsync(categoryId));
    }

    // ── Category dialog ──────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddCategoryDialog()
    {
        _dialogEditingCategory = true;
        _dialogIsEdit          = false;
        _dialogEditingId       = 0;
        DialogTitle            = "Add Category";
        DialogName             = "";
        DialogErrorMessage     = "";
        IsDialogOpen           = true;
    }

    [RelayCommand]
    private void OpenEditCategoryDialog(ItemCategory cat)
    {
        _dialogEditingCategory = true;
        _dialogIsEdit          = true;
        _dialogEditingId       = cat.Id;
        DialogTitle            = "Rename Category";
        DialogName             = cat.Name;
        DialogErrorMessage     = "";
        IsDialogOpen           = true;
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(ItemCategory cat)
    {
        if (!DialogHelper.Confirm(
            $"Delete category '{cat.Name}'? All item names in this category will also be deleted.")) return;
        try
        {
            await lookup.DeleteItemCategoryAsync(cat.Id);
            Categories.Remove(cat);
            if (SelectedCategory?.Id == cat.Id) { SelectedCategory = null; ItemNames.Clear(); }
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    // ── Item Name dialog ─────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAddItemNameDialog()
    {
        if (SelectedCategory is null) return;
        _dialogEditingCategory = false;
        _dialogIsEdit          = false;
        _dialogEditingId       = 0;
        DialogTitle            = $"Add Item to '{SelectedCategory.Name}'";
        DialogName             = "";
        DialogErrorMessage     = "";
        IsDialogOpen           = true;
    }

    [RelayCommand]
    private void OpenEditItemNameDialog(ItemName item)
    {
        _dialogEditingCategory = false;
        _dialogIsEdit          = true;
        _dialogEditingId       = item.Id;
        DialogTitle            = "Rename Item";
        DialogName             = item.Name;
        DialogErrorMessage     = "";
        IsDialogOpen           = true;
    }

    [RelayCommand]
    private async Task DeleteItemNameAsync(ItemName item)
    {
        if (!DialogHelper.Confirm($"Delete item '{item.Name}'?")) return;
        try
        {
            await lookup.DeleteItemNameAsync(item.Id);
            ItemNames.Remove(item);
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    // ── Shared dialog Save/Cancel ─────────────────────────────────────────

    [RelayCommand]
    private async Task SaveDialogAsync()
    {
        if (string.IsNullOrWhiteSpace(DialogName))
        { DialogErrorMessage = "Name is required."; return; }

        try
        {
            if (_dialogEditingCategory)
            {
                if (!_dialogIsEdit)
                {
                    var added = await lookup.AddItemCategoryAsync(DialogName.Trim());
                    Categories.Add(added);
                }
                else
                {
                    await lookup.RenameItemCategoryAsync(_dialogEditingId, DialogName.Trim());
                    var cat = Categories.First(c => c.Id == _dialogEditingId);
                    var idx = Categories.IndexOf(cat);
                    cat.Name = DialogName.Trim();
                    Categories.RemoveAt(idx);
                    Categories.Insert(idx, cat);
                    if (SelectedCategory?.Id == _dialogEditingId)
                        SelectedCategory = cat;
                }
            }
            else
            {
                if (SelectedCategory is null) { DialogErrorMessage = "Select a category first."; return; }

                if (!_dialogIsEdit)
                {
                    var added = await lookup.AddItemNameAsync(DialogName.Trim(), SelectedCategory.Id);
                    ItemNames.Add(added);
                }
                else
                {
                    await lookup.RenameItemNameAsync(_dialogEditingId, DialogName.Trim());
                    var item = ItemNames.First(i => i.Id == _dialogEditingId);
                    var idx = ItemNames.IndexOf(item);
                    item.Name = DialogName.Trim();
                    ItemNames.RemoveAt(idx);
                    ItemNames.Insert(idx, item);
                }
            }

            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;
}
```

- [ ] **Step 2: Create ItemsView.xaml**

```xml
<!--SmartSolutions.App/Views/ItemsView.xaml-->
<UserControl x:Class="SmartSolutions.App.Views.ItemsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <md:DialogHost IsOpen="{Binding IsDialogOpen}" CloseOnClickAway="False">
        <md:DialogHost.DialogContent>
            <StackPanel Margin="24" MinWidth="300">
                <TextBlock Text="{Binding DialogTitle}"
                           Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                           Margin="0,0,0,16"/>
                <TextBox md:HintAssist.Hint="Name *"
                         Text="{Binding DialogName, UpdateSourceTrigger=PropertyChanged}"
                         Margin="0,0,0,8"/>
                <TextBlock Text="{Binding DialogErrorMessage}"
                           Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                           MinHeight="20" TextWrapping="Wrap"/>
                <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                    <Button Content="Save"
                            Command="{Binding SaveDialogCommand}"
                            Style="{StaticResource MaterialDesignRaisedButton}"
                            Margin="0,0,8,0"/>
                    <Button Content="Cancel"
                            Command="{Binding CancelDialogCommand}"
                            Style="{StaticResource MaterialDesignOutlinedButton}"/>
                </StackPanel>
            </StackPanel>
        </md:DialogHost.DialogContent>

        <Grid Margin="24">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <TextBlock Grid.Row="0" Text="Items"
                       Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                       Margin="0,0,0,16"/>

            <Grid Grid.Row="1">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="16"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <!-- Categories -->
                <md:Card Grid.Column="0" Padding="16">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>
                        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,12">
                            <TextBlock Text="Item Categories"
                                       Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                                       VerticalAlignment="Center" Margin="0,0,16,0"/>
                            <Button Content="+ Add Category"
                                    Command="{Binding OpenAddCategoryDialogCommand}"
                                    Style="{StaticResource MaterialDesignOutlinedButton}"/>
                        </StackPanel>
                        <DataGrid Grid.Row="1"
                                  ItemsSource="{Binding Categories}"
                                  SelectedItem="{Binding SelectedCategory}"
                                  AutoGenerateColumns="False"
                                  CanUserAddRows="False"
                                  IsReadOnly="True"
                                  SelectionMode="Single">
                            <DataGrid.Columns>
                                <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
                                <DataGridTemplateColumn Width="130">
                                    <DataGridTemplateColumn.CellTemplate>
                                        <DataTemplate>
                                            <StackPanel Orientation="Horizontal">
                                                <Button Content="Edit"
                                                        Command="{Binding DataContext.OpenEditCategoryDialogCommand,
                                                                  RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                        CommandParameter="{Binding}"
                                                        Style="{StaticResource MaterialDesignFlatButton}"
                                                        Padding="4,0"/>
                                                <Button Content="Delete"
                                                        Command="{Binding DataContext.DeleteCategoryCommand,
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
                </md:Card>

                <!-- Item Names -->
                <md:Card Grid.Column="2" Padding="16">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>
                        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,12">
                            <TextBlock Text="Item Names"
                                       Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                                       VerticalAlignment="Center" Margin="0,0,16,0"/>
                            <Button Content="+ Add Item"
                                    Command="{Binding OpenAddItemNameDialogCommand}"
                                    Style="{StaticResource MaterialDesignOutlinedButton}"
                                    IsEnabled="{Binding SelectedCategory, Converter={StaticResource NullToBoolConverter}}"/>
                        </StackPanel>
                        <DataGrid Grid.Row="1"
                                  ItemsSource="{Binding ItemNames}"
                                  AutoGenerateColumns="False"
                                  CanUserAddRows="False"
                                  IsReadOnly="True"
                                  SelectionMode="Single">
                            <DataGrid.Columns>
                                <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
                                <DataGridTemplateColumn Width="130">
                                    <DataGridTemplateColumn.CellTemplate>
                                        <DataTemplate>
                                            <StackPanel Orientation="Horizontal">
                                                <Button Content="Edit"
                                                        Command="{Binding DataContext.OpenEditItemNameDialogCommand,
                                                                  RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                        CommandParameter="{Binding}"
                                                        Style="{StaticResource MaterialDesignFlatButton}"
                                                        Padding="4,0"/>
                                                <Button Content="Delete"
                                                        Command="{Binding DataContext.DeleteItemNameCommand,
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
                </md:Card>
            </Grid>
        </Grid>
    </md:DialogHost>
</UserControl>
```

**Note on `NullToBoolConverter`:** Check whether this converter already exists in the Converters folder. If not, add it:

```csharp
// SmartSolutions.App/Converters/NullToBoolConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace SmartSolutions.App.Converters;

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

Register it in `App.xaml` resources (or `MainWindow.xaml` resources) if added:
```xml
<converters:NullToBoolConverter x:Key="NullToBoolConverter"/>
```

- [ ] **Step 3: Create ItemsView.xaml.cs**

```csharp
// SmartSolutions.App/Views/ItemsView.xaml.cs
namespace SmartSolutions.App.Views;

public partial class ItemsView : UserControl
{
    public ItemsView() => InitializeComponent();
}
```

- [ ] **Step 4: Add to MainViewModel**

In `SmartSolutions.App/ViewModels/MainViewModel.cs`, add after `NavigateToReports`:

```csharp
[RelayCommand]
private void NavigateToItems()
{
    CurrentSection = "Items";
    CurrentView = _services.GetRequiredService<ItemsViewModel>();
    _ = ((ItemsViewModel)CurrentView!).LoadAsync();
}
```

- [ ] **Step 5: Add DataTemplate and nav button to MainWindow.xaml**

In `MainWindow.xaml`, add to `<Window.Resources>`:
```xml
<DataTemplate DataType="{x:Type vm:ItemsViewModel}">
    <views:ItemsView />
</DataTemplate>
```

Add to the sidebar `<StackPanel>`, after the first `<Separator>` (before the Settings button):
```xml
<Separator Background="White" Opacity="0.3" Margin="16,4"/>
<Button Content="Items" Command="{Binding NavigateToItemsCommand}"
        Style="{StaticResource MaterialDesignFlatButton}"
        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2"/>
```

- [ ] **Step 6: Register ViewModel in ServiceConfiguration.cs**

Add to the ViewModel registrations section:
```csharp
services.AddTransient<ItemsViewModel>();
```

- [ ] **Step 7: Build and smoke test**

```
dotnet build SmartSolutions/SmartSolutions.sln
```

Expected: 0 errors. Run the app, click "Items" in the sidebar, verify the split panel loads.

- [ ] **Step 8: Run tests**

```
dotnet test SmartSolutions.Tests -v minimal
```

Expected: All passing.

- [ ] **Step 9: Commit**

```
git add SmartSolutions/SmartSolutions.App/ViewModels/ItemsViewModel.cs
git add SmartSolutions/SmartSolutions.App/Views/ItemsView.xaml
git add SmartSolutions/SmartSolutions.App/Views/ItemsView.xaml.cs
git add SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs
git add SmartSolutions/SmartSolutions.App/MainWindow.xaml
git add SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs
git commit -m "feat: add Items management page with category/item-name popup dialogs"
```

---

## Task 3: Vendors Management Page

**Files:**
- Create: `SmartSolutions.App/ViewModels/VendorsViewModel.cs`
- Create: `SmartSolutions.App/Views/VendorsView.xaml`
- Create: `SmartSolutions.App/Views/VendorsView.xaml.cs`
- Modify: `SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions.App/MainWindow.xaml`
- Modify: `SmartSolutions.App/ServiceConfiguration.cs`

- [ ] **Step 1: Create VendorsViewModel.cs**

```csharp
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
    [ObservableProperty] private string _dialogTitle = "Add Vendor";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogPhone = "";
    [ObservableProperty] private string _dialogNotes = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Vendors = new(await lookup.GetVendorsAsync()); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode        = false;
        _editingId         = 0;
        DialogTitle        = "Add Vendor";
        DialogName         = "";
        DialogPhone        = "";
        DialogNotes        = "";
        DialogErrorMessage = "";
        IsDialogOpen       = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Vendor vendor)
    {
        _isEditMode        = true;
        _editingId         = vendor.Id;
        DialogTitle        = "Edit Vendor";
        DialogName         = vendor.Name;
        DialogPhone        = vendor.Phone ?? "";
        DialogNotes        = vendor.Notes ?? "";
        DialogErrorMessage = "";
        IsDialogOpen       = true;
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
                var added = await lookup.AddVendorAsync(DialogName.Trim(),
                    NullIfEmpty(DialogPhone), NullIfEmpty(DialogNotes));
                Vendors.Add(added);
            }
            else
            {
                await lookup.UpdateVendorAsync(_editingId, DialogName.Trim(),
                    NullIfEmpty(DialogPhone), NullIfEmpty(DialogNotes));
                var v = Vendors.First(x => x.Id == _editingId);
                var idx = Vendors.IndexOf(v);
                v.Name = DialogName.Trim();
                v.Phone = NullIfEmpty(DialogPhone);
                v.Notes = NullIfEmpty(DialogNotes);
                Vendors.RemoveAt(idx);
                Vendors.Insert(idx, v);
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
        try
        {
            await lookup.DeleteVendorAsync(vendor.Id);
            Vendors.Remove(vendor);
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
```

- [ ] **Step 2: Create VendorsView.xaml**

```xml
<!--SmartSolutions.App/Views/VendorsView.xaml-->
<UserControl x:Class="SmartSolutions.App.Views.VendorsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <md:DialogHost IsOpen="{Binding IsDialogOpen}" CloseOnClickAway="False">
        <md:DialogHost.DialogContent>
            <StackPanel Margin="24" MinWidth="320">
                <TextBlock Text="{Binding DialogTitle}"
                           Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                           Margin="0,0,0,16"/>
                <TextBox md:HintAssist.Hint="Name *"
                         Text="{Binding DialogName, UpdateSourceTrigger=PropertyChanged}"
                         Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Phone"
                         Text="{Binding DialogPhone, UpdateSourceTrigger=PropertyChanged}"
                         Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Notes"
                         Text="{Binding DialogNotes, UpdateSourceTrigger=PropertyChanged}"
                         Margin="0,0,0,8"/>
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
            <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
                <TextBlock Text="Vendors"
                           Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           VerticalAlignment="Center" Margin="0,0,16,0"/>
                <Button Content="+ Add Vendor" Command="{Binding OpenAddDialogCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <DataGrid Grid.Row="1" ItemsSource="{Binding Vendors}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Name"  Binding="{Binding Name}"  Width="*"/>
                    <DataGridTextColumn Header="Phone" Binding="{Binding Phone}" Width="150"/>
                    <DataGridTextColumn Header="Notes" Binding="{Binding Notes}" Width="200"/>
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

- [ ] **Step 3: Create VendorsView.xaml.cs**

```csharp
// SmartSolutions.App/Views/VendorsView.xaml.cs
namespace SmartSolutions.App.Views;

public partial class VendorsView : UserControl
{
    public VendorsView() => InitializeComponent();
}
```

- [ ] **Step 4: Wire navigation**

In `MainViewModel.cs`, add after `NavigateToItems`:
```csharp
[RelayCommand]
private void NavigateToVendors()
{
    CurrentSection = "Vendors";
    CurrentView = _services.GetRequiredService<VendorsViewModel>();
    _ = ((VendorsViewModel)CurrentView!).LoadAsync();
}
```

In `MainWindow.xaml` `<Window.Resources>`:
```xml
<DataTemplate DataType="{x:Type vm:VendorsViewModel}">
    <views:VendorsView />
</DataTemplate>
```

In `MainWindow.xaml` sidebar (after Items button):
```xml
<Button Content="Vendors" Command="{Binding NavigateToVendorsCommand}"
        Style="{StaticResource MaterialDesignFlatButton}"
        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2"/>
```

In `ServiceConfiguration.cs`:
```csharp
services.AddTransient<VendorsViewModel>();
```

- [ ] **Step 5: Build, smoke test, run tests**

```
dotnet build SmartSolutions/SmartSolutions.sln
dotnet test SmartSolutions.Tests -v minimal
```

Expected: 0 errors, all tests passing. Manually click "Vendors" in the app sidebar and verify the DataGrid and popup work.

- [ ] **Step 6: Commit**

```
git add SmartSolutions/SmartSolutions.App/ViewModels/VendorsViewModel.cs
git add SmartSolutions/SmartSolutions.App/Views/VendorsView.xaml
git add SmartSolutions/SmartSolutions.App/Views/VendorsView.xaml.cs
git add SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs
git add SmartSolutions/SmartSolutions.App/MainWindow.xaml
git add SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs
git commit -m "feat: add Vendors management page with popup dialog"
```

---

## Task 4: Technicians Management Page

**Files:**
- Create: `SmartSolutions.App/ViewModels/TechniciansViewModel.cs`
- Create: `SmartSolutions.App/Views/TechniciansView.xaml`
- Create: `SmartSolutions.App/Views/TechniciansView.xaml.cs`
- Modify: `SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions.App/MainWindow.xaml`
- Modify: `SmartSolutions.App/ServiceConfiguration.cs`

- [ ] **Step 1: Create TechniciansViewModel.cs**

```csharp
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
    [ObservableProperty] private string _dialogTitle = "Add Technician";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogPhone = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Technicians = new(await lookup.GetTechniciansAsync()); }
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
        DialogTitle = "Edit Technician"; DialogName = tech.Name;
        DialogPhone = tech.Phone ?? ""; DialogErrorMessage = ""; IsDialogOpen = true;
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
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
```

- [ ] **Step 2: Create TechniciansView.xaml**

```xml
<!--SmartSolutions.App/Views/TechniciansView.xaml-->
<UserControl x:Class="SmartSolutions.App.Views.TechniciansView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <md:DialogHost IsOpen="{Binding IsDialogOpen}" CloseOnClickAway="False">
        <md:DialogHost.DialogContent>
            <StackPanel Margin="24" MinWidth="300">
                <TextBlock Text="{Binding DialogTitle}"
                           Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,16"/>
                <TextBox md:HintAssist.Hint="Name *"
                         Text="{Binding DialogName, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Phone"
                         Text="{Binding DialogPhone, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
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
            <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
                <TextBlock Text="Technicians"
                           Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           VerticalAlignment="Center" Margin="0,0,16,0"/>
                <Button Content="+ Add Technician" Command="{Binding OpenAddDialogCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <DataGrid Grid.Row="1" ItemsSource="{Binding Technicians}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Name"  Binding="{Binding Name}"  Width="*"/>
                    <DataGridTextColumn Header="Phone" Binding="{Binding Phone}" Width="150"/>
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

- [ ] **Step 3: Create TechniciansView.xaml.cs**

```csharp
// SmartSolutions.App/Views/TechniciansView.xaml.cs
namespace SmartSolutions.App.Views;

public partial class TechniciansView : UserControl
{
    public TechniciansView() => InitializeComponent();
}
```

- [ ] **Step 4: Wire navigation**

In `MainViewModel.cs`:
```csharp
[RelayCommand]
private void NavigateToTechnicians()
{
    CurrentSection = "Technicians";
    CurrentView = _services.GetRequiredService<TechniciansViewModel>();
    _ = ((TechniciansViewModel)CurrentView!).LoadAsync();
}
```

In `MainWindow.xaml` resources:
```xml
<DataTemplate DataType="{x:Type vm:TechniciansViewModel}">
    <views:TechniciansView />
</DataTemplate>
```

In `MainWindow.xaml` sidebar (after Vendors button):
```xml
<Button Content="Technicians" Command="{Binding NavigateToTechniciansCommand}"
        Style="{StaticResource MaterialDesignFlatButton}"
        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2"/>
```

In `ServiceConfiguration.cs`:
```csharp
services.AddTransient<TechniciansViewModel>();
```

- [ ] **Step 5: Build, smoke test, run tests, commit**

```
dotnet build SmartSolutions/SmartSolutions.sln
dotnet test SmartSolutions.Tests -v minimal
git add SmartSolutions/SmartSolutions.App/ViewModels/TechniciansViewModel.cs
git add SmartSolutions/SmartSolutions.App/Views/TechniciansView.xaml
git add SmartSolutions/SmartSolutions.App/Views/TechniciansView.xaml.cs
git add SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs
git add SmartSolutions/SmartSolutions.App/MainWindow.xaml
git add SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs
git commit -m "feat: add Technicians management page with popup dialog"
```

---

## Task 5: Expense Categories Management Page

**Files:**
- Create: `SmartSolutions.App/ViewModels/ExpenseCategoriesViewModel.cs`
- Create: `SmartSolutions.App/Views/ExpenseCategoriesView.xaml`
- Create: `SmartSolutions.App/Views/ExpenseCategoriesView.xaml.cs`
- Modify: `SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions.App/MainWindow.xaml`
- Modify: `SmartSolutions.App/ServiceConfiguration.cs`

- [ ] **Step 1: Create ExpenseCategoriesViewModel.cs**

```csharp
// SmartSolutions.App/ViewModels/ExpenseCategoriesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class ExpenseCategoriesViewModel(ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ExpenseCategory> _categories = [];
    [ObservableProperty] private bool   _isDialogOpen;
    [ObservableProperty] private string _dialogTitle = "Add Category";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Categories = new(await lookup.GetExpenseCategoriesAsync()); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode = false; _editingId = 0;
        DialogTitle = "Add Category"; DialogName = "";
        DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(ExpenseCategory cat)
    {
        _isEditMode = true; _editingId = cat.Id;
        DialogTitle = "Rename Category"; DialogName = cat.Name;
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
                var added = await lookup.AddExpenseCategoryAsync(DialogName.Trim());
                Categories.Add(added);
            }
            else
            {
                await lookup.RenameExpenseCategoryAsync(_editingId, DialogName.Trim());
                var cat = Categories.First(c => c.Id == _editingId);
                var idx = Categories.IndexOf(cat);
                cat.Name = DialogName.Trim();
                Categories.RemoveAt(idx); Categories.Insert(idx, cat);
            }
            IsDialogOpen = false;
        }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task DeleteAsync(ExpenseCategory cat)
    {
        if (!DialogHelper.Confirm($"Delete expense category '{cat.Name}'?")) return;
        try { await lookup.DeleteExpenseCategoryAsync(cat.Id); Categories.Remove(cat); }
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }
}
```

- [ ] **Step 2: Create ExpenseCategoriesView.xaml**

```xml
<!--SmartSolutions.App/Views/ExpenseCategoriesView.xaml-->
<UserControl x:Class="SmartSolutions.App.Views.ExpenseCategoriesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <md:DialogHost IsOpen="{Binding IsDialogOpen}" CloseOnClickAway="False">
        <md:DialogHost.DialogContent>
            <StackPanel Margin="24" MinWidth="280">
                <TextBlock Text="{Binding DialogTitle}"
                           Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,16"/>
                <TextBox md:HintAssist.Hint="Category Name *"
                         Text="{Binding DialogName, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
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
            <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
                <TextBlock Text="Expense Categories"
                           Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           VerticalAlignment="Center" Margin="0,0,16,0"/>
                <Button Content="+ Add Category" Command="{Binding OpenAddDialogCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <DataGrid Grid.Row="1" ItemsSource="{Binding Categories}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
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

- [ ] **Step 3: Create ExpenseCategoriesView.xaml.cs**

```csharp
// SmartSolutions.App/Views/ExpenseCategoriesView.xaml.cs
namespace SmartSolutions.App.Views;

public partial class ExpenseCategoriesView : UserControl
{
    public ExpenseCategoriesView() => InitializeComponent();
}
```

- [ ] **Step 4: Wire navigation**

In `MainViewModel.cs`:
```csharp
[RelayCommand]
private void NavigateToExpenseCategories()
{
    CurrentSection = "Expense Categories";
    CurrentView = _services.GetRequiredService<ExpenseCategoriesViewModel>();
    _ = ((ExpenseCategoriesViewModel)CurrentView!).LoadAsync();
}
```

In `MainWindow.xaml` resources:
```xml
<DataTemplate DataType="{x:Type vm:ExpenseCategoriesViewModel}">
    <views:ExpenseCategoriesView />
</DataTemplate>
```

In `MainWindow.xaml` sidebar (after Technicians button):
```xml
<Button Content="Expense Categories" Command="{Binding NavigateToExpenseCategoriesCommand}"
        Style="{StaticResource MaterialDesignFlatButton}"
        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2"/>
```

In `ServiceConfiguration.cs`:
```csharp
services.AddTransient<ExpenseCategoriesViewModel>();
```

- [ ] **Step 5: Build, smoke test, run tests, commit**

```
dotnet build SmartSolutions/SmartSolutions.sln
dotnet test SmartSolutions.Tests -v minimal
git add SmartSolutions/SmartSolutions.App/ViewModels/ExpenseCategoriesViewModel.cs
git add SmartSolutions/SmartSolutions.App/Views/ExpenseCategoriesView.xaml
git add SmartSolutions/SmartSolutions.App/Views/ExpenseCategoriesView.xaml.cs
git add SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs
git add SmartSolutions/SmartSolutions.App/MainWindow.xaml
git add SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs
git commit -m "feat: add Expense Categories management page with popup dialog"
```

---

## Task 6: Payment Channels Management Page

**Files:**
- Create: `SmartSolutions.App/ViewModels/PaymentChannelsViewModel.cs`
- Create: `SmartSolutions.App/Views/PaymentChannelsView.xaml`
- Create: `SmartSolutions.App/Views/PaymentChannelsView.xaml.cs`
- Modify: `SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions.App/MainWindow.xaml`
- Modify: `SmartSolutions.App/ServiceConfiguration.cs`

- [ ] **Step 1: Create PaymentChannelsViewModel.cs**

```csharp
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
    [ObservableProperty] private string _dialogTitle = "Add Channel";
    [ObservableProperty] private string _dialogName = "";
    [ObservableProperty] private string _dialogErrorMessage = "";
    [ObservableProperty] private bool   _isBusy;

    private bool _isEditMode;
    private int  _editingId;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Channels = new(await lookup.GetPaymentChannelsAsync()); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _isEditMode = false; _editingId = 0;
        DialogTitle = "Add Channel"; DialogName = "";
        DialogErrorMessage = ""; IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(PaymentChannel channel)
    {
        _isEditMode = true; _editingId = channel.Id;
        DialogTitle = "Rename Channel"; DialogName = channel.Name;
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
        catch (Exception ex) { DialogErrorMessage = ex.Message; }
    }
}
```

- [ ] **Step 2: Create PaymentChannelsView.xaml**

```xml
<!--SmartSolutions.App/Views/PaymentChannelsView.xaml-->
<UserControl x:Class="SmartSolutions.App.Views.PaymentChannelsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <md:DialogHost IsOpen="{Binding IsDialogOpen}" CloseOnClickAway="False">
        <md:DialogHost.DialogContent>
            <StackPanel Margin="24" MinWidth="280">
                <TextBlock Text="{Binding DialogTitle}"
                           Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,16"/>
                <TextBox md:HintAssist.Hint="Channel Name *"
                         Text="{Binding DialogName, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
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
            <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
                <TextBlock Text="Payment Channels"
                           Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           VerticalAlignment="Center" Margin="0,0,16,0"/>
                <Button Content="+ Add Channel" Command="{Binding OpenAddDialogCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <DataGrid Grid.Row="1" ItemsSource="{Binding Channels}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
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

- [ ] **Step 3: Create PaymentChannelsView.xaml.cs**

```csharp
// SmartSolutions.App/Views/PaymentChannelsView.xaml.cs
namespace SmartSolutions.App.Views;

public partial class PaymentChannelsView : UserControl
{
    public PaymentChannelsView() => InitializeComponent();
}
```

- [ ] **Step 4: Wire navigation**

In `MainViewModel.cs`:
```csharp
[RelayCommand]
private void NavigateToPaymentChannels()
{
    CurrentSection = "Payment Channels";
    CurrentView = _services.GetRequiredService<PaymentChannelsViewModel>();
    _ = ((PaymentChannelsViewModel)CurrentView!).LoadAsync();
}
```

In `MainWindow.xaml` resources:
```xml
<DataTemplate DataType="{x:Type vm:PaymentChannelsViewModel}">
    <views:PaymentChannelsView />
</DataTemplate>
```

In `MainWindow.xaml` sidebar (after Expense Categories button):
```xml
<Button Content="Payment Channels" Command="{Binding NavigateToPaymentChannelsCommand}"
        Style="{StaticResource MaterialDesignFlatButton}"
        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2"/>
```

In `ServiceConfiguration.cs`:
```csharp
services.AddTransient<PaymentChannelsViewModel>();
```

- [ ] **Step 5: Build, smoke test, run tests, commit**

```
dotnet build SmartSolutions/SmartSolutions.sln
dotnet test SmartSolutions.Tests -v minimal
git add SmartSolutions/SmartSolutions.App/ViewModels/PaymentChannelsViewModel.cs
git add SmartSolutions/SmartSolutions.App/Views/PaymentChannelsView.xaml
git add SmartSolutions/SmartSolutions.App/Views/PaymentChannelsView.xaml.cs
git add SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs
git add SmartSolutions/SmartSolutions.App/MainWindow.xaml
git add SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs
git commit -m "feat: add Payment Channels management page with popup dialog"
```

---

## Task 7: Users Management Page

Users page is special: PasswordBox cannot be data-bound in WPF, so code-behind reads the PasswordBox value and calls typed ViewModel methods. This is a UI concern and does not violate MVVM.

**Files:**
- Create: `SmartSolutions.App/ViewModels/UsersViewModel.cs`
- Create: `SmartSolutions.App/Views/UsersView.xaml`
- Create: `SmartSolutions.App/Views/UsersView.xaml.cs`
- Modify: `SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions.App/MainWindow.xaml`
- Modify: `SmartSolutions.App/ServiceConfiguration.cs`

- [ ] **Step 1: Create UsersViewModel.cs**

```csharp
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
    [ObservableProperty] private bool   _isAddDialogOpen;
    [ObservableProperty] private bool   _isResetPinDialogOpen;
    [ObservableProperty] private AppUser? _resetPinTargetUser;
    [ObservableProperty] private string _newUsername = "";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool   _isBusy;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try { Users = new(await auth.GetAllAsync()); }
        finally { IsBusy = false; }
    }

    // ── Add User dialog ──────────────────────────────────────────────────

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

    // ── Reset PIN dialog ─────────────────────────────────────────────────

    [RelayCommand]
    private void OpenResetPinDialog(AppUser user)
    {
        ResetPinTargetUser = user; ErrorMessage = ""; IsResetPinDialogOpen = true;
    }

    public async Task ConfirmResetPinAsync(string newPin)
    {
        ErrorMessage = "";
        if (ResetPinTargetUser is null) return;
        if (string.IsNullOrWhiteSpace(newPin))
        { ErrorMessage = "PIN is required."; return; }
        await auth.UpdatePinAsync(ResetPinTargetUser.Id, newPin);
        ErrorMessage = "PIN updated.";
        IsResetPinDialogOpen = false;
    }

    [RelayCommand]
    private void CancelResetPinDialog() { IsResetPinDialogOpen = false; ErrorMessage = ""; }

    // ── Toggle Active ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ToggleActiveAsync(AppUser user)
    {
        ErrorMessage = "";
        if (user.Id == session.CurrentUser.Id)
        { ErrorMessage = "You cannot deactivate your own account."; return; }
        await auth.SetActiveAsync(user.Id, !user.IsActive);
        Users = new(await auth.GetAllAsync());
    }
}
```

- [ ] **Step 2: Create UsersView.xaml**

The view uses two separate `DialogHost`s for the two dialogs. Since they open at different times and are independent, this is handled by wrapping both in an outer `DialogHost` for Add User and an inner card for Reset PIN. The simpler approach: use two overlapping `Popup`-style areas with `IsOpen` binding. But the cleanest is two sequential `DialogHost` wraps.

Actually, use a single `DialogHost` that shows different content based on which operation is active, controlled by `IsAddDialogOpen` and `IsResetPinDialogOpen` both routed through one `IsDialogOpen` property:

Add a computed `IsDialogOpen` to the ViewModel:

Add to `UsersViewModel.cs` (after the existing properties):
```csharp
public bool IsDialogOpen => IsAddDialogOpen || IsResetPinDialogOpen;

partial void OnIsAddDialogOpenChanged(bool value)    => OnPropertyChanged(nameof(IsDialogOpen));
partial void OnIsResetPinDialogOpenChanged(bool value) => OnPropertyChanged(nameof(IsDialogOpen));
```

Then the XAML:

```xml
<!--SmartSolutions.App/Views/UsersView.xaml-->
<UserControl x:Class="SmartSolutions.App.Views.UsersView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <md:DialogHost IsOpen="{Binding IsDialogOpen}" CloseOnClickAway="False">
        <md:DialogHost.DialogContent>
            <StackPanel Margin="24" MinWidth="300">
                <!-- Add User form -->
                <StackPanel Visibility="{Binding IsAddDialogOpen,
                            Converter={StaticResource BoolToVisibilityConverter}}">
                    <TextBlock Text="Add User"
                               Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                               Margin="0,0,0,16"/>
                    <TextBox md:HintAssist.Hint="Username *"
                             Text="{Binding NewUsername, UpdateSourceTrigger=PropertyChanged}"
                             Margin="0,0,0,8"/>
                    <PasswordBox md:HintAssist.Hint="PIN *"
                                 x:Name="AddPinBox"
                                 Margin="0,0,0,8"/>
                    <TextBlock Text="{Binding ErrorMessage}"
                               Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                               MinHeight="20" TextWrapping="Wrap"/>
                    <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                        <Button Content="Add User" Click="OnConfirmAddUserClicked"
                                Style="{StaticResource MaterialDesignRaisedButton}" Margin="0,0,8,0"/>
                        <Button Content="Cancel" Command="{Binding CancelAddDialogCommand}"
                                Style="{StaticResource MaterialDesignOutlinedButton}"/>
                    </StackPanel>
                </StackPanel>

                <!-- Reset PIN form -->
                <StackPanel Visibility="{Binding IsResetPinDialogOpen,
                            Converter={StaticResource BoolToVisibilityConverter}}">
                    <TextBlock Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                               Margin="0,0,0,16">
                        <TextBlock.Text>
                            <MultiBinding StringFormat="Reset PIN for '{0}'">
                                <Binding Path="ResetPinTargetUser.Username"/>
                            </MultiBinding>
                        </TextBlock.Text>
                    </TextBlock>
                    <PasswordBox md:HintAssist.Hint="New PIN *"
                                 x:Name="ResetPinBox"
                                 Margin="0,0,0,8"/>
                    <TextBlock Text="{Binding ErrorMessage}"
                               Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                               MinHeight="20" TextWrapping="Wrap"/>
                    <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                        <Button Content="Reset PIN" Click="OnConfirmResetPinClicked"
                                Style="{StaticResource MaterialDesignRaisedButton}" Margin="0,0,8,0"/>
                        <Button Content="Cancel" Command="{Binding CancelResetPinDialogCommand}"
                                Style="{StaticResource MaterialDesignOutlinedButton}"/>
                    </StackPanel>
                </StackPanel>
            </StackPanel>
        </md:DialogHost.DialogContent>

        <!-- Main page content -->
        <Grid Margin="24">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
                <TextBlock Text="Users"
                           Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           VerticalAlignment="Center" Margin="0,0,16,0"/>
                <Button Content="+ Add User" Command="{Binding OpenAddDialogCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <DataGrid Grid.Row="1" ItemsSource="{Binding Users}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="Username" Binding="{Binding Username}" Width="*"/>
                    <DataGridTextColumn Header="Status"
                                        Binding="{Binding IsActive,
                                                  Converter={StaticResource BoolToActiveConverter}}"
                                        Width="100"/>
                    <DataGridTemplateColumn Width="200">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal">
                                    <Button Content="Reset PIN"
                                            Command="{Binding DataContext.OpenResetPinDialogCommand,
                                                      RelativeSource={RelativeSource AncestorType=UserControl}}"
                                            CommandParameter="{Binding}"
                                            Style="{StaticResource MaterialDesignFlatButton}" Padding="4,0"/>
                                    <Button Command="{Binding DataContext.ToggleActiveCommand,
                                                     RelativeSource={RelativeSource AncestorType=UserControl}}"
                                            CommandParameter="{Binding}"
                                            Style="{StaticResource MaterialDesignFlatButton}" Padding="4,0">
                                        <Button.Content>
                                            <TextBlock Text="{Binding IsActive,
                                                       Converter={StaticResource BoolToActiveConverter},
                                                       ConverterParameter=Deactivate|Reactivate}"/>
                                        </Button.Content>
                                    </Button>
                                </StackPanel>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
            <TextBlock Grid.Row="2" Text="{Binding ErrorMessage}"
                       Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                       Margin="0,8,0,0" MinHeight="20"/>
        </Grid>
    </md:DialogHost>
</UserControl>
```

**Note on `BoolToActiveConverter` with ConverterParameter:** The existing `BoolToActiveConverter` may not support a ConverterParameter for "Deactivate/Reactivate" toggle text. If it only converts to "Active"/"Inactive", use a separate `TextBlock` with a `DataTrigger` for the toggle button label instead:

```xml
<Button Command="..." CommandParameter="{Binding}" Style="..." Padding="4,0">
    <Button.Content>
        <TextBlock>
            <TextBlock.Style>
                <Style TargetType="TextBlock">
                    <Setter Property="Text" Value="Deactivate"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding IsActive}" Value="False">
                            <Setter Property="Text" Value="Reactivate"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </TextBlock.Style>
        </TextBlock>
    </Button.Content>
</Button>
```

- [ ] **Step 3: Create UsersView.xaml.cs**

```csharp
// SmartSolutions.App/Views/UsersView.xaml.cs
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views;

public partial class UsersView : UserControl
{
    public UsersView() => InitializeComponent();

    private void OnConfirmAddUserClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
            _ = vm.ConfirmAddUserAsync(vm.NewUsername, AddPinBox.Password);
    }

    private void OnConfirmResetPinClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
            _ = vm.ConfirmResetPinAsync(ResetPinBox.Password);
    }
}
```

- [ ] **Step 4: Wire navigation**

In `MainViewModel.cs`:
```csharp
[RelayCommand]
private void NavigateToUsers()
{
    CurrentSection = "Users";
    CurrentView = _services.GetRequiredService<UsersViewModel>();
    _ = ((UsersViewModel)CurrentView!).LoadAsync();
}
```

In `MainWindow.xaml` resources:
```xml
<DataTemplate DataType="{x:Type vm:UsersViewModel}">
    <views:UsersView />
</DataTemplate>
```

In `MainWindow.xaml` sidebar (after Payment Channels button):
```xml
<Button Content="Users" Command="{Binding NavigateToUsersCommand}"
        Style="{StaticResource MaterialDesignFlatButton}"
        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2"/>
```

In `ServiceConfiguration.cs`:
```csharp
services.AddTransient<UsersViewModel>();
```

- [ ] **Step 5: Build, smoke test, run tests, commit**

```
dotnet build SmartSolutions/SmartSolutions.sln
dotnet test SmartSolutions.Tests -v minimal
git add SmartSolutions/SmartSolutions.App/ViewModels/UsersViewModel.cs
git add SmartSolutions/SmartSolutions.App/Views/UsersView.xaml
git add SmartSolutions/SmartSolutions.App/Views/UsersView.xaml.cs
git add SmartSolutions/SmartSolutions.App/ViewModels/MainViewModel.cs
git add SmartSolutions/SmartSolutions.App/MainWindow.xaml
git add SmartSolutions/SmartSolutions.App/ServiceConfiguration.cs
git commit -m "feat: add Users management page with popup dialogs for add/reset-PIN"
```

---

## Task 8: Strip Settings to Business Info Only

Remove all sections from SettingsViewModel and SettingsView except Business Info. All navigation to these sections now goes through dedicated pages.

**Files:**
- Modify: `SmartSolutions.App/ViewModels/SettingsViewModel.cs`
- Modify: `SmartSolutions.App/Views/SettingsView.xaml`
- Modify: `SmartSolutions.App/Views/SettingsView.xaml.cs`

- [ ] **Step 1: Replace SettingsViewModel.cs**

Replace the entire file with a trimmed version:

```csharp
// SmartSolutions.App/ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.App.ViewModels;

public partial class SettingsViewModel(ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private BusinessInfo _businessInfo = new();
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            BusinessInfo  = await lookup.GetBusinessInfoAsync();
        }
        catch (Exception ex) { StatusMessage = $"Load failed: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveBusinessInfoAsync()
    {
        try
        {
            await lookup.SaveBusinessInfoAsync(BusinessInfo);
            StatusMessage = "Business info saved.";
        }
        catch (Exception ex) { StatusMessage = $"Failed to save: {ex.Message}"; }
    }
}
```

- [ ] **Step 2: Replace SettingsView.xaml**

Replace the entire file:

```xml
<!--SmartSolutions.App/Views/SettingsView.xaml-->
<UserControl x:Class="SmartSolutions.App.Views.SettingsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="24" MaxWidth="720">
            <TextBlock Text="Settings"
                       Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                       Margin="0,0,0,24"/>

            <md:Card Padding="16">
                <StackPanel>
                    <TextBlock Text="Business Information"
                               Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                               Margin="0,0,0,16"/>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="16"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <StackPanel Grid.Column="0">
                            <TextBox md:HintAssist.Hint="Business Name"
                                     Text="{Binding BusinessInfo.Name, UpdateSourceTrigger=PropertyChanged}"
                                     Margin="0,0,0,8"/>
                            <TextBox md:HintAssist.Hint="NTN"
                                     Text="{Binding BusinessInfo.Ntn, UpdateSourceTrigger=PropertyChanged}"
                                     Margin="0,0,0,8"/>
                            <TextBox md:HintAssist.Hint="Address"
                                     Text="{Binding BusinessInfo.Address, UpdateSourceTrigger=PropertyChanged}"
                                     Margin="0,0,0,8"/>
                        </StackPanel>
                        <StackPanel Grid.Column="2">
                            <TextBox md:HintAssist.Hint="Phone 1"
                                     Text="{Binding BusinessInfo.Phone1, UpdateSourceTrigger=PropertyChanged}"
                                     Margin="0,0,0,8"/>
                            <TextBox md:HintAssist.Hint="Phone 2"
                                     Text="{Binding BusinessInfo.Phone2, UpdateSourceTrigger=PropertyChanged}"
                                     Margin="0,0,0,8"/>
                            <TextBox md:HintAssist.Hint="Email"
                                     Text="{Binding BusinessInfo.Email, UpdateSourceTrigger=PropertyChanged}"
                                     Margin="0,0,0,8"/>
                        </StackPanel>
                    </Grid>
                    <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                        <Button Content="Save Business Info"
                                Command="{Binding SaveBusinessInfoCommand}"
                                Style="{StaticResource MaterialDesignRaisedButton}"
                                Margin="0,0,16,0"/>
                        <TextBlock Text="{Binding StatusMessage}"
                                   VerticalAlignment="Center" Opacity="0.7"/>
                    </StackPanel>
                </StackPanel>
            </md:Card>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: Replace SettingsView.xaml.cs**

The old code-behind had `OnAddUserClicked` and `OnResetPinClicked` methods. Remove them — the new view has no PasswordBox:

```csharp
// SmartSolutions.App/Views/SettingsView.xaml.cs
namespace SmartSolutions.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();
}
```

- [ ] **Step 4: Build**

```
dotnet build SmartSolutions/SmartSolutions.sln
```

Expected: 0 errors. If there are compilation errors due to removed properties/commands referenced elsewhere, fix them — but there should be none since all consumers have been moved to new ViewModels.

- [ ] **Step 5: Run full test suite**

```
dotnet test SmartSolutions.Tests -v minimal
```

Expected: All passing (no service logic was removed, only ViewModel code).

- [ ] **Step 6: Smoke test the app**

Run the app. Verify:
1. Settings page shows only Business Info card
2. All 6 new sidebar entries appear and each page loads correctly
3. Popup dialogs open and close for Add/Edit on each page
4. Delete with confirmation works on each page
5. Logged-in user display still works

- [ ] **Step 7: Final commit**

```
git add SmartSolutions/SmartSolutions.App/ViewModels/SettingsViewModel.cs
git add SmartSolutions/SmartSolutions.App/Views/SettingsView.xaml
git add SmartSolutions/SmartSolutions.App/Views/SettingsView.xaml.cs
git commit -m "refactor: strip Settings to Business Info only; all lookup management moved to dedicated pages"
```

---

## Final Sidebar Layout Reference

After all tasks, `MainWindow.xaml` sidebar should contain these buttons in order:

```
Dashboard
Print Orders
Haier Jobs
Expenses
Reports
──────────────
Items
Vendors
Technicians
Expense Categories
Payment Channels
Users
──────────────
Settings
Logged in as: {username}
```

The second separator goes between Reports and Items, and a third separator between Users and Settings.
