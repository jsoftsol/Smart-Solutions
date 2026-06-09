# Customers Page — Design Spec

**Date:** 2026-06-10
**Status:** Approved

---

## Overview

Add a Customers management page to the Smart Solutions WPF app. Customers are the individuals or businesses that place print orders and Haier service jobs. They need a dedicated page for browsing, adding, editing, and deleting customer records — consistent with the existing Vendors, Technicians, and other management pages.

---

## Current State

The following already exist and require no changes:

- `SmartSolutions.Data/Entities/Customer.cs` — entity with `Id`, `Name`, `Phone`, `Address`, `Notes`, `CreatedById`
- `SmartSolutions.Data/AppDbContext.cs` — `DbSet<Customer> Customers` already present
- `SmartSolutions.Core/Interfaces/ICustomerService.cs` — interface with `SearchCustomersAsync`, `GetCustomerAsync`, `AddCustomerAsync`, `UpdateCustomerAsync`
- `SmartSolutions.Core/Services/CustomerService.cs` — full implementation
- `SmartSolutions.App/ServiceConfiguration.cs` — `ICustomerService` registered as singleton
- Database migration — `Customers` table created in `InitialCreate`

---

## Changes Required

### 1. `ICustomerService` — add Delete

Add one method to the interface:

```csharp
Task DeleteCustomerAsync(int id);
```

### 2. `CustomerService` — implement Delete

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

If the customer has related print orders or Haier jobs, EF Core will throw a `DbUpdateException` (FK violation). The ViewModel catches this and surfaces a user-friendly message.

### 3. `CustomersViewModel`

Location: `SmartSolutions.App/ViewModels/CustomersViewModel.cs`

Mirrors `VendorsViewModel` with these differences:
- Injected service: `ICustomerService` (not `ILookupService`)
- Extra dialog field: `DialogAddress`
- Delete error handling: catches `DbUpdateException` and sets `StatusMessage = "Cannot delete a customer who has existing orders."`
- Load: calls `SearchCustomersAsync(null)` to retrieve all customers

Observable properties:
- `Customers` — `ObservableCollection<Customer>`
- `IsDialogOpen`, `IsBusy`
- `DialogTitle`, `DialogName`, `DialogPhone`, `DialogAddress`, `DialogNotes`
- `DialogErrorMessage`, `StatusMessage`

Commands: `OpenAddDialog`, `OpenEditDialog(Customer)`, `SaveDialog`, `CancelDialog`, `Delete(Customer)`

### 4. `CustomersView.xaml`

Location: `SmartSolutions.App/Views/CustomersView.xaml`

Mirrors `VendorsView.xaml` with:
- DataGrid columns: Name (`*`), Phone (150), Address (200), Notes (200), Actions (130)
- Dialog adds an Address TextBox between Phone and Notes
- No search box — consistent with Vendors/Technicians pages

### 5. `ServiceConfiguration.cs`

Add:
```csharp
services.AddTransient<ViewModels.CustomersViewModel>();
```

### 6. `MainViewModel.cs`

Add:
```csharp
[RelayCommand]
private void NavigateToCustomers()
{
    CurrentSection = "Customers";
    CurrentView = _services.GetRequiredService<CustomersViewModel>();
    _ = ((CustomersViewModel)CurrentView!).LoadAsync();
}
```

### 7. `MainWindow.xaml`

Add DataTemplate in `Window.Resources`:
```xml
<DataTemplate DataType="{x:Type vm:CustomersViewModel}">
    <views:CustomersView />
</DataTemplate>
```

Add sidebar button in the management section, first position after the separator (before Items):
```xml
<Button Content="Customers" Command="{Binding NavigateToCustomersCommand}" ... />
```

---

## Sidebar Placement

```
Dashboard
Print Orders
Haier Jobs
Expenses
Reports
─────────────────
Customers         ← NEW (first in management section)
Items
Vendors
Technicians
Expense Categories
Payment Channels
Users
─────────────────
Settings
```

---

## Constraints

- No EF migration needed — table already exists
- No roles or permission checks — consistent with the rest of the app
- Name is required; all other fields are optional
- Delete is blocked at DB level if FK exists; ViewModel shows a friendly error message
- No `Total` column, no computed fields — this is a simple reference entity
