# Smart Solutions — Full App Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the complete Smart Solutions WPF desktop app — print order management, Haier AC job tracking, expense recording, PDF invoice generation, and financial reporting.

**Architecture:** WPF + MVVM using CommunityToolkit.Mvvm across three projects: `SmartSolutions.Data` (EF Core entities + DbContext), `SmartSolutions.Core` (service interfaces + implementations), `SmartSolutions.App` (Views + ViewModels + DI host). All data access is async through services that receive `IDbContextFactory<AppDbContext>`; ViewModels are injected via `Microsoft.Extensions.Hosting`. No business logic in code-behind.

**Tech Stack:** .NET 10, C#, WPF, CommunityToolkit.Mvvm 8.x, EF Core 10 + SQL Server Express, MaterialDesignThemes 5.x, FastReport.Community, Microsoft.Extensions.Hosting 10.x, xUnit + NSubstitute + FluentAssertions for tests.

---

## File Map

### SmartSolutions.Data/
| File | Responsibility |
|------|---------------|
| `Entities/Enums.cs` | All enums: `PrintOrderStatus`, `HaierJobType`, `HaierJobStatus`, `RateType`, `DimensionUnit` |
| `Entities/Customer.cs` | Customer entity |
| `Entities/ItemCategory.cs` | Lookup — item category |
| `Entities/ItemName.cs` | Lookup — item name (FK to category) |
| `Entities/Vendor.cs` | Lookup — vendor |
| `Entities/Technician.cs` | Lookup — technician |
| `Entities/ExpenseCategory.cs` | Lookup — expense category |
| `Entities/PaymentChannel.cs` | Lookup — payment channel |
| `Entities/BusinessInfo.cs` | Singleton settings row (always Id=1) |
| `Entities/PrintOrder.cs` | Print order header |
| `Entities/PrintOrderLine.cs` | Line item with RateType/Unit/dimensions |
| `Entities/PrintOrderVendorAssignment.cs` | Vendor assignment per order |
| `Entities/PrintOrderPayment.cs` | Customer payment against an order |
| `Entities/HaierJob.cs` | Haier AC service job |
| `Entities/HaierJobPayment.cs` | Customer payment against a job |
| `Entities/Expense.cs` | Business expense record |
| `AppDbContext.cs` | EF Core DbContext with all DbSets and configuration |

### SmartSolutions.Core/
| File | Responsibility |
|------|---------------|
| `Interfaces/ILookupService.cs` | Contract for all lookup table CRUD |
| `Interfaces/ICustomerService.cs` | Contract for customer CRUD and search |
| `Interfaces/IPrintOrderService.cs` | Contract for orders, lines, payments, vendor assignment |
| `Interfaces/IHaierJobService.cs` | Contract for jobs and payments |
| `Interfaces/IExpenseService.cs` | Contract for expenses |
| `Interfaces/IDashboardService.cs` | Contract for dashboard aggregates |
| `Services/LookupService.cs` | Implements `ILookupService` |
| `Services/CustomerService.cs` | Implements `ICustomerService` |
| `Services/PrintOrderService.cs` | Implements `IPrintOrderService` |
| `Services/HaierJobService.cs` | Implements `IHaierJobService` |
| `Services/ExpenseService.cs` | Implements `IExpenseService` |
| `Services/DashboardService.cs` | Implements `IDashboardService` |

### SmartSolutions.App/
| File | Responsibility |
|------|---------------|
| `appsettings.json` | Connection string |
| `App.xaml` / `App.xaml.cs` | DI host bootstrap, Material Design resources |
| `ServiceConfiguration.cs` | All `services.Add*` registrations |
| `MainWindow.xaml` / `.cs` | Shell — sidebar nav + content area |
| `ViewModels/MainViewModel.cs` | Navigation state and current view switching |
| `ViewModels/DashboardViewModel.cs` | Dashboard aggregates |
| `ViewModels/PrintOrdersViewModel.cs` | Order list with filters |
| `ViewModels/PrintOrderDetailViewModel.cs` | Create/edit order, lines, payments |
| `ViewModels/HaierJobsViewModel.cs` | Job list with filters |
| `ViewModels/HaierJobDetailViewModel.cs` | Create/edit job and payments |
| `ViewModels/ExpensesViewModel.cs` | Expense list and add form |
| `ViewModels/SettingsViewModel.cs` | All lookup CRUD + business info + connection string |
| `Views/DashboardView.xaml` | Dashboard UI |
| `Views/PrintOrdersView.xaml` | Order list UI |
| `Views/PrintOrderDetailView.xaml` | Order form UI |
| `Views/HaierJobsView.xaml` | Job list UI |
| `Views/HaierJobDetailView.xaml` | Job form UI |
| `Views/ExpensesView.xaml` | Expenses UI |
| `Views/SettingsView.xaml` | Settings UI |
| `Converters/BoolToVisibilityConverter.cs` | bool → Visibility |
| `Converters/InverseBoolToVisibilityConverter.cs` | !bool → Visibility |
| `Converters/RateTypeToVisibilityConverter.cs` | RateType.PerSqft → Visible |
| `Converters/CurrencyConverter.cs` | decimal → "PKR #,##0.00" |
| `Reports/Invoice.frx` | FastReport template for PDF invoice |

### SmartSolutions.Tests/
| File | Responsibility |
|------|---------------|
| `Helpers/TestDbContextFactory.cs` | In-memory IDbContextFactory for tests |
| `Services/LookupServiceTests.cs` | CRUD tests for lookup service |
| `Services/PrintOrderServiceTests.cs` | Order/line/payment service tests |
| `Services/HaierJobServiceTests.cs` | Job/payment service tests |
| `Services/ExpenseServiceTests.cs` | Expense service tests |
| `ViewModels/PrintOrderDetailViewModelTests.cs` | Line total computation and validation tests |

---

## Phase 1: Solution & Data Layer

### Task 1: Create solution and project structure

**Files:**
- Create: `SmartSolutions/SmartSolutions.sln`
- Create: `SmartSolutions/SmartSolutions.Data/SmartSolutions.Data.csproj`
- Create: `SmartSolutions/SmartSolutions.Core/SmartSolutions.Core.csproj`
- Create: `SmartSolutions/SmartSolutions.App/SmartSolutions.App.csproj`
- Create: `SmartSolutions/SmartSolutions.Tests/SmartSolutions.Tests.csproj`

- [ ] **Step 1: Scaffold solution and all four projects**

Run from `D:\Documents\Programs\Visual Studio 2026\Smart Solutions`:

```powershell
New-Item -ItemType Directory SmartSolutions; Set-Location SmartSolutions
dotnet new sln -n SmartSolutions
dotnet new classlib -n SmartSolutions.Data   -f net10.0 -o SmartSolutions.Data
dotnet new classlib -n SmartSolutions.Core   -f net10.0 -o SmartSolutions.Core
dotnet new wpf      -n SmartSolutions.App    -f net10.0 -o SmartSolutions.App
dotnet new xunit    -n SmartSolutions.Tests  -f net10.0 -o SmartSolutions.Tests
dotnet sln add SmartSolutions.Data SmartSolutions.Core SmartSolutions.App SmartSolutions.Tests
```

Expected: `Project(s) added to solution.`

- [ ] **Step 2: Wire up project references**

```powershell
dotnet add SmartSolutions.Core\SmartSolutions.Core.csproj   reference SmartSolutions.Data\SmartSolutions.Data.csproj
dotnet add SmartSolutions.App\SmartSolutions.App.csproj     reference SmartSolutions.Core\SmartSolutions.Core.csproj SmartSolutions.Data\SmartSolutions.Data.csproj
dotnet add SmartSolutions.Tests\SmartSolutions.Tests.csproj reference SmartSolutions.Core\SmartSolutions.Core.csproj SmartSolutions.Data\SmartSolutions.Data.csproj
```

- [ ] **Step 3: Remove default boilerplate**

```powershell
Remove-Item SmartSolutions.Data\Class1.cs
Remove-Item SmartSolutions.Core\Class1.cs
```

- [ ] **Step 4: Build to verify**

```powershell
dotnet build SmartSolutions.sln
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Init git and commit**

```powershell
git init
git add .
git commit -m "chore: scaffold solution — Data, Core, App, Tests"
```

---

### Task 2: Install NuGet packages

**Files:**
- Modify: `SmartSolutions.Data/SmartSolutions.Data.csproj`
- Modify: `SmartSolutions.Core/SmartSolutions.Core.csproj`
- Modify: `SmartSolutions.App/SmartSolutions.App.csproj`
- Modify: `SmartSolutions.Tests/SmartSolutions.Tests.csproj`

- [ ] **Step 1: Data project packages**

```powershell
Set-Location SmartSolutions.Data
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
Set-Location ..
```

- [ ] **Step 2: App project packages**

```powershell
Set-Location SmartSolutions.App
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package CommunityToolkit.Mvvm
dotnet add package MaterialDesignThemes
dotnet add package Microsoft.Extensions.Hosting
dotnet add package FastReport.Community
Set-Location ..
```

> **Note:** If `FastReport.Community` is not found on NuGet, try `FastReport.OpenSource` instead. Verify at nuget.org before running.

- [ ] **Step 3: Test project packages**

```powershell
Set-Location SmartSolutions.Tests
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package NSubstitute
dotnet add package FluentAssertions
Set-Location ..
```

- [ ] **Step 4: Build to verify all packages resolve**

```powershell
dotnet build SmartSolutions.sln
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```powershell
git add .
git commit -m "chore: add NuGet packages to all projects"
```

---

### Task 3: Define enums and lookup entities

**Files:**
- Create: `SmartSolutions.Data/Entities/Enums.cs`
- Create: `SmartSolutions.Data/Entities/ItemCategory.cs`
- Create: `SmartSolutions.Data/Entities/ItemName.cs`
- Create: `SmartSolutions.Data/Entities/Vendor.cs`
- Create: `SmartSolutions.Data/Entities/Technician.cs`
- Create: `SmartSolutions.Data/Entities/ExpenseCategory.cs`
- Create: `SmartSolutions.Data/Entities/PaymentChannel.cs`
- Create: `SmartSolutions.Data/Entities/BusinessInfo.cs`
- Create: `SmartSolutions.Data/Entities/Customer.cs`

- [ ] **Step 1: Create `Entities/Enums.cs`**

```csharp
// SmartSolutions.Data/Entities/Enums.cs
namespace SmartSolutions.Data.Entities;

public enum PrintOrderStatus { Draft, Confirmed, SentToVendor, Ready, Delivered }
public enum HaierJobType    { Warranty, OutOfWarranty }
public enum HaierJobStatus  { Pending, InProgress, Completed }
public enum RateType        { PerSqft, PerPiece }
public enum DimensionUnit   { Feet, Inches }
```

- [ ] **Step 2: Create lookup entity files**

```csharp
// SmartSolutions.Data/Entities/ItemCategory.cs
namespace SmartSolutions.Data.Entities;

public class ItemCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<ItemName> ItemNames { get; set; } = [];
}
```

```csharp
// SmartSolutions.Data/Entities/ItemName.cs
namespace SmartSolutions.Data.Entities;

public class ItemName
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public ItemCategory Category { get; set; } = null!;
}
```

```csharp
// SmartSolutions.Data/Entities/Vendor.cs
namespace SmartSolutions.Data.Entities;

public class Vendor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}
```

```csharp
// SmartSolutions.Data/Entities/Technician.cs
namespace SmartSolutions.Data.Entities;

public class Technician
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
}
```

```csharp
// SmartSolutions.Data/Entities/ExpenseCategory.cs
namespace SmartSolutions.Data.Entities;

public class ExpenseCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

```csharp
// SmartSolutions.Data/Entities/PaymentChannel.cs
namespace SmartSolutions.Data.Entities;

public class PaymentChannel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

```csharp
// SmartSolutions.Data/Entities/BusinessInfo.cs
namespace SmartSolutions.Data.Entities;

public class BusinessInfo
{
    public int Id { get; set; }  // Always 1 — singleton row
    public string Name    { get; set; } = "";
    public string Ntn     { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone1  { get; set; } = "";
    public string? Phone2 { get; set; }
    public string? Email  { get; set; }
    public byte[]? Logo   { get; set; }
}
```

```csharp
// SmartSolutions.Data/Entities/Customer.cs
namespace SmartSolutions.Data.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Name    { get; set; } = "";
    public string? Phone   { get; set; }
    public string? Address { get; set; }
    public string? Notes   { get; set; }
}
```

- [ ] **Step 3: Build to catch any typos**

```powershell
dotnet build SmartSolutions.Data
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```powershell
git add SmartSolutions.Data/Entities
git commit -m "feat(data): add enums, lookup entities, Customer, BusinessInfo"
```

---

### Task 4: Define print order and Haier job entities

**Files:**
- Create: `SmartSolutions.Data/Entities/PrintOrder.cs`
- Create: `SmartSolutions.Data/Entities/PrintOrderLine.cs`
- Create: `SmartSolutions.Data/Entities/PrintOrderVendorAssignment.cs`
- Create: `SmartSolutions.Data/Entities/PrintOrderPayment.cs`
- Create: `SmartSolutions.Data/Entities/HaierJob.cs`
- Create: `SmartSolutions.Data/Entities/HaierJobPayment.cs`
- Create: `SmartSolutions.Data/Entities/Expense.cs`

- [ ] **Step 1: Create `PrintOrder.cs`**

```csharp
// SmartSolutions.Data/Entities/PrintOrder.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime Date { get; set; }
    public PrintOrderStatus Status { get; set; } = PrintOrderStatus.Draft;
    public decimal? TransportationCharges { get; set; }
    public string? Notes { get; set; }

    public ICollection<PrintOrderLine>             Lines             { get; set; } = [];
    public ICollection<PrintOrderVendorAssignment> VendorAssignments { get; set; } = [];
    public ICollection<PrintOrderPayment>          Payments          { get; set; } = [];
}
```

- [ ] **Step 2: Create `PrintOrderLine.cs`**

```csharp
// SmartSolutions.Data/Entities/PrintOrderLine.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PrintOrder Order { get; set; } = null!;
    public int ItemNameId { get; set; }
    public ItemName ItemName { get; set; } = null!;

    public RateType     RateType { get; set; } = RateType.PerSqft;
    public DimensionUnit Unit    { get; set; } = DimensionUnit.Feet;
    public decimal? Height       { get; set; }
    public decimal? Width        { get; set; }
    public int      Quantity     { get; set; }
    public decimal  Rate         { get; set; }

    // Never stored — computed in application layer
    public decimal ComputeTotal()
    {
        if (RateType == RateType.PerPiece)
            return Quantity * Rate;

        var h = Height ?? 0m;
        var w = Width  ?? 0m;
        if (Unit == DimensionUnit.Inches)
        {
            h /= 12m;
            w /= 12m;
        }
        return h * w * Quantity * Rate;
    }
}
```

- [ ] **Step 3: Create `PrintOrderVendorAssignment.cs`**

```csharp
// SmartSolutions.Data/Entities/PrintOrderVendorAssignment.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrderVendorAssignment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PrintOrder Order { get; set; } = null!;
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public DateTime SentDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public decimal VendorCost { get; set; }
    public bool VendorPaid { get; set; }
    public DateTime? VendorPaidDate { get; set; }
}
```

- [ ] **Step 4: Create `PrintOrderPayment.cs`**

```csharp
// SmartSolutions.Data/Entities/PrintOrderPayment.cs
namespace SmartSolutions.Data.Entities;

public class PrintOrderPayment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PrintOrder Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public int ChannelId { get; set; }
    public PaymentChannel Channel { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
```

- [ ] **Step 5: Create `HaierJob.cs`**

```csharp
// SmartSolutions.Data/Entities/HaierJob.cs
namespace SmartSolutions.Data.Entities;

public class HaierJob
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string AcModel { get; set; } = "";
    public string? AcSerial { get; set; }
    public string ProblemDescription { get; set; } = "";
    public int TechnicianId { get; set; }
    public Technician Technician { get; set; } = null!;
    public HaierJobType JobType { get; set; }
    public HaierJobStatus Status { get; set; } = HaierJobStatus.Pending;
    public string? ClaimReferenceNumber { get; set; }  // Warranty jobs only
    public string? PartsUsed { get; set; }
    public decimal PartsCost { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }

    public ICollection<HaierJobPayment> Payments { get; set; } = [];
}
```

- [ ] **Step 6: Create `HaierJobPayment.cs`**

```csharp
// SmartSolutions.Data/Entities/HaierJobPayment.cs
namespace SmartSolutions.Data.Entities;

public class HaierJobPayment
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public HaierJob Job { get; set; } = null!;
    public decimal Amount { get; set; }
    public int ChannelId { get; set; }
    public PaymentChannel Channel { get; set; } = null!;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
```

- [ ] **Step 7: Create `Expense.cs`**

```csharp
// SmartSolutions.Data/Entities/Expense.cs
namespace SmartSolutions.Data.Entities;

public class Expense
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int ChannelId { get; set; }
    public PaymentChannel Channel { get; set; } = null!;
    public DateTime Date { get; set; }
}
```

- [ ] **Step 8: Build**

```powershell
dotnet build SmartSolutions.Data
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 9: Commit**

```powershell
git add SmartSolutions.Data/Entities
git commit -m "feat(data): add PrintOrder, HaierJob, Expense entities with navigation properties"
```

---

### Task 5: AppDbContext and initial migration

**Files:**
- Create: `SmartSolutions.Data/AppDbContext.cs`

- [ ] **Step 1: Create `AppDbContext.cs`**

```csharp
// SmartSolutions.Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ItemCategory>               ItemCategories           { get; set; }
    public DbSet<ItemName>                   ItemNames                { get; set; }
    public DbSet<Vendor>                     Vendors                  { get; set; }
    public DbSet<Technician>                 Technicians              { get; set; }
    public DbSet<ExpenseCategory>            ExpenseCategories        { get; set; }
    public DbSet<PaymentChannel>             PaymentChannels          { get; set; }
    public DbSet<BusinessInfo>               BusinessInfos            { get; set; }
    public DbSet<Customer>                   Customers                { get; set; }
    public DbSet<PrintOrder>                 PrintOrders              { get; set; }
    public DbSet<PrintOrderLine>             PrintOrderLines          { get; set; }
    public DbSet<PrintOrderVendorAssignment> PrintOrderVendorAssignments { get; set; }
    public DbSet<PrintOrderPayment>          PrintOrderPayments       { get; set; }
    public DbSet<HaierJob>                   HaierJobs                { get; set; }
    public DbSet<HaierJobPayment>            HaierJobPayments         { get; set; }
    public DbSet<Expense>                    Expenses                 { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // BusinessInfo is a singleton — seed the one row
        modelBuilder.Entity<BusinessInfo>().HasData(new BusinessInfo
        {
            Id = 1, Name = "Smart Solutions", Ntn = "7569020-2",
            Address = "", Phone1 = ""
        });

        // Seed default payment channels
        modelBuilder.Entity<PaymentChannel>().HasData(
            new PaymentChannel { Id = 1, Name = "Cash"      },
            new PaymentChannel { Id = 2, Name = "Easypaisa" },
            new PaymentChannel { Id = 3, Name = "Bank"      }
        );

        // Store enums as strings for readability in the database
        modelBuilder.Entity<PrintOrder>()
            .Property(o => o.Status)
            .HasConversion<string>();

        modelBuilder.Entity<PrintOrderLine>()
            .Property(l => l.RateType)
            .HasConversion<string>();

        modelBuilder.Entity<PrintOrderLine>()
            .Property(l => l.Unit)
            .HasConversion<string>();

        modelBuilder.Entity<HaierJob>()
            .Property(j => j.JobType)
            .HasConversion<string>();

        modelBuilder.Entity<HaierJob>()
            .Property(j => j.Status)
            .HasConversion<string>();

        // Decimal precision for money fields
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }
}
```

- [ ] **Step 2: Add EF Core design tools reference to App project (needed for migrations)**

In `SmartSolutions.App/SmartSolutions.App.csproj`, add inside `<ItemGroup>`:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="*">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

- [ ] **Step 3: Add a `DesignTimeDbContextFactory` for migrations tooling**

```csharp
// SmartSolutions.Data/DesignTimeDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartSolutions.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=SmartSolutions;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new AppDbContext(options);
    }
}
```

- [ ] **Step 4: Build**

```powershell
dotnet build SmartSolutions.sln
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Create initial migration**

```powershell
dotnet ef migrations add InitialCreate --project SmartSolutions.Data --startup-project SmartSolutions.App
```

Expected: `Done. To undo this action, use 'ef migrations remove'`
A `Migrations/` folder with three files will appear in `SmartSolutions.Data`.

- [ ] **Step 6: Commit**

```powershell
git add .
git commit -m "feat(data): add AppDbContext with seed data and InitialCreate migration"
```

---

## Phase 2: Core Services

### Task 6: Test helper + ILookupService + LookupService

**Files:**
- Create: `SmartSolutions.Tests/Helpers/TestDbContextFactory.cs`
- Create: `SmartSolutions.Core/Interfaces/ILookupService.cs`
- Create: `SmartSolutions.Core/Services/LookupService.cs`
- Create: `SmartSolutions.Tests/Services/LookupServiceTests.cs`

- [ ] **Step 1: Create `TestDbContextFactory.cs`**

```csharp
// SmartSolutions.Tests/Helpers/TestDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Data;

namespace SmartSolutions.Tests.Helpers;

public class TestDbContextFactory(string dbName) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    // Convenience: unique db per test to ensure isolation
    public static TestDbContextFactory Unique() =>
        new(Guid.NewGuid().ToString());
}
```

- [ ] **Step 2: Create `ILookupService.cs`**

```csharp
// SmartSolutions.Core/Interfaces/ILookupService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface ILookupService
{
    Task<List<ItemCategory>> GetItemCategoriesAsync();
    Task<ItemCategory>       AddItemCategoryAsync(string name);
    Task                     RenameItemCategoryAsync(int id, string name);
    Task                     DeleteItemCategoryAsync(int id);

    Task<List<ItemName>> GetItemNamesAsync(int? categoryId = null);
    Task<ItemName>       AddItemNameAsync(string name, int categoryId);
    Task                 RenameItemNameAsync(int id, string name);
    Task                 DeleteItemNameAsync(int id);

    Task<List<Vendor>> GetVendorsAsync();
    Task<Vendor>       AddVendorAsync(string name, string? phone, string? notes);
    Task               UpdateVendorAsync(int id, string name, string? phone, string? notes);
    Task               DeleteVendorAsync(int id);

    Task<List<Technician>> GetTechniciansAsync();
    Task<Technician>       AddTechnicianAsync(string name, string? phone);
    Task                   UpdateTechnicianAsync(int id, string name, string? phone);
    Task                   DeleteTechnicianAsync(int id);

    Task<List<ExpenseCategory>> GetExpenseCategoriesAsync();
    Task<ExpenseCategory>       AddExpenseCategoryAsync(string name);
    Task                        RenameExpenseCategoryAsync(int id, string name);
    Task                        DeleteExpenseCategoryAsync(int id);

    Task<List<PaymentChannel>> GetPaymentChannelsAsync();
    Task<PaymentChannel>       AddPaymentChannelAsync(string name);
    Task                       RenamePaymentChannelAsync(int id, string name);
    Task                       DeletePaymentChannelAsync(int id);

    Task<BusinessInfo> GetBusinessInfoAsync();
    Task               SaveBusinessInfoAsync(BusinessInfo info);
}
```

- [ ] **Step 3: Write the failing test**

```csharp
// SmartSolutions.Tests/Services/LookupServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class LookupServiceTests
{
    [Fact]
    public async Task AddItemCategoryAsync_PersistsAndReturnsCategory()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new LookupService(factory);

        var result = await svc.AddItemCategoryAsync("Panaflex");

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Panaflex");
        var all = await svc.GetItemCategoriesAsync();
        all.Should().ContainSingle(c => c.Name == "Panaflex");
    }

    [Fact]
    public async Task DeleteItemCategoryAsync_RemovesCategory()
    {
        var factory = TestDbContextFactory.Unique();
        var svc = new LookupService(factory);
        var cat = await svc.AddItemCategoryAsync("ToDelete");

        await svc.DeleteItemCategoryAsync(cat.Id);

        var all = await svc.GetItemCategoriesAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentChannelsAsync_ReturnsSeedDefaults()
    {
        // InMemory does not run HasData seeds — insert manually to mirror seed intent
        var factory = TestDbContextFactory.Unique();
        await using var ctx = factory.CreateDbContext();
        ctx.PaymentChannels.AddRange(
            new() { Id = 1, Name = "Cash" },
            new() { Id = 2, Name = "Easypaisa" },
            new() { Id = 3, Name = "Bank" });
        await ctx.SaveChangesAsync();

        var svc = new LookupService(factory);
        var channels = await svc.GetPaymentChannelsAsync();

        channels.Should().HaveCount(3);
        channels.Select(c => c.Name).Should().Contain(["Cash", "Easypaisa", "Bank"]);
    }
}
```

- [ ] **Step 4: Run test to confirm it fails**

```powershell
dotnet test SmartSolutions.Tests --filter "LookupServiceTests" -v n
```

Expected: FAIL — `LookupService` not found.

- [ ] **Step 5: Implement `LookupService.cs`**

```csharp
// SmartSolutions.Core/Services/LookupService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class LookupService(IDbContextFactory<AppDbContext> factory) : ILookupService
{
    public async Task<List<ItemCategory>> GetItemCategoriesAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.ItemCategories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<ItemCategory> AddItemCategoryAsync(string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = new ItemCategory { Name = name };
        db.ItemCategories.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task RenameItemCategoryAsync(int id, string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ItemCategories.FindAsync(id)
            ?? throw new InvalidOperationException($"ItemCategory {id} not found");
        entity.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeleteItemCategoryAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ItemCategories.FindAsync(id)
            ?? throw new InvalidOperationException($"ItemCategory {id} not found");
        db.ItemCategories.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<ItemName>> GetItemNamesAsync(int? categoryId = null)
    {
        await using var db = factory.CreateDbContext();
        var q = db.ItemNames.Include(n => n.Category).AsQueryable();
        if (categoryId.HasValue) q = q.Where(n => n.CategoryId == categoryId.Value);
        return await q.OrderBy(n => n.Name).ToListAsync();
    }

    public async Task<ItemName> AddItemNameAsync(string name, int categoryId)
    {
        await using var db = factory.CreateDbContext();
        var entity = new ItemName { Name = name, CategoryId = categoryId };
        db.ItemNames.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task RenameItemNameAsync(int id, string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ItemNames.FindAsync(id)
            ?? throw new InvalidOperationException($"ItemName {id} not found");
        entity.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeleteItemNameAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ItemNames.FindAsync(id)
            ?? throw new InvalidOperationException($"ItemName {id} not found");
        db.ItemNames.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<Vendor>> GetVendorsAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.Vendors.OrderBy(v => v.Name).ToListAsync();
    }

    public async Task<Vendor> AddVendorAsync(string name, string? phone, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new Vendor { Name = name, Phone = phone, Notes = notes };
        db.Vendors.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateVendorAsync(int id, string name, string? phone, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Vendors.FindAsync(id)
            ?? throw new InvalidOperationException($"Vendor {id} not found");
        entity.Name = name; entity.Phone = phone; entity.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteVendorAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Vendors.FindAsync(id)
            ?? throw new InvalidOperationException($"Vendor {id} not found");
        db.Vendors.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<Technician>> GetTechniciansAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.Technicians.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<Technician> AddTechnicianAsync(string name, string? phone)
    {
        await using var db = factory.CreateDbContext();
        var entity = new Technician { Name = name, Phone = phone };
        db.Technicians.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateTechnicianAsync(int id, string name, string? phone)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Technicians.FindAsync(id)
            ?? throw new InvalidOperationException($"Technician {id} not found");
        entity.Name = name; entity.Phone = phone;
        await db.SaveChangesAsync();
    }

    public async Task DeleteTechnicianAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Technicians.FindAsync(id)
            ?? throw new InvalidOperationException($"Technician {id} not found");
        db.Technicians.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.ExpenseCategories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<ExpenseCategory> AddExpenseCategoryAsync(string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = new ExpenseCategory { Name = name };
        db.ExpenseCategories.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task RenameExpenseCategoryAsync(int id, string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ExpenseCategories.FindAsync(id)
            ?? throw new InvalidOperationException($"ExpenseCategory {id} not found");
        entity.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeleteExpenseCategoryAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.ExpenseCategories.FindAsync(id)
            ?? throw new InvalidOperationException($"ExpenseCategory {id} not found");
        db.ExpenseCategories.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<PaymentChannel>> GetPaymentChannelsAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.PaymentChannels.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<PaymentChannel> AddPaymentChannelAsync(string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = new PaymentChannel { Name = name };
        db.PaymentChannels.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task RenamePaymentChannelAsync(int id, string name)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PaymentChannels.FindAsync(id)
            ?? throw new InvalidOperationException($"PaymentChannel {id} not found");
        entity.Name = name;
        await db.SaveChangesAsync();
    }

    public async Task DeletePaymentChannelAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PaymentChannels.FindAsync(id)
            ?? throw new InvalidOperationException($"PaymentChannel {id} not found");
        db.PaymentChannels.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<BusinessInfo> GetBusinessInfoAsync()
    {
        await using var db = factory.CreateDbContext();
        return await db.BusinessInfos.FindAsync(1) ?? new BusinessInfo { Id = 1 };
    }

    public async Task SaveBusinessInfoAsync(BusinessInfo info)
    {
        await using var db = factory.CreateDbContext();
        info.Id = 1;
        if (await db.BusinessInfos.AnyAsync(b => b.Id == 1))
            db.BusinessInfos.Update(info);
        else
            db.BusinessInfos.Add(info);
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 6: Run tests**

```powershell
dotnet test SmartSolutions.Tests --filter "LookupServiceTests" -v n
```

Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 7: Commit**

```powershell
git add .
git commit -m "feat(core): add ILookupService + LookupService with tests"
```

---

### Task 7: ICustomerService + CustomerService

**Files:**
- Create: `SmartSolutions.Core/Interfaces/ICustomerService.cs`
- Create: `SmartSolutions.Core/Services/CustomerService.cs`

- [ ] **Step 1: Create `ICustomerService.cs`**

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
}
```

- [ ] **Step 2: Implement `CustomerService.cs`**

```csharp
// SmartSolutions.Core/Services/CustomerService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class CustomerService(IDbContextFactory<AppDbContext> factory) : ICustomerService
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
        var entity = new Customer { Name = name, Phone = phone, Address = address, Notes = notes };
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
}
```

- [ ] **Step 3: Build and commit**

```powershell
dotnet build SmartSolutions.sln
git add .
git commit -m "feat(core): add ICustomerService + CustomerService"
```

---

### Task 8: IPrintOrderService + PrintOrderService

**Files:**
- Create: `SmartSolutions.Core/Interfaces/IPrintOrderService.cs`
- Create: `SmartSolutions.Core/Services/PrintOrderService.cs`
- Create: `SmartSolutions.Tests/Services/PrintOrderServiceTests.cs`

- [ ] **Step 1: Create `IPrintOrderService.cs`**

```csharp
// SmartSolutions.Core/Interfaces/IPrintOrderService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface IPrintOrderService
{
    Task<List<PrintOrder>> GetOrdersAsync(PrintOrderStatus? status = null, int? customerId = null,
        DateTime? from = null, DateTime? to = null, bool outstandingOnly = false);
    Task<PrintOrder>       GetOrderWithDetailsAsync(int id);
    Task<PrintOrder>       CreateOrderAsync(int customerId, DateTime date, string? notes);
    Task                   UpdateOrderHeaderAsync(int id, int customerId, DateTime date,
        PrintOrderStatus status, decimal? transportationCharges, string? notes);
    Task                   DeleteOrderAsync(int id);

    Task<PrintOrderLine>   AddLineAsync(int orderId, int itemNameId, RateType rateType,
        DimensionUnit unit, decimal? height, decimal? width, int quantity, decimal rate);
    Task                   UpdateLineAsync(int lineId, int itemNameId, RateType rateType,
        DimensionUnit unit, decimal? height, decimal? width, int quantity, decimal rate);
    Task                   DeleteLineAsync(int lineId);

    Task<PrintOrderVendorAssignment> SetVendorAssignmentAsync(int orderId, int vendorId,
        DateTime sentDate, DateTime? expectedDate, decimal vendorCost);
    Task                             MarkVendorPaidAsync(int assignmentId, DateTime paidDate);

    Task<PrintOrderPayment>          AddPaymentAsync(int orderId, decimal amount,
        int channelId, DateTime date, string? notes);
    Task                             DeletePaymentAsync(int paymentId);

    Task<bool> PaymentDuplicateExistsAsync(int orderId, decimal amount, DateTime date);
}
```

- [ ] **Step 2: Write failing tests**

```csharp
// SmartSolutions.Tests/Services/PrintOrderServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Data.Entities;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class PrintOrderServiceTests
{
    private static async Task<(TestDbContextFactory factory, Customer customer, ItemName item)> SeedAsync()
    {
        var factory = TestDbContextFactory.Unique();
        await using var db = factory.CreateDbContext();
        var customer = new Customer { Name = "Test Customer" };
        var cat = new ItemCategory { Name = "Cat" };
        db.Customers.Add(customer);
        db.ItemCategories.Add(cat);
        await db.SaveChangesAsync();
        var item = new ItemName { Name = "Panaflex", CategoryId = cat.Id };
        db.ItemNames.Add(item);
        await db.SaveChangesAsync();
        return (factory, customer, item);
    }

    [Fact]
    public async Task CreateOrderAsync_DefaultStatusIsDraft()
    {
        var (factory, customer, _) = await SeedAsync();
        var svc = new PrintOrderService(factory);

        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);

        order.Status.Should().Be(PrintOrderStatus.Draft);
        order.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddLineAsync_PerSqftInFeet_ComputesTotalCorrectly()
    {
        var (factory, customer, item) = await SeedAsync();
        var svc = new PrintOrderService(factory);
        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);

        var line = await svc.AddLineAsync(order.Id, item.Id,
            RateType.PerSqft, DimensionUnit.Feet, 4m, 6m, 2, 100m);

        // 4 × 6 × 2 × 100 = 4800
        line.ComputeTotal().Should().Be(4800m);
    }

    [Fact]
    public async Task AddLineAsync_PerSqftInInches_ConvertsToFeet()
    {
        var (factory, customer, item) = await SeedAsync();
        var svc = new PrintOrderService(factory);
        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);

        var line = await svc.AddLineAsync(order.Id, item.Id,
            RateType.PerSqft, DimensionUnit.Inches, 12m, 12m, 1, 144m);

        // (12/12) × (12/12) × 1 × 144 = 1 × 1 × 144 = 144
        line.ComputeTotal().Should().Be(144m);
    }

    [Fact]
    public async Task AddLineAsync_PerPiece_IgnoresDimensions()
    {
        var (factory, customer, item) = await SeedAsync();
        var svc = new PrintOrderService(factory);
        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);

        var line = await svc.AddLineAsync(order.Id, item.Id,
            RateType.PerPiece, DimensionUnit.Feet, null, null, 500, 2m);

        // 500 × 2 = 1000
        line.ComputeTotal().Should().Be(1000m);
    }

    [Fact]
    public async Task PaymentDuplicateExistsAsync_ReturnsTrueForSameOrderAmountDate()
    {
        var (factory, customer, _) = await SeedAsync();
        await using var db = factory.CreateDbContext();
        var channel = new PaymentChannel { Name = "Cash" };
        db.PaymentChannels.Add(channel);
        await db.SaveChangesAsync();

        var svc = new PrintOrderService(factory);
        var order = await svc.CreateOrderAsync(customer.Id, DateTime.UtcNow, null);
        var date = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await svc.AddPaymentAsync(order.Id, 5000m, channel.Id, date, null);

        var isDuplicate = await svc.PaymentDuplicateExistsAsync(order.Id, 5000m, date);
        isDuplicate.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run tests to confirm failure**

```powershell
dotnet test SmartSolutions.Tests --filter "PrintOrderServiceTests" -v n
```

Expected: FAIL — `PrintOrderService` not found.

- [ ] **Step 4: Implement `PrintOrderService.cs`**

```csharp
// SmartSolutions.Core/Services/PrintOrderService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class PrintOrderService(IDbContextFactory<AppDbContext> factory) : IPrintOrderService
{
    public async Task<List<PrintOrder>> GetOrdersAsync(PrintOrderStatus? status = null,
        int? customerId = null, DateTime? from = null, DateTime? to = null,
        bool outstandingOnly = false)
    {
        await using var db = factory.CreateDbContext();
        var q = db.PrintOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .Include(o => o.Payments)
            .Include(o => o.VendorAssignments)
            .AsQueryable();
        if (status.HasValue)     q = q.Where(o => o.Status == status.Value);
        if (customerId.HasValue) q = q.Where(o => o.CustomerId == customerId.Value);
        if (from.HasValue)       q = q.Where(o => o.Date >= from.Value);
        if (to.HasValue)         q = q.Where(o => o.Date <= to.Value);
        var orders = await q.OrderByDescending(o => o.Date).ToListAsync();
        if (outstandingOnly)
            orders = orders.Where(o =>
                o.Payments.Sum(p => p.Amount) <
                o.Lines.Sum(l => l.ComputeTotal()) + (o.TransportationCharges ?? 0)).ToList();
        return orders;
    }

    public async Task<PrintOrder> GetOrderWithDetailsAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        return await db.PrintOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.ItemName).ThenInclude(n => n.Category)
            .Include(o => o.VendorAssignments).ThenInclude(a => a.Vendor)
            .Include(o => o.Payments).ThenInclude(p => p.Channel)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new InvalidOperationException($"PrintOrder {id} not found");
    }

    public async Task<PrintOrder> CreateOrderAsync(int customerId, DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new PrintOrder
        {
            CustomerId = customerId, Date = date.ToUniversalTime(),
            Status = PrintOrderStatus.Draft, Notes = notes
        };
        db.PrintOrders.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateOrderHeaderAsync(int id, int customerId, DateTime date,
        PrintOrderStatus status, decimal? transportationCharges, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"PrintOrder {id} not found");
        entity.CustomerId = customerId; entity.Date = date.ToUniversalTime();
        entity.Status = status; entity.TransportationCharges = transportationCharges; entity.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteOrderAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrders.FindAsync(id)
            ?? throw new InvalidOperationException($"PrintOrder {id} not found");
        db.PrintOrders.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<PrintOrderLine> AddLineAsync(int orderId, int itemNameId,
        RateType rateType, DimensionUnit unit, decimal? height, decimal? width,
        int quantity, decimal rate)
    {
        await using var db = factory.CreateDbContext();
        var entity = new PrintOrderLine
        {
            OrderId = orderId, ItemNameId = itemNameId, RateType = rateType, Unit = unit,
            Height = rateType == RateType.PerPiece ? null : height,
            Width  = rateType == RateType.PerPiece ? null : width,
            Quantity = quantity, Rate = rate
        };
        db.PrintOrderLines.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateLineAsync(int lineId, int itemNameId, RateType rateType,
        DimensionUnit unit, decimal? height, decimal? width, int quantity, decimal rate)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrderLines.FindAsync(lineId)
            ?? throw new InvalidOperationException($"PrintOrderLine {lineId} not found");
        entity.ItemNameId = itemNameId; entity.RateType = rateType; entity.Unit = unit;
        entity.Height = rateType == RateType.PerPiece ? null : height;
        entity.Width  = rateType == RateType.PerPiece ? null : width;
        entity.Quantity = quantity; entity.Rate = rate;
        await db.SaveChangesAsync();
    }

    public async Task DeleteLineAsync(int lineId)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrderLines.FindAsync(lineId)
            ?? throw new InvalidOperationException($"PrintOrderLine {lineId} not found");
        db.PrintOrderLines.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<PrintOrderVendorAssignment> SetVendorAssignmentAsync(int orderId,
        int vendorId, DateTime sentDate, DateTime? expectedDate, decimal vendorCost)
    {
        await using var db = factory.CreateDbContext();
        var existing = await db.PrintOrderVendorAssignments.FirstOrDefaultAsync(a => a.OrderId == orderId);
        if (existing is not null)
        {
            existing.VendorId = vendorId; existing.SentDate = sentDate.ToUniversalTime();
            existing.ExpectedDate = expectedDate?.ToUniversalTime(); existing.VendorCost = vendorCost;
            await db.SaveChangesAsync();
            return existing;
        }
        var entity = new PrintOrderVendorAssignment
        {
            OrderId = orderId, VendorId = vendorId, SentDate = sentDate.ToUniversalTime(),
            ExpectedDate = expectedDate?.ToUniversalTime(), VendorCost = vendorCost
        };
        db.PrintOrderVendorAssignments.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task MarkVendorPaidAsync(int assignmentId, DateTime paidDate)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrderVendorAssignments.FindAsync(assignmentId)
            ?? throw new InvalidOperationException($"VendorAssignment {assignmentId} not found");
        entity.VendorPaid = true; entity.VendorPaidDate = paidDate.ToUniversalTime();
        await db.SaveChangesAsync();
    }

    public async Task<PrintOrderPayment> AddPaymentAsync(int orderId, decimal amount,
        int channelId, DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new PrintOrderPayment
        {
            OrderId = orderId, Amount = amount, ChannelId = channelId,
            Date = date.ToUniversalTime(), Notes = notes
        };
        db.PrintOrderPayments.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeletePaymentAsync(int paymentId)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.PrintOrderPayments.FindAsync(paymentId)
            ?? throw new InvalidOperationException($"PrintOrderPayment {paymentId} not found");
        db.PrintOrderPayments.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<bool> PaymentDuplicateExistsAsync(int orderId, decimal amount, DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var d = date.Date;
        return await db.PrintOrderPayments.AnyAsync(p =>
            p.OrderId == orderId && p.Amount == amount && p.Date.Date == d);
    }
}
```

- [ ] **Step 5: Run tests**

```powershell
dotnet test SmartSolutions.Tests --filter "PrintOrderServiceTests" -v n
```

Expected: `Passed! - Failed: 0, Passed: 5`

- [ ] **Step 6: Commit**

```powershell
git add .
git commit -m "feat(core): add IPrintOrderService + PrintOrderService with tests"
```

---

### Task 9: IHaierJobService + HaierJobService

**Files:**
- Create: `SmartSolutions.Core/Interfaces/IHaierJobService.cs`
- Create: `SmartSolutions.Core/Services/HaierJobService.cs`
- Create: `SmartSolutions.Tests/Services/HaierJobServiceTests.cs`

- [ ] **Step 1: Create `IHaierJobService.cs`**

```csharp
// SmartSolutions.Core/Interfaces/IHaierJobService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface IHaierJobService
{
    Task<List<HaierJob>> GetJobsAsync(HaierJobStatus? status = null,
        HaierJobType? jobType = null, int? technicianId = null,
        DateTime? from = null, DateTime? to = null);
    Task<HaierJob>       GetJobWithDetailsAsync(int id);
    Task<HaierJob>       CreateJobAsync(int customerId, string acModel, string? acSerial,
        string problemDescription, int technicianId, HaierJobType jobType,
        string? claimReferenceNumber, string? partsUsed, decimal partsCost,
        DateTime date, string? notes);
    Task                 UpdateJobAsync(int id, int customerId, string acModel, string? acSerial,
        string problemDescription, int technicianId, HaierJobType jobType,
        HaierJobStatus status, string? claimReferenceNumber, string? partsUsed,
        decimal partsCost, DateTime date, string? notes);
    Task                 DeleteJobAsync(int id);

    Task<HaierJobPayment> AddPaymentAsync(int jobId, decimal amount,
        int channelId, DateTime date, string? notes);
    Task                  DeletePaymentAsync(int paymentId);
    Task<bool>            PaymentDuplicateExistsAsync(int jobId, decimal amount, DateTime date);
}
```

- [ ] **Step 2: Write failing test**

```csharp
// SmartSolutions.Tests/Services/HaierJobServiceTests.cs
using FluentAssertions;
using SmartSolutions.Core.Services;
using SmartSolutions.Data.Entities;
using SmartSolutions.Tests.Helpers;

namespace SmartSolutions.Tests.Services;

public class HaierJobServiceTests
{
    private static async Task<(TestDbContextFactory factory, Customer customer, Technician tech)> SeedAsync()
    {
        var factory = TestDbContextFactory.Unique();
        await using var db = factory.CreateDbContext();
        var customer = new Customer { Name = "Ahmed" };
        var tech = new Technician { Name = "Ali" };
        db.Customers.Add(customer);
        db.Technicians.Add(tech);
        await db.SaveChangesAsync();
        return (factory, customer, tech);
    }

    [Fact]
    public async Task CreateJobAsync_WarrantyJob_StoresClaimReference()
    {
        var (factory, customer, tech) = await SeedAsync();
        var svc = new HaierJobService(factory);

        var job = await svc.CreateJobAsync(customer.Id, "HSU-12", null,
            "No cooling", tech.Id, HaierJobType.Warranty, "CLM-2026-001",
            null, 0, DateTime.UtcNow, null);

        job.JobType.Should().Be(HaierJobType.Warranty);
        job.ClaimReferenceNumber.Should().Be("CLM-2026-001");
        job.Status.Should().Be(HaierJobStatus.Pending);
    }
}
```

- [ ] **Step 3: Run to confirm failure**

```powershell
dotnet test SmartSolutions.Tests --filter "HaierJobServiceTests" -v n
```

Expected: FAIL — `HaierJobService` not found.

- [ ] **Step 4: Implement `HaierJobService.cs`**

```csharp
// SmartSolutions.Core/Services/HaierJobService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class HaierJobService(IDbContextFactory<AppDbContext> factory) : IHaierJobService
{
    public async Task<List<HaierJob>> GetJobsAsync(HaierJobStatus? status = null,
        HaierJobType? jobType = null, int? technicianId = null,
        DateTime? from = null, DateTime? to = null)
    {
        await using var db = factory.CreateDbContext();
        var q = db.HaierJobs
            .Include(j => j.Customer)
            .Include(j => j.Technician)
            .Include(j => j.Payments)
            .AsQueryable();
        if (status.HasValue)       q = q.Where(j => j.Status == status.Value);
        if (jobType.HasValue)      q = q.Where(j => j.JobType == jobType.Value);
        if (technicianId.HasValue) q = q.Where(j => j.TechnicianId == technicianId.Value);
        if (from.HasValue)         q = q.Where(j => j.Date >= from.Value);
        if (to.HasValue)           q = q.Where(j => j.Date <= to.Value);
        return await q.OrderByDescending(j => j.Date).ToListAsync();
    }

    public async Task<HaierJob> GetJobWithDetailsAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        return await db.HaierJobs
            .Include(j => j.Customer)
            .Include(j => j.Technician)
            .Include(j => j.Payments).ThenInclude(p => p.Channel)
            .FirstOrDefaultAsync(j => j.Id == id)
            ?? throw new InvalidOperationException($"HaierJob {id} not found");
    }

    public async Task<HaierJob> CreateJobAsync(int customerId, string acModel, string? acSerial,
        string problemDescription, int technicianId, HaierJobType jobType,
        string? claimReferenceNumber, string? partsUsed, decimal partsCost,
        DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new HaierJob
        {
            CustomerId = customerId, AcModel = acModel, AcSerial = acSerial,
            ProblemDescription = problemDescription, TechnicianId = technicianId,
            JobType = jobType, Status = HaierJobStatus.Pending,
            ClaimReferenceNumber = claimReferenceNumber,
            PartsUsed = partsUsed, PartsCost = partsCost,
            Date = date.ToUniversalTime(), Notes = notes
        };
        db.HaierJobs.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateJobAsync(int id, int customerId, string acModel, string? acSerial,
        string problemDescription, int technicianId, HaierJobType jobType,
        HaierJobStatus status, string? claimReferenceNumber, string? partsUsed,
        decimal partsCost, DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.HaierJobs.FindAsync(id)
            ?? throw new InvalidOperationException($"HaierJob {id} not found");
        entity.CustomerId = customerId; entity.AcModel = acModel; entity.AcSerial = acSerial;
        entity.ProblemDescription = problemDescription; entity.TechnicianId = technicianId;
        entity.JobType = jobType; entity.Status = status;
        entity.ClaimReferenceNumber = claimReferenceNumber;
        entity.PartsUsed = partsUsed; entity.PartsCost = partsCost;
        entity.Date = date.ToUniversalTime(); entity.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteJobAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.HaierJobs.FindAsync(id)
            ?? throw new InvalidOperationException($"HaierJob {id} not found");
        db.HaierJobs.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<HaierJobPayment> AddPaymentAsync(int jobId, decimal amount,
        int channelId, DateTime date, string? notes)
    {
        await using var db = factory.CreateDbContext();
        var entity = new HaierJobPayment
        {
            JobId = jobId, Amount = amount, ChannelId = channelId,
            Date = date.ToUniversalTime(), Notes = notes
        };
        db.HaierJobPayments.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeletePaymentAsync(int paymentId)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.HaierJobPayments.FindAsync(paymentId)
            ?? throw new InvalidOperationException($"HaierJobPayment {paymentId} not found");
        db.HaierJobPayments.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<bool> PaymentDuplicateExistsAsync(int jobId, decimal amount, DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var d = date.Date;
        return await db.HaierJobPayments.AnyAsync(p =>
            p.JobId == jobId && p.Amount == amount && p.Date.Date == d);
    }
}
```

- [ ] **Step 5: Run tests**

```powershell
dotnet test SmartSolutions.Tests --filter "HaierJobServiceTests" -v n
```

Expected: `Passed! - Failed: 0, Passed: 1`

- [ ] **Step 6: Commit**

```powershell
git add .
git commit -m "feat(core): add IHaierJobService + HaierJobService with tests"
```

---

### Task 10: IExpenseService + ExpenseService

**Files:**
- Create: `SmartSolutions.Core/Interfaces/IExpenseService.cs`
- Create: `SmartSolutions.Core/Services/ExpenseService.cs`

- [ ] **Step 1: Create `IExpenseService.cs`**

```csharp
// SmartSolutions.Core/Interfaces/IExpenseService.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Interfaces;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(int? categoryId = null,
        DateTime? from = null, DateTime? to = null);
    Task<Expense>       AddExpenseAsync(int categoryId, string? description,
        decimal amount, int channelId, DateTime date);
    Task                UpdateExpenseAsync(int id, int categoryId, string? description,
        decimal amount, int channelId, DateTime date);
    Task                DeleteExpenseAsync(int id);
}
```

- [ ] **Step 2: Implement `ExpenseService.cs`**

```csharp
// SmartSolutions.Core/Services/ExpenseService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class ExpenseService(IDbContextFactory<AppDbContext> factory) : IExpenseService
{
    public async Task<List<Expense>> GetExpensesAsync(int? categoryId = null,
        DateTime? from = null, DateTime? to = null)
    {
        await using var db = factory.CreateDbContext();
        var q = db.Expenses.Include(e => e.Category).Include(e => e.Channel).AsQueryable();
        if (categoryId.HasValue) q = q.Where(e => e.CategoryId == categoryId.Value);
        if (from.HasValue)       q = q.Where(e => e.Date >= from.Value);
        if (to.HasValue)         q = q.Where(e => e.Date <= to.Value);
        return await q.OrderByDescending(e => e.Date).ToListAsync();
    }

    public async Task<Expense> AddExpenseAsync(int categoryId, string? description,
        decimal amount, int channelId, DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var entity = new Expense
        {
            CategoryId = categoryId, Description = description,
            Amount = amount, ChannelId = channelId, Date = date.ToUniversalTime()
        };
        db.Expenses.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateExpenseAsync(int id, int categoryId, string? description,
        decimal amount, int channelId, DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Expenses.FindAsync(id)
            ?? throw new InvalidOperationException($"Expense {id} not found");
        entity.CategoryId = categoryId; entity.Description = description;
        entity.Amount = amount; entity.ChannelId = channelId; entity.Date = date.ToUniversalTime();
        await db.SaveChangesAsync();
    }

    public async Task DeleteExpenseAsync(int id)
    {
        await using var db = factory.CreateDbContext();
        var entity = await db.Expenses.FindAsync(id)
            ?? throw new InvalidOperationException($"Expense {id} not found");
        db.Expenses.Remove(entity);
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 3: Build and commit**

```powershell
dotnet build SmartSolutions.sln
git add .
git commit -m "feat(core): add IExpenseService + ExpenseService"
```

---

### Task 11: IDashboardService + DashboardService

**Files:**
- Create: `SmartSolutions.Core/Interfaces/IDashboardService.cs`
- Create: `SmartSolutions.Core/Services/DashboardService.cs`

- [ ] **Step 1: Create `IDashboardService.cs`**

```csharp
// SmartSolutions.Core/Interfaces/IDashboardService.cs
namespace SmartSolutions.Core.Interfaces;

public record DayBookEntry(string Description, decimal Amount, string Channel, string Type);
public record BalanceSummary(decimal Cash, decimal Easypaisa, decimal Bank);
public record OutstandingItem(int Id, string Label, string Customer, decimal Total, decimal Paid, decimal Balance, DateTime Date);
public record MonthlySummary(decimal TotalIncome, decimal TotalExpenses, decimal Profit, BalanceSummary Balances);

public interface IDashboardService
{
    Task<List<DayBookEntry>>    GetDayBookAsync(DateTime date);
    Task<BalanceSummary>        GetBalanceSummaryAsync();
    Task<List<OutstandingItem>> GetOutstandingItemsAsync();
    Task<MonthlySummary>        GetMonthlySummaryAsync(int year, int month);
}
```

- [ ] **Step 2: Implement `DashboardService.cs`**

```csharp
// SmartSolutions.Core/Services/DashboardService.cs
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.Core.Services;

public class DashboardService(IDbContextFactory<AppDbContext> factory) : IDashboardService
{
    public async Task<List<DayBookEntry>> GetDayBookAsync(DateTime date)
    {
        await using var db = factory.CreateDbContext();
        var day = date.Date;
        var entries = new List<DayBookEntry>();

        var printPayments = await db.PrintOrderPayments
            .Include(p => p.Order).ThenInclude(o => o.Customer)
            .Include(p => p.Channel)
            .Where(p => p.Date.Date == day).ToListAsync();
        entries.AddRange(printPayments.Select(p =>
            new DayBookEntry($"Print Order #{p.OrderId} — {p.Order.Customer.Name}",
                p.Amount, p.Channel.Name, "Income")));

        var jobPayments = await db.HaierJobPayments
            .Include(p => p.Job).ThenInclude(j => j.Customer)
            .Include(p => p.Channel)
            .Where(p => p.Date.Date == day).ToListAsync();
        entries.AddRange(jobPayments.Select(p =>
            new DayBookEntry($"Haier Job #{p.JobId} — {p.Job.Customer.Name}",
                p.Amount, p.Channel.Name, "Income")));

        var expenses = await db.Expenses
            .Include(e => e.Category).Include(e => e.Channel)
            .Where(e => e.Date.Date == day).ToListAsync();
        entries.AddRange(expenses.Select(e =>
            new DayBookEntry(e.Description ?? e.Category.Name,
                e.Amount, e.Channel.Name, "Expense")));

        return entries;
    }

    public async Task<BalanceSummary> GetBalanceSummaryAsync()
    {
        await using var db = factory.CreateDbContext();
        var channels = await db.PaymentChannels.ToListAsync();

        var printByChannel = await db.PrintOrderPayments
            .GroupBy(p => p.ChannelId)
            .Select(g => new { g.Key, Total = g.Sum(p => p.Amount) }).ToListAsync();
        var jobByChannel = await db.HaierJobPayments
            .GroupBy(p => p.ChannelId)
            .Select(g => new { g.Key, Total = g.Sum(p => p.Amount) }).ToListAsync();
        var expenseByChannel = await db.Expenses
            .GroupBy(e => e.ChannelId)
            .Select(g => new { g.Key, Total = g.Sum(e => e.Amount) }).ToListAsync();

        decimal Balance(int id) =>
            (printByChannel.FirstOrDefault(x => x.Key == id)?.Total ?? 0) +
            (jobByChannel.FirstOrDefault(x => x.Key == id)?.Total ?? 0) -
            (expenseByChannel.FirstOrDefault(x => x.Key == id)?.Total ?? 0);

        var cash      = channels.FirstOrDefault(c => c.Name == "Cash");
        var easypaisa = channels.FirstOrDefault(c => c.Name == "Easypaisa");
        var bank      = channels.FirstOrDefault(c => c.Name == "Bank");

        return new BalanceSummary(
            cash      is null ? 0 : Balance(cash.Id),
            easypaisa is null ? 0 : Balance(easypaisa.Id),
            bank      is null ? 0 : Balance(bank.Id));
    }

    public async Task<List<OutstandingItem>> GetOutstandingItemsAsync()
    {
        await using var db = factory.CreateDbContext();
        var items = new List<OutstandingItem>();

        var orders = await db.PrintOrders
            .Include(o => o.Customer).Include(o => o.Lines).Include(o => o.Payments)
            .ToListAsync();
        foreach (var o in orders)
        {
            var total = o.Lines.Sum(l => l.ComputeTotal()) + (o.TransportationCharges ?? 0);
            var paid  = o.Payments.Sum(p => p.Amount);
            if (paid < total)
                items.Add(new OutstandingItem(o.Id, $"Print Order #{o.Id}",
                    o.Customer.Name, total, paid, total - paid, o.Date));
        }

        var jobs = await db.HaierJobs
            .Include(j => j.Customer).Include(j => j.Payments)
            .Where(j => j.JobType == HaierJobType.OutOfWarranty).ToListAsync();
        foreach (var j in jobs)
        {
            var paid = j.Payments.Sum(p => p.Amount);
            if (paid < j.PartsCost)
                items.Add(new OutstandingItem(j.Id, $"Haier Job #{j.Id}",
                    j.Customer.Name, j.PartsCost, paid, j.PartsCost - paid, j.Date));
        }

        return items.OrderBy(i => i.Date).ToList();
    }

    public async Task<MonthlySummary> GetMonthlySummaryAsync(int year, int month)
    {
        await using var db = factory.CreateDbContext();
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = from.AddMonths(1);

        var printIncome = await db.PrintOrderPayments
            .Where(p => p.Date >= from && p.Date < to).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var jobIncome = await db.HaierJobPayments
            .Where(p => p.Date >= from && p.Date < to).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var expenses = await db.Expenses
            .Where(e => e.Date >= from && e.Date < to).SumAsync(e => (decimal?)e.Amount) ?? 0;

        var balances = await GetBalanceSummaryAsync();
        return new MonthlySummary(printIncome + jobIncome, expenses,
            printIncome + jobIncome - expenses, balances);
    }
}
```

- [ ] **Step 3: Build and commit**

```powershell
dotnet build SmartSolutions.sln
git add .
git commit -m "feat(core): add IDashboardService + DashboardService"
```

---

## Phase 3: App Shell

### Task 12: DI host, appsettings.json, and App.xaml.cs bootstrap

**Files:**
- Create: `SmartSolutions.App/appsettings.json`
- Create: `SmartSolutions.App/ServiceConfiguration.cs`
- Modify: `SmartSolutions.App/App.xaml`
- Modify: `SmartSolutions.App/App.xaml.cs`

- [ ] **Step 1: Create `appsettings.json`**

```json
{
  "ConnectionStrings": {
    "Default": "Server=.\\SQLEXPRESS;Database=SmartSolutions;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

In `SmartSolutions.App.csproj`, ensure the file is copied to output:

```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 2: Create `ServiceConfiguration.cs`**

```csharp
// SmartSolutions.App/ServiceConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Core.Services;
using SmartSolutions.Data;

namespace SmartSolutions.App;

public static class ServiceConfiguration
{
    public static IHostBuilder ConfigureSmartSolutions(this IHostBuilder builder) =>
        builder.ConfigureServices((context, services) =>
        {
            // EF Core factory — one per request, never a shared context
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(
                    context.Configuration.GetConnectionString("Default")));

            // Services — singletons are fine; each method creates its own context
            services.AddSingleton<ILookupService,    LookupService>();
            services.AddSingleton<ICustomerService,  CustomerService>();
            services.AddSingleton<IPrintOrderService, PrintOrderService>();
            services.AddSingleton<IHaierJobService,  HaierJobService>();
            services.AddSingleton<IExpenseService,   ExpenseService>();
            services.AddSingleton<IDashboardService, DashboardService>();

            // ViewModels — transient so each navigation creates a fresh instance
            services.AddTransient<ViewModels.MainViewModel>();
            services.AddTransient<ViewModels.DashboardViewModel>();
            services.AddTransient<ViewModels.PrintOrdersViewModel>();
            services.AddTransient<ViewModels.PrintOrderDetailViewModel>();
            services.AddTransient<ViewModels.HaierJobsViewModel>();
            services.AddTransient<ViewModels.HaierJobDetailViewModel>();
            services.AddTransient<ViewModels.ExpensesViewModel>();
            services.AddTransient<ViewModels.SettingsViewModel>();

            // Main window is a singleton
            services.AddSingleton<MainWindow>();
        });
}
```

- [ ] **Step 3: Replace `App.xaml` content**

```xml
<!-- SmartSolutions.App/App.xaml -->
<Application x:Class="SmartSolutions.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <materialDesign:BundledTheme BaseTheme="Light"
                                             PrimaryColor="DeepPurple"
                                             SecondaryColor="Lime" />
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 4: Replace `App.xaml.cs` content**

```csharp
// SmartSolutions.App/App.xaml.cs
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutions.App.ViewModels;

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

        // Apply any pending migrations automatically on startup
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<SmartSolutions.Data.AppDbContext>>()
            .CreateDbContext();
        await db.Database.MigrateAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 5: Build**

```powershell
dotnet build SmartSolutions.App
```

Expected: `Build succeeded. 0 Error(s)` (ViewModels don't exist yet — expect namespace errors; stub them in the next task first if needed)

- [ ] **Step 6: Commit**

```powershell
git add .
git commit -m "feat(app): add DI host, appsettings.json, App.xaml.cs bootstrap"
```

---

### Task 13: MainWindow + MainViewModel + sidebar navigation

**Files:**
- Create: `SmartSolutions.App/ViewModels/MainViewModel.cs`
- Modify: `SmartSolutions.App/MainWindow.xaml`
- Modify: `SmartSolutions.App/MainWindow.xaml.cs`

- [ ] **Step 1: Create `ViewModels/MainViewModel.cs`**

```csharp
// SmartSolutions.App/ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutions.App.ViewModels;

public partial class MainViewModel(IServiceProvider services) : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private string _currentSection = "Dashboard";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        NavigateToDashboard();
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentSection = "Dashboard";
        CurrentView = services.GetRequiredService<DashboardViewModel>();
        ((DashboardViewModel)CurrentView!).LoadAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void NavigateToPrintOrders()
    {
        CurrentSection = "Print Orders";
        CurrentView = services.GetRequiredService<PrintOrdersViewModel>();
        ((PrintOrdersViewModel)CurrentView!).LoadAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void NavigateToHaierJobs()
    {
        CurrentSection = "Haier Jobs";
        CurrentView = services.GetRequiredService<HaierJobsViewModel>();
        ((HaierJobsViewModel)CurrentView!).LoadAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void NavigateToExpenses()
    {
        CurrentSection = "Expenses";
        CurrentView = services.GetRequiredService<ExpensesViewModel>();
        ((ExpensesViewModel)CurrentView!).LoadAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentSection = "Settings";
        CurrentView = services.GetRequiredService<SettingsViewModel>();
        ((SettingsViewModel)CurrentView!).LoadAsync().ConfigureAwait(false);
    }
}
```

- [ ] **Step 2: Replace `MainWindow.xaml`**

```xml
<!-- SmartSolutions.App/MainWindow.xaml -->
<Window x:Class="SmartSolutions.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        xmlns:vm="clr-namespace:SmartSolutions.App.ViewModels"
        xmlns:views="clr-namespace:SmartSolutions.App.Views"
        Title="Smart Solutions" Height="768" Width="1280" MinHeight="600" MinWidth="900"
        TextElement.Foreground="{DynamicResource MaterialDesignBody}"
        Background="{DynamicResource MaterialDesignPaper}"
        TextElement.FontSize="13"
        FontFamily="{md:MaterialDesignFont}">

    <Window.Resources>
        <DataTemplate DataType="{x:Type vm:DashboardViewModel}">
            <views:DashboardView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:PrintOrdersViewModel}">
            <views:PrintOrdersView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:PrintOrderDetailViewModel}">
            <views:PrintOrderDetailView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:HaierJobsViewModel}">
            <views:HaierJobsView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:HaierJobDetailViewModel}">
            <views:HaierJobDetailView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:ExpensesViewModel}">
            <views:ExpensesView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:SettingsViewModel}">
            <views:SettingsView />
        </DataTemplate>
    </Window.Resources>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="220" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- Sidebar -->
        <Border Grid.Column="0" Background="{DynamicResource PrimaryHueDarkBrush}">
            <StackPanel Margin="0,16,0,0">
                <!-- Logo / Business Name -->
                <TextBlock Text="Smart Solutions"
                           Foreground="White" FontSize="16" FontWeight="Bold"
                           Margin="16,0,0,24" />

                <Button Content="Dashboard"      Command="{Binding NavigateToDashboardCommand}"
                        Style="{StaticResource MaterialDesignFlatButton}"
                        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2" />
                <Button Content="Print Orders"   Command="{Binding NavigateToPrintOrdersCommand}"
                        Style="{StaticResource MaterialDesignFlatButton}"
                        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2" />
                <Button Content="Haier Jobs"     Command="{Binding NavigateToHaierJobsCommand}"
                        Style="{StaticResource MaterialDesignFlatButton}"
                        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2" />
                <Button Content="Expenses"       Command="{Binding NavigateToExpensesCommand}"
                        Style="{StaticResource MaterialDesignFlatButton}"
                        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2" />

                <Separator Background="White" Opacity="0.3" Margin="16,16" />

                <Button Content="Settings"       Command="{Binding NavigateToSettingsCommand}"
                        Style="{StaticResource MaterialDesignFlatButton}"
                        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2" />
            </StackPanel>
        </Border>

        <!-- Content area — DataTemplate dispatch is automatic via Window.Resources -->
        <ContentControl Grid.Column="1"
                        Content="{Binding CurrentView}"
                        Margin="0" />
    </Grid>
</Window>
```

- [ ] **Step 3: Replace `MainWindow.xaml.cs`**

```csharp
// SmartSolutions.App/MainWindow.xaml.cs
using System.Windows;

namespace SmartSolutions.App;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
}
```

- [ ] **Step 4: Build**

```powershell
dotnet build SmartSolutions.App
```

Expected: Errors about missing ViewModels (DashboardViewModel etc.) — that is expected. Stub them in Step 5.

- [ ] **Step 5: Create empty stub ViewModels so the project builds**

Create these files — each a minimal `ObservableObject` subclass with a no-op `LoadAsync`:

```csharp
// SmartSolutions.App/ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
namespace SmartSolutions.App.ViewModels;
public partial class DashboardViewModel : ObservableObject
{
    public Task LoadAsync() => Task.CompletedTask;
}
```

```csharp
// SmartSolutions.App/ViewModels/PrintOrdersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
namespace SmartSolutions.App.ViewModels;
public partial class PrintOrdersViewModel : ObservableObject
{
    public Task LoadAsync() => Task.CompletedTask;
}
```

```csharp
// SmartSolutions.App/ViewModels/PrintOrderDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
namespace SmartSolutions.App.ViewModels;
public partial class PrintOrderDetailViewModel : ObservableObject
{
    public Task LoadAsync() => Task.CompletedTask;
}
```

```csharp
// SmartSolutions.App/ViewModels/HaierJobsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
namespace SmartSolutions.App.ViewModels;
public partial class HaierJobsViewModel : ObservableObject
{
    public Task LoadAsync() => Task.CompletedTask;
}
```

```csharp
// SmartSolutions.App/ViewModels/HaierJobDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
namespace SmartSolutions.App.ViewModels;
public partial class HaierJobDetailViewModel : ObservableObject
{
    public Task LoadAsync() => Task.CompletedTask;
}
```

```csharp
// SmartSolutions.App/ViewModels/ExpensesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
namespace SmartSolutions.App.ViewModels;
public partial class ExpensesViewModel : ObservableObject
{
    public Task LoadAsync() => Task.CompletedTask;
}
```

```csharp
// SmartSolutions.App/ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
namespace SmartSolutions.App.ViewModels;
public partial class SettingsViewModel : ObservableObject
{
    public Task LoadAsync() => Task.CompletedTask;
}
```

Create matching stub Views (each is just an empty UserControl):

```xml
<!-- SmartSolutions.App/Views/DashboardView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <TextBlock Text="Dashboard" Margin="24" FontSize="24" />
</UserControl>
```

Create the same stub pattern for `PrintOrdersView.xaml`, `PrintOrderDetailView.xaml`, `HaierJobsView.xaml`, `HaierJobDetailView.xaml`, `ExpensesView.xaml`, `SettingsView.xaml` — each with its own `x:Class` and a placeholder `TextBlock`.

Each view's code-behind is just:
```csharp
// SmartSolutions.App/Views/DashboardView.xaml.cs
using System.Windows.Controls;
namespace SmartSolutions.App.Views;
public partial class DashboardView : UserControl { public DashboardView() => InitializeComponent(); }
```

- [ ] **Step 6: Build clean**

```powershell
dotnet build SmartSolutions.App
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```powershell
git add .
git commit -m "feat(app): add MainWindow with sidebar nav, stub ViewModels and Views"
```

---

### Task 14: Value converters

**Files:**
- Create: `SmartSolutions.App/Converters/BoolToVisibilityConverter.cs`
- Create: `SmartSolutions.App/Converters/InverseBoolToVisibilityConverter.cs`
- Create: `SmartSolutions.App/Converters/RateTypeToVisibilityConverter.cs`
- Create: `SmartSolutions.App/Converters/CurrencyConverter.cs`

- [ ] **Step 1: Create `BoolToVisibilityConverter.cs`**

```csharp
// SmartSolutions.App/Converters/BoolToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartSolutions.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}
```

- [ ] **Step 2: Create `InverseBoolToVisibilityConverter.cs`**

```csharp
// SmartSolutions.App/Converters/InverseBoolToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartSolutions.App.Converters;

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not Visibility.Visible;
}
```

- [ ] **Step 3: Create `RateTypeToVisibilityConverter.cs`**

```csharp
// SmartSolutions.App/Converters/RateTypeToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.App.Converters;

// Returns Visible when RateType == PerSqft (so dimension fields show)
public class RateTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is RateType.PerSqft ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

- [ ] **Step 4: Create `CurrencyConverter.cs`**

```csharp
// SmartSolutions.App/Converters/CurrencyConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace SmartSolutions.App.Converters;

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is decimal d ? $"PKR {d:#,##0.00}" : "PKR 0.00";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

- [ ] **Step 5: Register converters in `App.xaml`**

Add inside the `<ResourceDictionary>` in `App.xaml`:

```xml
<converters:BoolToVisibilityConverter        x:Key="BoolToVisibility" />
<converters:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibility" />
<converters:RateTypeToVisibilityConverter    x:Key="RateTypeToVisibility" />
<converters:CurrencyConverter                x:Key="CurrencyConverter" />
```

And add the namespace at the top of `App.xaml`:

```xml
xmlns:converters="clr-namespace:SmartSolutions.App.Converters"
```

- [ ] **Step 6: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add .
git commit -m "feat(app): add value converters — BoolToVisibility, RateType, Currency"
```

---

## Phase 4: Settings Module

### Task 15: SettingsViewModel

**Files:**
- Modify: `SmartSolutions.App/ViewModels/SettingsViewModel.cs` (replace stub)

- [ ] **Step 1: Replace `SettingsViewModel.cs` with full implementation**

```csharp
// SmartSolutions.App/ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class SettingsViewModel(ILookupService lookup) : ObservableObject
{
    // ── Item Categories ──────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ItemCategory> _itemCategories = [];
    [ObservableProperty] private ItemCategory? _selectedItemCategory;
    [ObservableProperty] private string _newCategoryName = "";
    [ObservableProperty] private string _renameCategoryName = "";

    // ── Item Names ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ItemName> _itemNames = [];
    [ObservableProperty] private ItemName? _selectedItemName;
    [ObservableProperty] private string _newItemName = "";
    [ObservableProperty] private string _renameItemName = "";

    // ── Vendors ──────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Vendor> _vendors = [];
    [ObservableProperty] private Vendor? _selectedVendor;
    [ObservableProperty] private string _vendorName = "";
    [ObservableProperty] private string _vendorPhone = "";
    [ObservableProperty] private string _vendorNotes = "";

    // ── Technicians ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Technician> _technicians = [];
    [ObservableProperty] private Technician? _selectedTechnician;
    [ObservableProperty] private string _technicianName = "";
    [ObservableProperty] private string _technicianPhone = "";

    // ── Expense Categories ───────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ExpenseCategory> _expenseCategories = [];
    [ObservableProperty] private ExpenseCategory? _selectedExpenseCategory;
    [ObservableProperty] private string _newExpenseCategoryName = "";

    // ── Payment Channels ─────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<PaymentChannel> _paymentChannels = [];
    [ObservableProperty] private PaymentChannel? _selectedPaymentChannel;
    [ObservableProperty] private string _newChannelName = "";

    // ── Business Info ─────────────────────────────────────────────────────
    [ObservableProperty] private BusinessInfo _businessInfo = new();
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    public async Task LoadAsync()
    {
        IsBusy = true;
        ItemCategories   = new(await lookup.GetItemCategoriesAsync());
        Vendors          = new(await lookup.GetVendorsAsync());
        Technicians      = new(await lookup.GetTechniciansAsync());
        ExpenseCategories = new(await lookup.GetExpenseCategoriesAsync());
        PaymentChannels  = new(await lookup.GetPaymentChannelsAsync());
        BusinessInfo     = await lookup.GetBusinessInfoAsync();
        IsBusy = false;
    }

    partial void OnSelectedItemCategoryChanged(ItemCategory? value)
    {
        if (value is not null) LoadItemNamesForCategoryAsync(value.Id).ConfigureAwait(false);
        RenameCategoryName = value?.Name ?? "";
    }

    private async Task LoadItemNamesForCategoryAsync(int categoryId)
    {
        ItemNames = new(await lookup.GetItemNamesAsync(categoryId));
    }

    // ── Item Category commands ────────────────────────────────────────────

    [RelayCommand]
    private async Task AddItemCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName)) return;
        var added = await lookup.AddItemCategoryAsync(NewCategoryName.Trim());
        ItemCategories.Add(added);
        NewCategoryName = "";
    }

    [RelayCommand]
    private async Task RenameItemCategoryAsync()
    {
        if (SelectedItemCategory is null || string.IsNullOrWhiteSpace(RenameCategoryName)) return;
        await lookup.RenameItemCategoryAsync(SelectedItemCategory.Id, RenameCategoryName.Trim());
        SelectedItemCategory.Name = RenameCategoryName.Trim();
        // Force list refresh
        var idx = ItemCategories.IndexOf(SelectedItemCategory);
        ItemCategories.RemoveAt(idx);
        ItemCategories.Insert(idx, SelectedItemCategory);
    }

    [RelayCommand]
    private async Task DeleteItemCategoryAsync()
    {
        if (SelectedItemCategory is null) return;
        await lookup.DeleteItemCategoryAsync(SelectedItemCategory.Id);
        ItemCategories.Remove(SelectedItemCategory);
        SelectedItemCategory = null;
        ItemNames.Clear();
    }

    // ── Item Name commands ────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddItemNameAsync()
    {
        if (SelectedItemCategory is null || string.IsNullOrWhiteSpace(NewItemName)) return;
        var added = await lookup.AddItemNameAsync(NewItemName.Trim(), SelectedItemCategory.Id);
        ItemNames.Add(added);
        NewItemName = "";
    }

    [RelayCommand]
    private async Task DeleteItemNameAsync()
    {
        if (SelectedItemName is null) return;
        await lookup.DeleteItemNameAsync(SelectedItemName.Id);
        ItemNames.Remove(SelectedItemName);
        SelectedItemName = null;
    }

    // ── Vendor commands ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveVendorAsync()
    {
        if (string.IsNullOrWhiteSpace(VendorName)) return;
        if (SelectedVendor is null)
        {
            var added = await lookup.AddVendorAsync(VendorName.Trim(),
                NullIfEmpty(VendorPhone), NullIfEmpty(VendorNotes));
            Vendors.Add(added);
        }
        else
        {
            await lookup.UpdateVendorAsync(SelectedVendor.Id, VendorName.Trim(),
                NullIfEmpty(VendorPhone), NullIfEmpty(VendorNotes));
            SelectedVendor.Name = VendorName.Trim();
        }
        ClearVendorForm();
    }

    [RelayCommand]
    private async Task DeleteVendorAsync()
    {
        if (SelectedVendor is null) return;
        await lookup.DeleteVendorAsync(SelectedVendor.Id);
        Vendors.Remove(SelectedVendor);
        SelectedVendor = null;
        ClearVendorForm();
    }

    partial void OnSelectedVendorChanged(Vendor? value)
    {
        VendorName  = value?.Name  ?? "";
        VendorPhone = value?.Phone ?? "";
        VendorNotes = value?.Notes ?? "";
    }

    private void ClearVendorForm()
    {
        SelectedVendor = null;
        VendorName = VendorPhone = VendorNotes = "";
    }

    // ── Technician commands ───────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveTechnicianAsync()
    {
        if (string.IsNullOrWhiteSpace(TechnicianName)) return;
        if (SelectedTechnician is null)
        {
            var added = await lookup.AddTechnicianAsync(TechnicianName.Trim(), NullIfEmpty(TechnicianPhone));
            Technicians.Add(added);
        }
        else
        {
            await lookup.UpdateTechnicianAsync(SelectedTechnician.Id,
                TechnicianName.Trim(), NullIfEmpty(TechnicianPhone));
            SelectedTechnician.Name = TechnicianName.Trim();
        }
        ClearTechnicianForm();
    }

    [RelayCommand]
    private async Task DeleteTechnicianAsync()
    {
        if (SelectedTechnician is null) return;
        await lookup.DeleteTechnicianAsync(SelectedTechnician.Id);
        Technicians.Remove(SelectedTechnician);
        SelectedTechnician = null;
        ClearTechnicianForm();
    }

    partial void OnSelectedTechnicianChanged(Technician? value)
    {
        TechnicianName  = value?.Name  ?? "";
        TechnicianPhone = value?.Phone ?? "";
    }

    private void ClearTechnicianForm() { SelectedTechnician = null; TechnicianName = TechnicianPhone = ""; }

    // ── Expense Category commands ─────────────────────────────────────────

    [RelayCommand]
    private async Task AddExpenseCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExpenseCategoryName)) return;
        var added = await lookup.AddExpenseCategoryAsync(NewExpenseCategoryName.Trim());
        ExpenseCategories.Add(added);
        NewExpenseCategoryName = "";
    }

    [RelayCommand]
    private async Task DeleteExpenseCategoryAsync()
    {
        if (SelectedExpenseCategory is null) return;
        await lookup.DeleteExpenseCategoryAsync(SelectedExpenseCategory.Id);
        ExpenseCategories.Remove(SelectedExpenseCategory);
        SelectedExpenseCategory = null;
    }

    // ── Payment Channel commands ──────────────────────────────────────────

    [RelayCommand]
    private async Task AddPaymentChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(NewChannelName)) return;
        var added = await lookup.AddPaymentChannelAsync(NewChannelName.Trim());
        PaymentChannels.Add(added);
        NewChannelName = "";
    }

    [RelayCommand]
    private async Task DeletePaymentChannelAsync()
    {
        if (SelectedPaymentChannel is null) return;
        await lookup.DeletePaymentChannelAsync(SelectedPaymentChannel.Id);
        PaymentChannels.Remove(SelectedPaymentChannel);
        SelectedPaymentChannel = null;
    }

    // ── Business Info command ─────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveBusinessInfoAsync()
    {
        await lookup.SaveBusinessInfoAsync(BusinessInfo);
        StatusMessage = "Business info saved.";
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build SmartSolutions.App
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```powershell
git add SmartSolutions.App/ViewModels/SettingsViewModel.cs
git commit -m "feat(app): implement SettingsViewModel — all lookup CRUD + business info"
```

---

### Task 16: SettingsView.xaml

**Files:**
- Modify: `SmartSolutions.App/Views/SettingsView.xaml` (replace stub)
- Create: `SmartSolutions.App/Views/SettingsView.xaml.cs`

- [ ] **Step 1: Replace `SettingsView.xaml`**

```xml
<!-- SmartSolutions.App/Views/SettingsView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.SettingsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
      <StackPanel Margin="24" MaxWidth="960">

        <TextBlock Text="Settings" Style="{StaticResource MaterialDesignHeadline5TextBlock}" Margin="0,0,0,24"/>

        <!-- Item Categories + Item Names -->
        <md:Card Margin="0,0,0,16" Padding="16">
          <Grid>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*"/>
              <ColumnDefinition Width="16"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Categories column -->
            <StackPanel Grid.Column="0">
              <TextBlock Text="Item Categories" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
              <ListBox ItemsSource="{Binding ItemCategories}"
                       SelectedItem="{Binding SelectedItemCategory}"
                       DisplayMemberPath="Name" Height="180"/>
              <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                <TextBox md:HintAssist.Hint="New category name"
                         Text="{Binding NewCategoryName, UpdateSourceTrigger=PropertyChanged}"
                         Width="160" Margin="0,0,8,0"/>
                <Button Content="Add" Command="{Binding AddItemCategoryCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"/>
              </StackPanel>
              <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                <TextBox md:HintAssist.Hint="Rename selected"
                         Text="{Binding RenameCategoryName, UpdateSourceTrigger=PropertyChanged}"
                         Width="160" Margin="0,0,8,0"/>
                <Button Content="Rename" Command="{Binding RenameItemCategoryCommand}" Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>
                <Button Content="Delete" Command="{Binding DeleteItemCategoryCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                        Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
              </StackPanel>
            </StackPanel>

            <!-- Item Names column -->
            <StackPanel Grid.Column="2">
              <TextBlock Text="Item Names (for selected category)" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
              <ListBox ItemsSource="{Binding ItemNames}"
                       SelectedItem="{Binding SelectedItemName}"
                       DisplayMemberPath="Name" Height="180"/>
              <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                <TextBox md:HintAssist.Hint="New item name"
                         Text="{Binding NewItemName, UpdateSourceTrigger=PropertyChanged}"
                         Width="160" Margin="0,0,8,0"/>
                <Button Content="Add"    Command="{Binding AddItemNameCommand}"    Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>
                <Button Content="Delete" Command="{Binding DeleteItemNameCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                        Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
              </StackPanel>
            </StackPanel>
          </Grid>
        </md:Card>

        <!-- Vendors -->
        <md:Card Margin="0,0,0,16" Padding="16">
          <Grid>
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="16"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
            <ListBox Grid.Column="0" ItemsSource="{Binding Vendors}" SelectedItem="{Binding SelectedVendor}" DisplayMemberPath="Name" Height="160"/>
            <StackPanel Grid.Column="2">
              <TextBlock Text="Vendor" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Name *" Text="{Binding VendorName, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Phone"  Text="{Binding VendorPhone, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Notes"  Text="{Binding VendorNotes, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <StackPanel Orientation="Horizontal">
                <Button Content="Save"   Command="{Binding SaveVendorCommand}"   Style="{StaticResource MaterialDesignRaisedButton}" Margin="0,0,8,0"/>
                <Button Content="Delete" Command="{Binding DeleteVendorCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                        Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
              </StackPanel>
            </StackPanel>
          </Grid>
        </md:Card>

        <!-- Technicians -->
        <md:Card Margin="0,0,0,16" Padding="16">
          <Grid>
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="16"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
            <ListBox Grid.Column="0" ItemsSource="{Binding Technicians}" SelectedItem="{Binding SelectedTechnician}" DisplayMemberPath="Name" Height="120"/>
            <StackPanel Grid.Column="2">
              <TextBlock Text="Technician" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Name *"  Text="{Binding TechnicianName,  UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Phone"   Text="{Binding TechnicianPhone, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <StackPanel Orientation="Horizontal">
                <Button Content="Save"   Command="{Binding SaveTechnicianCommand}"   Style="{StaticResource MaterialDesignRaisedButton}" Margin="0,0,8,0"/>
                <Button Content="Delete" Command="{Binding DeleteTechnicianCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                        Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
              </StackPanel>
            </StackPanel>
          </Grid>
        </md:Card>

        <!-- Expense Categories + Payment Channels side by side -->
        <Grid Margin="0,0,0,16">
          <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="16"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
          <md:Card Grid.Column="0" Padding="16">
            <StackPanel>
              <TextBlock Text="Expense Categories" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
              <ListBox ItemsSource="{Binding ExpenseCategories}" SelectedItem="{Binding SelectedExpenseCategory}" DisplayMemberPath="Name" Height="120"/>
              <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                <TextBox md:HintAssist.Hint="New name" Text="{Binding NewExpenseCategoryName, UpdateSourceTrigger=PropertyChanged}" Width="140" Margin="0,0,8,0"/>
                <Button Content="Add"    Command="{Binding AddExpenseCategoryCommand}"    Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>
                <Button Content="Delete" Command="{Binding DeleteExpenseCategoryCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                        Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
              </StackPanel>
            </StackPanel>
          </md:Card>
          <md:Card Grid.Column="2" Padding="16">
            <StackPanel>
              <TextBlock Text="Payment Channels" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
              <ListBox ItemsSource="{Binding PaymentChannels}" SelectedItem="{Binding SelectedPaymentChannel}" DisplayMemberPath="Name" Height="120"/>
              <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                <TextBox md:HintAssist.Hint="New channel" Text="{Binding NewChannelName, UpdateSourceTrigger=PropertyChanged}" Width="140" Margin="0,0,8,0"/>
                <Button Content="Add"    Command="{Binding AddPaymentChannelCommand}"    Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>
                <Button Content="Delete" Command="{Binding DeletePaymentChannelCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"
                        Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
              </StackPanel>
            </StackPanel>
          </md:Card>
        </Grid>

        <!-- Business Info -->
        <md:Card Padding="16">
          <StackPanel>
            <TextBlock Text="Business Information" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <Grid>
              <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="16"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
              <StackPanel Grid.Column="0">
                <TextBox md:HintAssist.Hint="Business Name" Text="{Binding BusinessInfo.Name, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="NTN"          Text="{Binding BusinessInfo.Ntn,  UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Address"      Text="{Binding BusinessInfo.Address, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              </StackPanel>
              <StackPanel Grid.Column="2">
                <TextBox md:HintAssist.Hint="Phone 1"  Text="{Binding BusinessInfo.Phone1, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Phone 2"  Text="{Binding BusinessInfo.Phone2, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
                <TextBox md:HintAssist.Hint="Email"    Text="{Binding BusinessInfo.Email,  UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              </StackPanel>
            </Grid>
            <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
              <Button Content="Save Business Info" Command="{Binding SaveBusinessInfoCommand}"
                      Style="{StaticResource MaterialDesignRaisedButton}" Margin="0,0,16,0"/>
              <TextBlock Text="{Binding StatusMessage}" VerticalAlignment="Center" Opacity="0.7"/>
            </StackPanel>
          </StackPanel>
        </md:Card>

      </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 2: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add SmartSolutions.App/Views/SettingsView.xaml SmartSolutions.App/Views/SettingsView.xaml.cs
git commit -m "feat(app): implement SettingsView — all lookup CRUD + business info UI"
```

---

## Phase 5: Print Orders Module

### Task 17: PrintOrdersViewModel (list with filters)

**Files:**
- Modify: `SmartSolutions.App/ViewModels/PrintOrdersViewModel.cs` (replace stub)

- [ ] **Step 1: Replace `PrintOrdersViewModel.cs`**

```csharp
// SmartSolutions.App/ViewModels/PrintOrdersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class PrintOrdersViewModel(
    IPrintOrderService orders,
    IServiceProvider services) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<PrintOrder> _orderList = [];
    [ObservableProperty] private PrintOrder? _selectedOrder;
    [ObservableProperty] private PrintOrderStatus? _filterStatus;
    [ObservableProperty] private bool _filterOutstandingOnly;
    [ObservableProperty] private DateTime? _filterFrom;
    [ObservableProperty] private DateTime? _filterTo;
    [ObservableProperty] private bool _isBusy;

    public async Task LoadAsync()
    {
        IsBusy = true;
        var results = await orders.GetOrdersAsync(
            FilterStatus, null, FilterFrom, FilterTo, FilterOutstandingOnly);
        OrderList = new(results);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync() => await LoadAsync();

    [RelayCommand]
    private void ClearFilters()
    {
        FilterStatus = null;
        FilterOutstandingOnly = false;
        FilterFrom = FilterTo = null;
    }

    [RelayCommand]
    private void OpenNewOrder()
    {
        var vm = services.GetRequiredService<PrintOrderDetailViewModel>();
        vm.InitNew();
        NavigateTo(vm);
    }

    [RelayCommand]
    private void OpenOrder(PrintOrder order)
    {
        var vm = services.GetRequiredService<PrintOrderDetailViewModel>();
        vm.InitEdit(order.Id);
        NavigateTo(vm);
    }

    [RelayCommand]
    private async Task DeleteOrderAsync(PrintOrder order)
    {
        await orders.DeleteOrderAsync(order.Id);
        OrderList.Remove(order);
    }

    private void NavigateTo(ObservableObject vm)
    {
        // Navigate via MainViewModel in parent scope
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Print Orders";
        main.CurrentView = vm;
    }
}
```

- [ ] **Step 2: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add SmartSolutions.App/ViewModels/PrintOrdersViewModel.cs
git commit -m "feat(app): implement PrintOrdersViewModel with filters and navigation"
```

---

### Task 18: PrintOrderDetailViewModel

**Files:**
- Modify: `SmartSolutions.App/ViewModels/PrintOrderDetailViewModel.cs` (replace stub)

- [ ] **Step 1: Replace `PrintOrderDetailViewModel.cs`**

```csharp
// SmartSolutions.App/ViewModels/PrintOrderDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class PrintOrderDetailViewModel(
    IPrintOrderService orders,
    ICustomerService customers,
    ILookupService lookup,
    IServiceProvider services) : ObservableObject
{
    // ── Order Header ─────────────────────────────────────────────────────
    [ObservableProperty] private int _orderId;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearch = "";
    [ObservableProperty] private ObservableCollection<Customer> _customerSuggestions = [];
    [ObservableProperty] private DateTime _orderDate = DateTime.Today;
    [ObservableProperty] private PrintOrderStatus _orderStatus = PrintOrderStatus.Draft;
    [ObservableProperty] private decimal? _transportationCharges;
    [ObservableProperty] private string _orderNotes = "";

    // ── Line Items ────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<PrintOrderLine> _lines = [];
    [ObservableProperty] private ObservableCollection<ItemCategory> _itemCategories = [];
    [ObservableProperty] private ObservableCollection<ItemName> _itemNames = [];

    // Staging area for the new line being entered
    [ObservableProperty] private ItemCategory? _newLineCategory;
    [ObservableProperty] private ItemName? _newLineItemName;
    [ObservableProperty] private RateType _newLineRateType = RateType.PerSqft;
    [ObservableProperty] private DimensionUnit _newLineUnit = DimensionUnit.Feet;
    [ObservableProperty] private decimal? _newLineHeight;
    [ObservableProperty] private decimal? _newLineWidth;
    [ObservableProperty] private int _newLineQuantity = 1;
    [ObservableProperty] private decimal _newLineRate;
    [ObservableProperty] private decimal _newLineComputedTotal;
    [ObservableProperty] private bool _newLineDimensionsVisible = true;

    // ── Vendor Assignment ─────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Vendor> _vendors = [];
    [ObservableProperty] private Vendor? _selectedVendor;
    [ObservableProperty] private DateTime _vendorSentDate = DateTime.Today;
    [ObservableProperty] private DateTime? _vendorExpectedDate;
    [ObservableProperty] private decimal _vendorCost;
    [ObservableProperty] private PrintOrderVendorAssignment? _vendorAssignment;

    // ── Payments ──────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<PrintOrderPayment> _payments = [];
    [ObservableProperty] private ObservableCollection<PaymentChannel> _paymentChannels = [];
    [ObservableProperty] private decimal _newPaymentAmount;
    [ObservableProperty] private PaymentChannel? _newPaymentChannel;
    [ObservableProperty] private DateTime _newPaymentDate = DateTime.Today;
    [ObservableProperty] private string _newPaymentNotes = "";
    [ObservableProperty] private string _duplicatePaymentWarning = "";

    // ── Summary ───────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _orderTotal;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isBusy;

    public void InitNew()
    {
        IsNew = true;
        OrderId = 0;
        OrderDate = DateTime.Today;
        OrderStatus = PrintOrderStatus.Draft;
        LoadLookupsAsync().ConfigureAwait(false);
    }

    public void InitEdit(int orderId)
    {
        IsNew = false;
        OrderId = orderId;
        LoadAsync().ConfigureAwait(false);
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        await LoadLookupsAsync();
        if (OrderId > 0)
        {
            var order = await orders.GetOrderWithDetailsAsync(OrderId);
            SelectedCustomer = order.Customer;
            CustomerSearch = order.Customer.Name;
            OrderDate = order.Date.ToLocalTime();
            OrderStatus = order.Status;
            TransportationCharges = order.TransportationCharges;
            OrderNotes = order.Notes ?? "";
            Lines = new(order.Lines);
            Payments = new(order.Payments);
            VendorAssignment = order.VendorAssignments.FirstOrDefault();
            if (VendorAssignment is not null)
            {
                SelectedVendor = VendorAssignment.Vendor;
                VendorSentDate = VendorAssignment.SentDate.ToLocalTime();
                VendorExpectedDate = VendorAssignment.ExpectedDate?.ToLocalTime();
                VendorCost = VendorAssignment.VendorCost;
            }
            RefreshSummary();
        }
        IsBusy = false;
    }

    private async Task LoadLookupsAsync()
    {
        ItemCategories  = new(await lookup.GetItemCategoriesAsync());
        Vendors         = new(await lookup.GetVendorsAsync());
        PaymentChannels = new(await lookup.GetPaymentChannelsAsync());
    }

    // ── Customer search ───────────────────────────────────────────────────

    partial void OnCustomerSearchChanged(string value)
    {
        SearchCustomersAsync(value).ConfigureAwait(false);
    }

    private async Task SearchCustomersAsync(string query)
    {
        if (query.Length < 2) { CustomerSuggestions.Clear(); return; }
        var results = await customers.SearchCustomersAsync(query);
        CustomerSuggestions = new(results);
    }

    [RelayCommand]
    private void SelectCustomer(Customer customer)
    {
        SelectedCustomer = customer;
        CustomerSearch = customer.Name;
        CustomerSuggestions.Clear();
    }

    [RelayCommand]
    private async Task CreateCustomerInlineAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerSearch)) return;
        var newCustomer = await customers.AddCustomerAsync(CustomerSearch.Trim(), null, null, null);
        SelectedCustomer = newCustomer;
        CustomerSuggestions.Clear();
    }

    // ── New line entry ────────────────────────────────────────────────────

    partial void OnNewLineCategoryChanged(ItemCategory? value)
    {
        if (value is not null) LoadItemNamesForCategoryAsync(value.Id).ConfigureAwait(false);
    }

    private async Task LoadItemNamesForCategoryAsync(int categoryId)
    {
        ItemNames = new(await lookup.GetItemNamesAsync(categoryId));
        NewLineItemName = null;
    }

    partial void OnNewLineRateTypeChanged(RateType value)
    {
        NewLineDimensionsVisible = value == RateType.PerSqft;
        if (value == RateType.PerPiece) { NewLineHeight = NewLineWidth = null; }
        RecalcNewLineTotal();
    }

    partial void OnNewLineHeightChanged(decimal? value)   => RecalcNewLineTotal();
    partial void OnNewLineWidthChanged(decimal? value)    => RecalcNewLineTotal();
    partial void OnNewLineQuantityChanged(int value)      => RecalcNewLineTotal();
    partial void OnNewLineRateChanged(decimal value)      => RecalcNewLineTotal();
    partial void OnNewLineUnitChanged(DimensionUnit value) => RecalcNewLineTotal();

    private void RecalcNewLineTotal()
    {
        var stub = new PrintOrderLine
        {
            RateType = NewLineRateType, Unit = NewLineUnit,
            Height = NewLineHeight, Width = NewLineWidth,
            Quantity = NewLineQuantity, Rate = NewLineRate
        };
        NewLineComputedTotal = stub.ComputeTotal();
    }

    [RelayCommand]
    private async Task AddLineAsync()
    {
        if (NewLineItemName is null || NewLineQuantity <= 0 || NewLineRate <= 0)
        {
            ErrorMessage = "Item name, quantity > 0, and rate > 0 are required.";
            return;
        }
        if (NewLineRateType == RateType.PerSqft && (NewLineHeight is null or <= 0 || NewLineWidth is null or <= 0))
        {
            ErrorMessage = "Height and width must be > 0 for sqft rate type.";
            return;
        }
        ErrorMessage = "";

        if (OrderId == 0)
        {
            // Auto-save order header first
            if (SelectedCustomer is null) { ErrorMessage = "Select a customer first."; return; }
            var newOrder = await orders.CreateOrderAsync(
                SelectedCustomer.Id, OrderDate.ToUniversalTime(), OrderNotes);
            OrderId = newOrder.Id;
            IsNew = false;
        }

        var line = await orders.AddLineAsync(OrderId, NewLineItemName.Id, NewLineRateType,
            NewLineUnit, NewLineHeight, NewLineWidth, NewLineQuantity, NewLineRate);
        line.ItemName = NewLineItemName;
        Lines.Add(line);
        ClearNewLineForm();
        RefreshSummary();
    }

    [RelayCommand]
    private async Task DeleteLineAsync(PrintOrderLine line)
    {
        await orders.DeleteLineAsync(line.Id);
        Lines.Remove(line);
        RefreshSummary();
    }

    private void ClearNewLineForm()
    {
        NewLineItemName = null; NewLineHeight = NewLineWidth = null;
        NewLineQuantity = 1; NewLineRate = 0; NewLineComputedTotal = 0;
    }

    // ── Vendor assignment ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveVendorAssignmentAsync()
    {
        if (SelectedVendor is null || OrderId == 0) return;
        VendorAssignment = await orders.SetVendorAssignmentAsync(OrderId, SelectedVendor.Id,
            VendorSentDate.ToUniversalTime(), VendorExpectedDate?.ToUniversalTime(), VendorCost);
    }

    [RelayCommand]
    private async Task MarkVendorPaidAsync()
    {
        if (VendorAssignment is null) return;
        await orders.MarkVendorPaidAsync(VendorAssignment.Id, DateTime.UtcNow);
        VendorAssignment.VendorPaid = true;
        OnPropertyChanged(nameof(VendorAssignment));
    }

    // ── Payments ──────────────────────────────────────────────────────────

    partial void OnNewPaymentAmountChanged(decimal value)
    {
        CheckDuplicateAsync().ConfigureAwait(false);
    }

    private async Task CheckDuplicateAsync()
    {
        if (OrderId == 0 || NewPaymentAmount <= 0) { DuplicatePaymentWarning = ""; return; }
        var isDuplicate = await orders.PaymentDuplicateExistsAsync(
            OrderId, NewPaymentAmount, NewPaymentDate);
        DuplicatePaymentWarning = isDuplicate
            ? "Warning: a payment with this amount already exists for this date."
            : "";
    }

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        if (NewPaymentAmount <= 0)    { ErrorMessage = "Payment amount must be > 0."; return; }
        if (NewPaymentChannel is null) { ErrorMessage = "Select a payment channel.";  return; }
        if (OrderId == 0)              { ErrorMessage = "Save the order header first."; return; }
        ErrorMessage = "";

        var payment = await orders.AddPaymentAsync(OrderId, NewPaymentAmount,
            NewPaymentChannel.Id, NewPaymentDate.ToUniversalTime(), NewPaymentNotes);
        payment.Channel = NewPaymentChannel;
        Payments.Add(payment);
        NewPaymentAmount = 0; NewPaymentNotes = "";
        DuplicatePaymentWarning = "";
        RefreshSummary();
    }

    [RelayCommand]
    private async Task DeletePaymentAsync(PrintOrderPayment payment)
    {
        await orders.DeletePaymentAsync(payment.Id);
        Payments.Remove(payment);
        RefreshSummary();
    }

    // ── Save order header ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveOrderAsync()
    {
        if (SelectedCustomer is null) { ErrorMessage = "Customer is required."; return; }
        ErrorMessage = "";
        if (OrderId == 0)
        {
            var newOrder = await orders.CreateOrderAsync(
                SelectedCustomer.Id, OrderDate.ToUniversalTime(), OrderNotes);
            OrderId = newOrder.Id;
            IsNew = false;
        }
        else
        {
            await orders.UpdateOrderHeaderAsync(OrderId, SelectedCustomer.Id,
                OrderDate.ToUniversalTime(), OrderStatus, TransportationCharges, OrderNotes);
        }
    }

    // ── Navigation back ───────────────────────────────────────────────────

    [RelayCommand]
    private void GoBack()
    {
        var listVm = services.GetRequiredService<PrintOrdersViewModel>();
        listVm.LoadAsync().ConfigureAwait(false);
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Print Orders";
        main.CurrentView = listVm;
    }

    // ── Summary ───────────────────────────────────────────────────────────

    private void RefreshSummary()
    {
        OrderTotal = Lines.Sum(l => l.ComputeTotal()) + (TransportationCharges ?? 0);
        TotalPaid  = Payments.Sum(p => p.Amount);
        Balance    = OrderTotal - TotalPaid;
    }
}
```

- [ ] **Step 2: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add SmartSolutions.App/ViewModels/PrintOrderDetailViewModel.cs
git commit -m "feat(app): implement PrintOrderDetailViewModel — lines, payments, vendor"
```

---

### Task 19: PrintOrdersView + PrintOrderDetailView XAML

**Files:**
- Modify: `SmartSolutions.App/Views/PrintOrdersView.xaml`
- Modify: `SmartSolutions.App/Views/PrintOrderDetailView.xaml`

- [ ] **Step 1: Replace `PrintOrdersView.xaml`**

```xml
<!-- SmartSolutions.App/Views/PrintOrdersView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.PrintOrdersView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:converters="clr-namespace:SmartSolutions.App.Converters">
    <UserControl.Resources>
        <converters:CurrencyConverter x:Key="CurrencyConverter"/>
    </UserControl.Resources>
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Grid Grid.Row="0" Margin="0,0,0,16">
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" Text="Print Orders" Style="{StaticResource MaterialDesignHeadline5TextBlock}"/>
            <Button    Grid.Column="1" Content="New Order" Command="{Binding OpenNewOrderCommand}"
                       Style="{StaticResource MaterialDesignRaisedButton}"/>
        </Grid>

        <!-- Filters -->
        <md:Card Grid.Row="1" Padding="12" Margin="0,0,0,16">
            <StackPanel Orientation="Horizontal">
                <ComboBox md:HintAssist.Hint="Status" SelectedItem="{Binding FilterStatus}"
                          Width="140" Margin="0,0,12,0">
                    <ComboBox.Items>
                        <x:Null/>
                        <sys:String xmlns:sys="clr-namespace:System;assembly=mscorlib">Draft</sys:String>
                        <sys:String>Confirmed</sys:String>
                        <sys:String>SentToVendor</sys:String>
                        <sys:String>Ready</sys:String>
                        <sys:String>Delivered</sys:String>
                    </ComboBox.Items>
                </ComboBox>
                <CheckBox Content="Outstanding only" IsChecked="{Binding FilterOutstandingOnly}" Margin="0,0,16,0"/>
                <DatePicker md:HintAssist.Hint="From" SelectedDate="{Binding FilterFrom}" Width="130" Margin="0,0,8,0"/>
                <DatePicker md:HintAssist.Hint="To"   SelectedDate="{Binding FilterTo}"   Width="130" Margin="0,0,8,0"/>
                <Button Content="Search" Command="{Binding ApplyFiltersCommand}" Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>
                <Button Content="Clear"  Command="{Binding ClearFiltersCommand}" Style="{StaticResource MaterialDesignFlatButton}"/>
            </StackPanel>
        </md:Card>

        <!-- Order List -->
        <DataGrid Grid.Row="2" ItemsSource="{Binding OrderList}"
                  SelectedItem="{Binding SelectedOrder}"
                  AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                  md:DataGridAssist.CellPadding="8 6 8 6">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Order #"      Binding="{Binding Id}" Width="80"/>
                <DataGridTextColumn Header="Date"         Binding="{Binding Date, StringFormat=dd/MM/yyyy}" Width="100"/>
                <DataGridTextColumn Header="Customer"     Binding="{Binding Customer.Name}" Width="*"/>
                <DataGridTextColumn Header="Total"        Binding="{Binding ., Converter={StaticResource CurrencyConverter}}" Width="120"/>
                <DataGridTextColumn Header="Status"       Binding="{Binding Status}" Width="120"/>
                <DataGridTemplateColumn Header="Actions"  Width="160">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="Open"   Command="{Binding DataContext.OpenOrderCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource MaterialDesignFlatButton}" Margin="0,0,4,0"/>
                                <Button Content="Delete" Command="{Binding DataContext.DeleteOrderCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource MaterialDesignFlatButton}"
                                        Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Replace `PrintOrderDetailView.xaml`**

```xml
<!-- SmartSolutions.App/Views/PrintOrderDetailView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.PrintOrderDetailView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:converters="clr-namespace:SmartSolutions.App.Converters">
    <UserControl.Resources>
        <converters:CurrencyConverter             x:Key="CurrencyConverter"/>
        <converters:RateTypeToVisibilityConverter x:Key="RateTypeToVisibility"/>
    </UserControl.Resources>
    <ScrollViewer VerticalScrollBarVisibility="Auto">
      <StackPanel Margin="24" MaxWidth="1100">

        <!-- Header bar -->
        <Grid Margin="0,0,0,16">
            <Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="← Back" Command="{Binding GoBackCommand}" Style="{StaticResource MaterialDesignFlatButton}"/>
            <TextBlock Grid.Column="1" Text="{Binding IsNew, Converter={x:Static md:BooleanToVisibilityConverter.Instance}}"
                       Style="{StaticResource MaterialDesignHeadline6TextBlock}" VerticalAlignment="Center" Margin="8,0"/>
            <TextBlock Grid.Column="1" Text="{Binding OrderId, StringFormat='Print Order #{0}'}"
                       Style="{StaticResource MaterialDesignHeadline6TextBlock}" VerticalAlignment="Center" Margin="8,0"/>
        </Grid>

        <!-- Error message -->
        <TextBlock Text="{Binding ErrorMessage}" Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"
                   Margin="0,0,0,8" Visibility="{Binding ErrorMessage, Converter={StaticResource BoolToVisibility}}"/>

        <!-- Order Header Card -->
        <md:Card Padding="16" Margin="0,0,0,16">
          <Grid>
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="16"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0">
              <!-- Customer autocomplete -->
              <TextBox md:HintAssist.Hint="Customer (type to search) *"
                       Text="{Binding CustomerSearch, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,4"/>
              <ListBox ItemsSource="{Binding CustomerSuggestions}" DisplayMemberPath="Name"
                       MaxHeight="120" Margin="0,0,0,4"
                       Visibility="{Binding CustomerSuggestions.Count, Converter={StaticResource BoolToVisibility}}">
                <ListBox.InputBindings>
                    <MouseBinding Gesture="LeftClick"
                                  Command="{Binding SelectCustomerCommand}"
                                  CommandParameter="{Binding RelativeSource={RelativeSource AncestorType=ListBox}, Path=SelectedItem}"/>
                </ListBox.InputBindings>
              </ListBox>
              <Button Content="+ Create customer with this name" Command="{Binding CreateCustomerInlineCommand}"
                      Style="{StaticResource MaterialDesignFlatButton}" HorizontalAlignment="Left"/>
              <DatePicker md:HintAssist.Hint="Order Date *" SelectedDate="{Binding OrderDate}" Margin="0,8,0,0"/>
            </StackPanel>
            <StackPanel Grid.Column="2">
              <ComboBox md:HintAssist.Hint="Status" SelectedItem="{Binding OrderStatus}" Margin="0,0,0,8">
                <ComboBox.ItemsSource>
                    <x:Array Type="{x:Type local:PrintOrderStatus}" xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">
                        <local:PrintOrderStatus>Draft</local:PrintOrderStatus>
                        <local:PrintOrderStatus>Confirmed</local:PrintOrderStatus>
                        <local:PrintOrderStatus>SentToVendor</local:PrintOrderStatus>
                        <local:PrintOrderStatus>Ready</local:PrintOrderStatus>
                        <local:PrintOrderStatus>Delivered</local:PrintOrderStatus>
                    </x:Array>
                </ComboBox.ItemsSource>
              </ComboBox>
              <TextBox md:HintAssist.Hint="Transportation Charges (optional)"
                       Text="{Binding TransportationCharges, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Notes" Text="{Binding OrderNotes, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <Button Content="Save Header" Command="{Binding SaveOrderCommand}"
                      Style="{StaticResource MaterialDesignRaisedButton}" HorizontalAlignment="Left"/>
            </StackPanel>
          </Grid>
        </md:Card>

        <!-- Line Items Card -->
        <md:Card Padding="16" Margin="0,0,0,16">
          <StackPanel>
            <TextBlock Text="Line Items" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>

            <!-- New line entry row -->
            <Grid Margin="0,0,0,8">
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="160"/>
                <ColumnDefinition Width="160"/>
                <ColumnDefinition Width="100"/>
                <ColumnDefinition Width="80"/>
                <ColumnDefinition Width="80"/>
                <ColumnDefinition Width="80"/>
                <ColumnDefinition Width="80"/>
                <ColumnDefinition Width="100"/>
                <ColumnDefinition Width="Auto"/>
              </Grid.ColumnDefinitions>
              <ComboBox Grid.Column="0" md:HintAssist.Hint="Category"
                        ItemsSource="{Binding ItemCategories}" SelectedItem="{Binding NewLineCategory}"
                        DisplayMemberPath="Name" Margin="0,0,4,0"/>
              <ComboBox Grid.Column="1" md:HintAssist.Hint="Item"
                        ItemsSource="{Binding ItemNames}" SelectedItem="{Binding NewLineItemName}"
                        DisplayMemberPath="Name" Margin="0,0,4,0"/>
              <ComboBox Grid.Column="2" md:HintAssist.Hint="Rate Type"
                        SelectedItem="{Binding NewLineRateType}" Margin="0,0,4,0">
                <ComboBox.Items>
                    <local:RateType xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">PerSqft</local:RateType>
                    <local:RateType xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">PerPiece</local:RateType>
                </ComboBox.Items>
              </ComboBox>
              <!-- Dimension fields — hidden for PerPiece -->
              <ComboBox Grid.Column="3" md:HintAssist.Hint="Unit"
                        SelectedItem="{Binding NewLineUnit}" Margin="0,0,4,0"
                        Visibility="{Binding NewLineRateType, Converter={StaticResource RateTypeToVisibility}}">
                <ComboBox.Items>
                    <local:DimensionUnit xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">Feet</local:DimensionUnit>
                    <local:DimensionUnit xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">Inches</local:DimensionUnit>
                </ComboBox.Items>
              </ComboBox>
              <TextBox Grid.Column="4" md:HintAssist.Hint="H"
                       Text="{Binding NewLineHeight, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,4,0"
                       Visibility="{Binding NewLineRateType, Converter={StaticResource RateTypeToVisibility}}"/>
              <TextBox Grid.Column="5" md:HintAssist.Hint="W"
                       Text="{Binding NewLineWidth, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,4,0"
                       Visibility="{Binding NewLineRateType, Converter={StaticResource RateTypeToVisibility}}"/>
              <TextBox Grid.Column="6" md:HintAssist.Hint="Qty"
                       Text="{Binding NewLineQuantity, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,4,0"/>
              <TextBox Grid.Column="7" md:HintAssist.Hint="Rate"
                       Text="{Binding NewLineRate, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,4,0"/>
              <Button  Grid.Column="8" Content="Add Line" Command="{Binding AddLineCommand}"
                       Style="{StaticResource MaterialDesignRaisedButton}"/>
            </Grid>
            <TextBlock Text="{Binding NewLineComputedTotal, StringFormat='Preview Total: PKR {0:#,##0.00}'}"
                       Opacity="0.7" Margin="0,0,0,8"/>

            <!-- Lines list -->
            <DataGrid ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
              <DataGrid.Columns>
                <DataGridTextColumn Header="Item"      Binding="{Binding ItemName.Name}" Width="*"/>
                <DataGridTextColumn Header="Type"      Binding="{Binding RateType}" Width="80"/>
                <DataGridTextColumn Header="Unit"      Binding="{Binding Unit}" Width="60"/>
                <DataGridTextColumn Header="H"         Binding="{Binding Height}" Width="60"/>
                <DataGridTextColumn Header="W"         Binding="{Binding Width}" Width="60"/>
                <DataGridTextColumn Header="Qty"       Binding="{Binding Quantity}" Width="60"/>
                <DataGridTextColumn Header="Rate"      Binding="{Binding Rate, StringFormat='PKR {0:#,##0.00}'}" Width="100"/>
                <DataGridTextColumn Header="Total"     Binding="{Binding ., StringFormat='PKR {0:#,##0.00}'}" Width="110"/>
                <DataGridTemplateColumn Header="" Width="60">
                  <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                      <Button Content="✕" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                              CommandParameter="{Binding}" Style="{StaticResource MaterialDesignFlatButton}"/>
                    </DataTemplate>
                  </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
              </DataGrid.Columns>
            </DataGrid>

            <!-- Order total summary -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,8,0,0">
              <TextBlock Text="{Binding OrderTotal, StringFormat='Order Total: PKR {0:#,##0.00}'}" FontWeight="Bold" Margin="0,0,24,0"/>
              <TextBlock Text="{Binding TotalPaid,  StringFormat='Paid: PKR {0:#,##0.00}'}"        Margin="0,0,24,0"/>
              <TextBlock Text="{Binding Balance,    StringFormat='Balance: PKR {0:#,##0.00}'}"     FontWeight="Bold"/>
            </StackPanel>
          </StackPanel>
        </md:Card>

        <!-- Vendor Assignment Card -->
        <md:Card Padding="16" Margin="0,0,0,16">
          <StackPanel>
            <TextBlock Text="Vendor Assignment" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <Grid>
              <Grid.ColumnDefinitions><ColumnDefinition Width="200"/><ColumnDefinition Width="130"/><ColumnDefinition Width="130"/><ColumnDefinition Width="130"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
              <ComboBox Grid.Column="0" md:HintAssist.Hint="Vendor" ItemsSource="{Binding Vendors}"
                        SelectedItem="{Binding SelectedVendor}" DisplayMemberPath="Name" Margin="0,0,8,0"/>
              <DatePicker Grid.Column="1" md:HintAssist.Hint="Sent Date"     SelectedDate="{Binding VendorSentDate}"     Margin="0,0,8,0"/>
              <DatePicker Grid.Column="2" md:HintAssist.Hint="Expected Date" SelectedDate="{Binding VendorExpectedDate}" Margin="0,0,8,0"/>
              <TextBox    Grid.Column="3" md:HintAssist.Hint="Vendor Cost"   Text="{Binding VendorCost, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,8,0"/>
              <Button     Grid.Column="4" Content="Save" Command="{Binding SaveVendorAssignmentCommand}"
                          Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>
              <Button     Grid.Column="5" Content="Mark Paid" Command="{Binding MarkVendorPaidCommand}"
                          Style="{StaticResource MaterialDesignOutlinedButton}"/>
            </Grid>
          </StackPanel>
        </md:Card>

        <!-- Payments Card -->
        <md:Card Padding="16">
          <StackPanel>
            <TextBlock Text="Customer Payments" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <!-- Duplicate warning -->
            <TextBlock Text="{Binding DuplicatePaymentWarning}"
                       Foreground="Orange" Margin="0,0,0,8"
                       Visibility="{Binding DuplicatePaymentWarning, Converter={StaticResource BoolToVisibility}}"/>
            <!-- New payment entry -->
            <Grid Margin="0,0,0,8">
              <Grid.ColumnDefinitions><ColumnDefinition Width="130"/><ColumnDefinition Width="180"/><ColumnDefinition Width="130"/><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
              <TextBox  Grid.Column="0" md:HintAssist.Hint="Amount *" Text="{Binding NewPaymentAmount, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,8,0"/>
              <ComboBox Grid.Column="1" md:HintAssist.Hint="Channel *" ItemsSource="{Binding PaymentChannels}"
                        SelectedItem="{Binding NewPaymentChannel}" DisplayMemberPath="Name" Margin="0,0,8,0"/>
              <DatePicker Grid.Column="2" md:HintAssist.Hint="Date" SelectedDate="{Binding NewPaymentDate}" Margin="0,0,8,0"/>
              <TextBox  Grid.Column="3" md:HintAssist.Hint="Notes"   Text="{Binding NewPaymentNotes,  UpdateSourceTrigger=PropertyChanged}" Margin="0,0,8,0"/>
              <Button   Grid.Column="4" Content="Add Payment" Command="{Binding AddPaymentCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </Grid>
            <!-- Payments list -->
            <DataGrid ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
              <DataGrid.Columns>
                <DataGridTextColumn Header="Date"    Binding="{Binding Date, StringFormat=dd/MM/yyyy}" Width="100"/>
                <DataGridTextColumn Header="Amount"  Binding="{Binding Amount, StringFormat='PKR {0:#,##0.00}'}" Width="130"/>
                <DataGridTextColumn Header="Channel" Binding="{Binding Channel.Name}" Width="120"/>
                <DataGridTextColumn Header="Notes"   Binding="{Binding Notes}" Width="*"/>
                <DataGridTemplateColumn Header="" Width="60">
                  <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                      <Button Content="✕" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                              CommandParameter="{Binding}" Style="{StaticResource MaterialDesignFlatButton}"/>
                    </DataTemplate>
                  </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
              </DataGrid.Columns>
            </DataGrid>
          </StackPanel>
        </md:Card>

      </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add SmartSolutions.App/Views/
git commit -m "feat(app): implement PrintOrdersView and PrintOrderDetailView XAML"
```

---

## Phase 6: Haier Jobs Module

### Task 20: HaierJobsViewModel + HaierJobDetailViewModel

**Files:**
- Modify: `SmartSolutions.App/ViewModels/HaierJobsViewModel.cs`
- Modify: `SmartSolutions.App/ViewModels/HaierJobDetailViewModel.cs`

- [ ] **Step 1: Replace `HaierJobsViewModel.cs`**

```csharp
// SmartSolutions.App/ViewModels/HaierJobsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class HaierJobsViewModel(
    IHaierJobService jobs,
    IServiceProvider services) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<HaierJob> _jobList = [];
    [ObservableProperty] private HaierJobStatus? _filterStatus;
    [ObservableProperty] private HaierJobType?   _filterJobType;
    [ObservableProperty] private bool _isBusy;

    public async Task LoadAsync()
    {
        IsBusy = true;
        var results = await jobs.GetJobsAsync(FilterStatus, FilterJobType);
        JobList = new(results);
        IsBusy = false;
    }

    [RelayCommand] private async Task ApplyFiltersAsync() => await LoadAsync();

    [RelayCommand]
    private void OpenNewJob()
    {
        var vm = services.GetRequiredService<HaierJobDetailViewModel>();
        vm.InitNew();
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Haier Jobs";
        main.CurrentView = vm;
    }

    [RelayCommand]
    private void OpenJob(HaierJob job)
    {
        var vm = services.GetRequiredService<HaierJobDetailViewModel>();
        vm.InitEdit(job.Id);
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Haier Jobs";
        main.CurrentView = vm;
    }

    [RelayCommand]
    private async Task DeleteJobAsync(HaierJob job)
    {
        await jobs.DeleteJobAsync(job.Id);
        JobList.Remove(job);
    }
}
```

- [ ] **Step 2: Replace `HaierJobDetailViewModel.cs`**

```csharp
// SmartSolutions.App/ViewModels/HaierJobDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class HaierJobDetailViewModel(
    IHaierJobService jobs,
    ICustomerService customers,
    ILookupService lookup,
    IServiceProvider services) : ObservableObject
{
    [ObservableProperty] private int _jobId;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearch = "";
    [ObservableProperty] private ObservableCollection<Customer> _customerSuggestions = [];
    [ObservableProperty] private string _acModel = "";
    [ObservableProperty] private string _acSerial = "";
    [ObservableProperty] private string _problemDescription = "";
    [ObservableProperty] private ObservableCollection<Technician> _technicians = [];
    [ObservableProperty] private Technician? _selectedTechnician;
    [ObservableProperty] private HaierJobType _jobType = HaierJobType.OutOfWarranty;
    [ObservableProperty] private HaierJobStatus _jobStatus = HaierJobStatus.Pending;
    [ObservableProperty] private string _claimReferenceNumber = "";
    [ObservableProperty] private bool _isWarrantyJob;
    [ObservableProperty] private string _partsUsed = "";
    [ObservableProperty] private decimal _partsCost;
    [ObservableProperty] private DateTime _jobDate = DateTime.Today;
    [ObservableProperty] private string _jobNotes = "";
    [ObservableProperty] private string _errorMessage = "";

    // Payments
    [ObservableProperty] private ObservableCollection<HaierJobPayment> _payments = [];
    [ObservableProperty] private ObservableCollection<PaymentChannel> _paymentChannels = [];
    [ObservableProperty] private decimal _newPaymentAmount;
    [ObservableProperty] private PaymentChannel? _newPaymentChannel;
    [ObservableProperty] private DateTime _newPaymentDate = DateTime.Today;
    [ObservableProperty] private string _newPaymentNotes = "";
    [ObservableProperty] private string _duplicatePaymentWarning = "";
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private bool _isBusy;

    public void InitNew()
    {
        IsNew = true; JobId = 0; JobDate = DateTime.Today;
        LoadLookupsAsync().ConfigureAwait(false);
    }

    public void InitEdit(int jobId)
    {
        IsNew = false; JobId = jobId;
        LoadAsync().ConfigureAwait(false);
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        await LoadLookupsAsync();
        if (JobId > 0)
        {
            var job = await jobs.GetJobWithDetailsAsync(JobId);
            SelectedCustomer = job.Customer;
            CustomerSearch = job.Customer.Name;
            AcModel = job.AcModel; AcSerial = job.AcSerial ?? "";
            ProblemDescription = job.ProblemDescription;
            SelectedTechnician = job.Technician;
            JobType = job.JobType; JobStatus = job.Status;
            IsWarrantyJob = job.JobType == HaierJobType.Warranty;
            ClaimReferenceNumber = job.ClaimReferenceNumber ?? "";
            PartsUsed = job.PartsUsed ?? ""; PartsCost = job.PartsCost;
            JobDate = job.Date.ToLocalTime(); JobNotes = job.Notes ?? "";
            Payments = new(job.Payments);
            RefreshSummary();
        }
        IsBusy = false;
    }

    private async Task LoadLookupsAsync()
    {
        Technicians     = new(await lookup.GetTechniciansAsync());
        PaymentChannels = new(await lookup.GetPaymentChannelsAsync());
    }

    partial void OnJobTypeChanged(HaierJobType value) =>
        IsWarrantyJob = value == HaierJobType.Warranty;

    partial void OnCustomerSearchChanged(string value)
    {
        if (value.Length >= 2) SearchCustomersAsync(value).ConfigureAwait(false);
        else CustomerSuggestions.Clear();
    }

    private async Task SearchCustomersAsync(string query)
    {
        CustomerSuggestions = new(await customers.SearchCustomersAsync(query));
    }

    [RelayCommand]
    private void SelectCustomer(Customer c)
    {
        SelectedCustomer = c; CustomerSearch = c.Name; CustomerSuggestions.Clear();
    }

    [RelayCommand]
    private async Task CreateCustomerInlineAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerSearch)) return;
        var c = await customers.AddCustomerAsync(CustomerSearch.Trim(), null, null, null);
        SelectedCustomer = c; CustomerSuggestions.Clear();
    }

    [RelayCommand]
    private async Task SaveJobAsync()
    {
        if (SelectedCustomer is null)      { ErrorMessage = "Customer is required."; return; }
        if (string.IsNullOrWhiteSpace(AcModel)) { ErrorMessage = "AC model is required."; return; }
        if (SelectedTechnician is null)    { ErrorMessage = "Technician is required."; return; }
        ErrorMessage = "";

        if (IsNew)
        {
            var newJob = await jobs.CreateJobAsync(SelectedCustomer.Id, AcModel.Trim(),
                NullIfEmpty(AcSerial), ProblemDescription.Trim(), SelectedTechnician.Id,
                JobType, NullIfEmpty(ClaimReferenceNumber), NullIfEmpty(PartsUsed),
                PartsCost, JobDate.ToUniversalTime(), NullIfEmpty(JobNotes));
            JobId = newJob.Id; IsNew = false;
        }
        else
        {
            await jobs.UpdateJobAsync(JobId, SelectedCustomer.Id, AcModel.Trim(),
                NullIfEmpty(AcSerial), ProblemDescription.Trim(), SelectedTechnician.Id,
                JobType, JobStatus, NullIfEmpty(ClaimReferenceNumber), NullIfEmpty(PartsUsed),
                PartsCost, JobDate.ToUniversalTime(), NullIfEmpty(JobNotes));
        }
    }

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        if (NewPaymentAmount <= 0)     { ErrorMessage = "Amount must be > 0."; return; }
        if (NewPaymentChannel is null) { ErrorMessage = "Select a channel.";   return; }
        if (JobId == 0)                { ErrorMessage = "Save the job first."; return; }
        ErrorMessage = "";

        var payment = await jobs.AddPaymentAsync(JobId, NewPaymentAmount,
            NewPaymentChannel.Id, NewPaymentDate.ToUniversalTime(), NullIfEmpty(NewPaymentNotes));
        payment.Channel = NewPaymentChannel;
        Payments.Add(payment);
        NewPaymentAmount = 0; NewPaymentNotes = "";
        DuplicatePaymentWarning = "";
        RefreshSummary();
    }

    [RelayCommand]
    private async Task DeletePaymentAsync(HaierJobPayment payment)
    {
        await jobs.DeletePaymentAsync(payment.Id);
        Payments.Remove(payment);
        RefreshSummary();
    }

    [RelayCommand]
    private void GoBack()
    {
        var listVm = services.GetRequiredService<HaierJobsViewModel>();
        listVm.LoadAsync().ConfigureAwait(false);
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Haier Jobs";
        main.CurrentView = listVm;
    }

    private void RefreshSummary()
    {
        TotalPaid = Payments.Sum(p => p.Amount);
        Balance   = PartsCost - TotalPaid;
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
```

- [ ] **Step 3: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add SmartSolutions.App/ViewModels/HaierJobsViewModel.cs SmartSolutions.App/ViewModels/HaierJobDetailViewModel.cs
git commit -m "feat(app): implement HaierJobsViewModel + HaierJobDetailViewModel"
```

---

### Task 21: HaierJobsView + HaierJobDetailView XAML

**Files:**
- Modify: `SmartSolutions.App/Views/HaierJobsView.xaml`
- Modify: `SmartSolutions.App/Views/HaierJobDetailView.xaml`

- [ ] **Step 1: Replace `HaierJobsView.xaml`**

```xml
<!-- SmartSolutions.App/Views/HaierJobsView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.HaierJobsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <Grid Grid.Row="0" Margin="0,0,0,16">
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
            <TextBlock Grid.Column="0" Text="Haier AC Jobs" Style="{StaticResource MaterialDesignHeadline5TextBlock}"/>
            <Button    Grid.Column="1" Content="New Job" Command="{Binding OpenNewJobCommand}"
                       Style="{StaticResource MaterialDesignRaisedButton}"/>
        </Grid>
        <md:Card Grid.Row="1" Padding="12" Margin="0,0,0,16">
            <StackPanel Orientation="Horizontal">
                <ComboBox md:HintAssist.Hint="Status"   SelectedItem="{Binding FilterStatus}"   Width="130" Margin="0,0,12,0"/>
                <ComboBox md:HintAssist.Hint="Job Type" SelectedItem="{Binding FilterJobType}"  Width="130" Margin="0,0,12,0"/>
                <Button Content="Search" Command="{Binding ApplyFiltersCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"/>
            </StackPanel>
        </md:Card>
        <DataGrid Grid.Row="2" ItemsSource="{Binding JobList}"
                  AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Job #"       Binding="{Binding Id}" Width="70"/>
                <DataGridTextColumn Header="Date"        Binding="{Binding Date, StringFormat=dd/MM/yyyy}" Width="100"/>
                <DataGridTextColumn Header="Customer"    Binding="{Binding Customer.Name}" Width="*"/>
                <DataGridTextColumn Header="AC Model"    Binding="{Binding AcModel}" Width="120"/>
                <DataGridTextColumn Header="Technician"  Binding="{Binding Technician.Name}" Width="120"/>
                <DataGridTextColumn Header="Type"        Binding="{Binding JobType}" Width="100"/>
                <DataGridTextColumn Header="Status"      Binding="{Binding Status}" Width="100"/>
                <DataGridTemplateColumn Header="Actions" Width="160">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="Open"   Command="{Binding DataContext.OpenJobCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding}" Style="{StaticResource MaterialDesignFlatButton}" Margin="0,0,4,0"/>
                                <Button Content="Delete" Command="{Binding DataContext.DeleteJobCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                        CommandParameter="{Binding}" Style="{StaticResource MaterialDesignFlatButton}"
                                        Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Replace `HaierJobDetailView.xaml`**

```xml
<!-- SmartSolutions.App/Views/HaierJobDetailView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.HaierJobDetailView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:converters="clr-namespace:SmartSolutions.App.Converters">
    <UserControl.Resources>
        <converters:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
    </UserControl.Resources>
    <ScrollViewer VerticalScrollBarVisibility="Auto">
      <StackPanel Margin="24" MaxWidth="900">
        <!-- Header -->
        <Grid Margin="0,0,0,16">
          <Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
          <Button Grid.Column="0" Content="← Back" Command="{Binding GoBackCommand}" Style="{StaticResource MaterialDesignFlatButton}"/>
          <TextBlock Grid.Column="1" Text="{Binding JobId, StringFormat='Haier Job #{0}'}"
                     Style="{StaticResource MaterialDesignHeadline6TextBlock}" VerticalAlignment="Center" Margin="8,0"/>
        </Grid>
        <TextBlock Text="{Binding ErrorMessage}" Foreground="{DynamicResource MaterialDesignValidationErrorBrush}" Margin="0,0,0,8"
                   Visibility="{Binding ErrorMessage, Converter={StaticResource BoolToVisibility}}"/>

        <!-- Job Details -->
        <md:Card Padding="16" Margin="0,0,0,16">
          <Grid>
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="16"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0">
              <TextBox md:HintAssist.Hint="Customer (type to search) *"
                       Text="{Binding CustomerSearch, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,4"/>
              <ListBox ItemsSource="{Binding CustomerSuggestions}" DisplayMemberPath="Name" MaxHeight="120"
                       Visibility="{Binding CustomerSuggestions.Count, Converter={StaticResource BoolToVisibility}}">
                <ListBox.InputBindings>
                    <MouseBinding Gesture="LeftClick"
                                  Command="{Binding SelectCustomerCommand}"
                                  CommandParameter="{Binding RelativeSource={RelativeSource AncestorType=ListBox}, Path=SelectedItem}"/>
                </ListBox.InputBindings>
              </ListBox>
              <Button Content="+ Create customer" Command="{Binding CreateCustomerInlineCommand}"
                      Style="{StaticResource MaterialDesignFlatButton}" HorizontalAlignment="Left"/>
              <TextBox md:HintAssist.Hint="AC Model *"   Text="{Binding AcModel,  UpdateSourceTrigger=PropertyChanged}" Margin="0,8,0,8"/>
              <TextBox md:HintAssist.Hint="AC Serial"    Text="{Binding AcSerial, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Problem Description *"
                       Text="{Binding ProblemDescription, UpdateSourceTrigger=PropertyChanged}"
                       AcceptsReturn="True" Height="80" Margin="0,0,0,8"/>
            </StackPanel>
            <StackPanel Grid.Column="2">
              <ComboBox md:HintAssist.Hint="Technician *" ItemsSource="{Binding Technicians}"
                        SelectedItem="{Binding SelectedTechnician}" DisplayMemberPath="Name" Margin="0,0,0,8"/>
              <ComboBox md:HintAssist.Hint="Job Type" SelectedItem="{Binding JobType}" Margin="0,0,0,8">
                <ComboBox.Items>
                    <local:HaierJobType xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">Warranty</local:HaierJobType>
                    <local:HaierJobType xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">OutOfWarranty</local:HaierJobType>
                </ComboBox.Items>
              </ComboBox>
              <!-- Claim reference — only for warranty jobs -->
              <TextBox md:HintAssist.Hint="Claim Reference Number"
                       Text="{Binding ClaimReferenceNumber, UpdateSourceTrigger=PropertyChanged}"
                       Margin="0,0,0,8"
                       Visibility="{Binding IsWarrantyJob, Converter={StaticResource BoolToVisibility}}"/>
              <ComboBox md:HintAssist.Hint="Status" SelectedItem="{Binding JobStatus}" Margin="0,0,0,8">
                <ComboBox.Items>
                    <local:HaierJobStatus xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">Pending</local:HaierJobStatus>
                    <local:HaierJobStatus xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">InProgress</local:HaierJobStatus>
                    <local:HaierJobStatus xmlns:local="clr-namespace:SmartSolutions.Data.Entities;assembly=SmartSolutions.Data">Completed</local:HaierJobStatus>
                </ComboBox.Items>
              </ComboBox>
              <TextBox md:HintAssist.Hint="Parts Used"  Text="{Binding PartsUsed,  UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Parts Cost"  Text="{Binding PartsCost,  UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <DatePicker md:HintAssist.Hint="Date"     SelectedDate="{Binding JobDate}" Margin="0,0,0,8"/>
              <TextBox md:HintAssist.Hint="Notes"       Text="{Binding JobNotes,   UpdateSourceTrigger=PropertyChanged}" Margin="0,0,0,8"/>
              <Button Content="Save Job" Command="{Binding SaveJobCommand}"
                      Style="{StaticResource MaterialDesignRaisedButton}" HorizontalAlignment="Left"/>
            </StackPanel>
          </Grid>
        </md:Card>

        <!-- Payments (out-of-warranty only) -->
        <md:Card Padding="16"
                 Visibility="{Binding IsWarrantyJob, Converter={StaticResource InverseBoolToVisibility}, ConverterParameter=inverse}">
          <StackPanel>
            <TextBlock Text="Payments" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <Grid Margin="0,0,0,8">
              <Grid.ColumnDefinitions><ColumnDefinition Width="120"/><ColumnDefinition Width="160"/><ColumnDefinition Width="120"/><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
              <TextBox  Grid.Column="0" md:HintAssist.Hint="Amount *" Text="{Binding NewPaymentAmount, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,8,0"/>
              <ComboBox Grid.Column="1" md:HintAssist.Hint="Channel *" ItemsSource="{Binding PaymentChannels}"
                        SelectedItem="{Binding NewPaymentChannel}" DisplayMemberPath="Name" Margin="0,0,8,0"/>
              <DatePicker Grid.Column="2" SelectedDate="{Binding NewPaymentDate}" Margin="0,0,8,0"/>
              <TextBox  Grid.Column="3" md:HintAssist.Hint="Notes" Text="{Binding NewPaymentNotes, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,8,0"/>
              <Button   Grid.Column="4" Content="Add Payment" Command="{Binding AddPaymentCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </Grid>
            <DataGrid ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
              <DataGrid.Columns>
                <DataGridTextColumn Header="Date"    Binding="{Binding Date, StringFormat=dd/MM/yyyy}" Width="100"/>
                <DataGridTextColumn Header="Amount"  Binding="{Binding Amount, StringFormat='PKR {0:#,##0.00}'}" Width="130"/>
                <DataGridTextColumn Header="Channel" Binding="{Binding Channel.Name}" Width="120"/>
                <DataGridTextColumn Header="Notes"   Binding="{Binding Notes}" Width="*"/>
                <DataGridTemplateColumn Header="" Width="60">
                  <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                      <Button Content="✕" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                              CommandParameter="{Binding}" Style="{StaticResource MaterialDesignFlatButton}"/>
                    </DataTemplate>
                  </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
              </DataGrid.Columns>
            </DataGrid>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,8,0,0">
              <TextBlock Text="{Binding TotalPaid, StringFormat='Paid: PKR {0:#,##0.00}'}" Margin="0,0,24,0"/>
              <TextBlock Text="{Binding Balance,   StringFormat='Balance: PKR {0:#,##0.00}'}" FontWeight="Bold"/>
            </StackPanel>
          </StackPanel>
        </md:Card>

      </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add SmartSolutions.App/Views/HaierJobsView.xaml SmartSolutions.App/Views/HaierJobDetailView.xaml
git commit -m "feat(app): implement HaierJobsView and HaierJobDetailView XAML"
```

---

## Phase 7: Expenses Module

### Task 22: ExpensesViewModel + ExpensesView

**Files:**
- Modify: `SmartSolutions.App/ViewModels/ExpensesViewModel.cs`
- Modify: `SmartSolutions.App/Views/ExpensesView.xaml`

- [ ] **Step 1: Replace `ExpensesViewModel.cs`**

```csharp
// SmartSolutions.App/ViewModels/ExpensesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class ExpensesViewModel(
    IExpenseService expenseService,
    ILookupService lookup) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<Expense> _expenses = [];
    [ObservableProperty] private ObservableCollection<ExpenseCategory> _categories = [];
    [ObservableProperty] private ObservableCollection<PaymentChannel> _channels = [];
    [ObservableProperty] private ExpenseCategory? _filterCategory;
    [ObservableProperty] private DateTime? _filterFrom;
    [ObservableProperty] private DateTime? _filterTo;

    // Add form
    [ObservableProperty] private ExpenseCategory? _newCategory;
    [ObservableProperty] private string _newDescription = "";
    [ObservableProperty] private decimal _newAmount;
    [ObservableProperty] private PaymentChannel? _newChannel;
    [ObservableProperty] private DateTime _newDate = DateTime.Today;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isBusy;

    public decimal MonthlyTotal => Expenses.Sum(e => e.Amount);

    public async Task LoadAsync()
    {
        IsBusy = true;
        Categories = new(await lookup.GetExpenseCategoriesAsync());
        Channels   = new(await lookup.GetPaymentChannelsAsync());
        var results = await expenseService.GetExpensesAsync(FilterCategory?.Id, FilterFrom, FilterTo);
        Expenses = new(results);
        OnPropertyChanged(nameof(MonthlyTotal));
        IsBusy = false;
    }

    [RelayCommand] private async Task ApplyFiltersAsync() => await LoadAsync();

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (NewCategory is null)     { ErrorMessage = "Category is required."; return; }
        if (NewAmount <= 0)          { ErrorMessage = "Amount must be > 0.";   return; }
        if (NewChannel is null)      { ErrorMessage = "Channel is required.";  return; }
        ErrorMessage = "";

        var expense = await expenseService.AddExpenseAsync(NewCategory.Id,
            string.IsNullOrWhiteSpace(NewDescription) ? null : NewDescription.Trim(),
            NewAmount, NewChannel.Id, NewDate.ToUniversalTime());
        expense.Category = NewCategory;
        expense.Channel  = NewChannel;
        Expenses.Insert(0, expense);
        NewAmount = 0; NewDescription = "";
        OnPropertyChanged(nameof(MonthlyTotal));
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(Expense expense)
    {
        await expenseService.DeleteExpenseAsync(expense.Id);
        Expenses.Remove(expense);
        OnPropertyChanged(nameof(MonthlyTotal));
    }
}
```

- [ ] **Step 2: Replace `ExpensesView.xaml`**

```xml
<!-- SmartSolutions.App/Views/ExpensesView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.ExpensesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="Expenses"
                   Style="{StaticResource MaterialDesignHeadline5TextBlock}" Margin="0,0,0,16"/>
        <TextBlock Grid.Row="0" Text="{Binding MonthlyTotal, StringFormat='Showing Total: PKR {0:#,##0.00}'}"
                   HorizontalAlignment="Right" VerticalAlignment="Center" Opacity="0.7"/>

        <!-- Add expense form -->
        <md:Card Grid.Row="1" Padding="16" Margin="0,0,0,16">
          <StackPanel>
            <TextBlock Text="Add Expense" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <TextBlock Text="{Binding ErrorMessage}" Foreground="{DynamicResource MaterialDesignValidationErrorBrush}" Margin="0,0,0,8"/>
            <Grid>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="180"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="130"/>
                <ColumnDefinition Width="160"/>
                <ColumnDefinition Width="130"/>
                <ColumnDefinition Width="Auto"/>
              </Grid.ColumnDefinitions>
              <ComboBox Grid.Column="0" md:HintAssist.Hint="Category *" ItemsSource="{Binding Categories}"
                        SelectedItem="{Binding NewCategory}" DisplayMemberPath="Name" Margin="0,0,8,0"/>
              <TextBox  Grid.Column="1" md:HintAssist.Hint="Description" Text="{Binding NewDescription, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,8,0"/>
              <TextBox  Grid.Column="2" md:HintAssist.Hint="Amount *"    Text="{Binding NewAmount, UpdateSourceTrigger=PropertyChanged}" Margin="0,0,8,0"/>
              <ComboBox Grid.Column="3" md:HintAssist.Hint="Channel *"   ItemsSource="{Binding Channels}"
                        SelectedItem="{Binding NewChannel}" DisplayMemberPath="Name" Margin="0,0,8,0"/>
              <DatePicker Grid.Column="4" SelectedDate="{Binding NewDate}" Margin="0,0,8,0"/>
              <Button   Grid.Column="5" Content="Add" Command="{Binding AddExpenseCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </Grid>
          </StackPanel>
        </md:Card>

        <!-- Filters -->
        <md:Card Grid.Row="2" Padding="12" Margin="0,0,0,16">
          <StackPanel Orientation="Horizontal">
            <ComboBox md:HintAssist.Hint="Category filter" ItemsSource="{Binding Categories}"
                      SelectedItem="{Binding FilterCategory}" DisplayMemberPath="Name" Width="160" Margin="0,0,12,0"/>
            <DatePicker md:HintAssist.Hint="From" SelectedDate="{Binding FilterFrom}" Width="130" Margin="0,0,8,0"/>
            <DatePicker md:HintAssist.Hint="To"   SelectedDate="{Binding FilterTo}"   Width="130" Margin="0,0,8,0"/>
            <Button Content="Search" Command="{Binding ApplyFiltersCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"/>
          </StackPanel>
        </md:Card>

        <!-- Expense list -->
        <DataGrid Grid.Row="3" ItemsSource="{Binding Expenses}"
                  AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Date"        Binding="{Binding Date, StringFormat=dd/MM/yyyy}" Width="100"/>
                <DataGridTextColumn Header="Category"    Binding="{Binding Category.Name}" Width="160"/>
                <DataGridTextColumn Header="Description" Binding="{Binding Description}" Width="*"/>
                <DataGridTextColumn Header="Amount"      Binding="{Binding Amount, StringFormat='PKR {0:#,##0.00}'}" Width="130"/>
                <DataGridTextColumn Header="Channel"     Binding="{Binding Channel.Name}" Width="120"/>
                <DataGridTemplateColumn Header="" Width="60">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="✕" Command="{Binding DataContext.DeleteExpenseCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                    CommandParameter="{Binding}" Style="{StaticResource MaterialDesignFlatButton}"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

- [ ] **Step 3: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add SmartSolutions.App/ViewModels/ExpensesViewModel.cs SmartSolutions.App/Views/ExpensesView.xaml
git commit -m "feat(app): implement ExpensesViewModel + ExpensesView"
```

---

## Phase 8: Dashboard

### Task 23: DashboardViewModel + DashboardView

**Files:**
- Modify: `SmartSolutions.App/ViewModels/DashboardViewModel.cs`
- Modify: `SmartSolutions.App/Views/DashboardView.xaml`

- [ ] **Step 1: Replace `DashboardViewModel.cs`**

```csharp
// SmartSolutions.App/ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class DashboardViewModel(IDashboardService dashboard) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<DayBookEntry> _dayBookEntries = [];
    [ObservableProperty] private DateTime _dayBookDate = DateTime.Today;
    [ObservableProperty] private BalanceSummary _balanceSummary = new(0, 0, 0);
    [ObservableProperty] private ObservableCollection<OutstandingItem> _outstandingItems = [];
    [ObservableProperty] private MonthlySummary? _monthlySummary;
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private int _selectedYear  = DateTime.Today.Year;
    [ObservableProperty] private bool _isBusy;

    public async Task LoadAsync()
    {
        IsBusy = true;
        DayBookEntries   = new(await dashboard.GetDayBookAsync(DayBookDate));
        BalanceSummary   = await dashboard.GetBalanceSummaryAsync();
        OutstandingItems = new(await dashboard.GetOutstandingItemsAsync());
        MonthlySummary   = await dashboard.GetMonthlySummaryAsync(SelectedYear, SelectedMonth);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RefreshDayBookAsync()
    {
        DayBookEntries = new(await dashboard.GetDayBookAsync(DayBookDate));
    }

    [RelayCommand]
    private async Task RefreshMonthlyAsync()
    {
        MonthlySummary = await dashboard.GetMonthlySummaryAsync(SelectedYear, SelectedMonth);
    }
}
```

- [ ] **Step 2: Replace `DashboardView.xaml`**

```xml
<!-- SmartSolutions.App/Views/DashboardView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
      <StackPanel Margin="24">
        <TextBlock Text="Dashboard" Style="{StaticResource MaterialDesignHeadline5TextBlock}" Margin="0,0,0,16"/>

        <!-- Balance Cards row -->
        <Grid Margin="0,0,0,16">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="16"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="16"/>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>

          <md:Card Grid.Column="0" Padding="16">
            <StackPanel>
              <TextBlock Text="Cash in Hand" Opacity="0.7"/>
              <TextBlock Text="{Binding BalanceSummary.Cash, StringFormat='PKR {0:#,##0.00}'}"
                         Style="{StaticResource MaterialDesignHeadline6TextBlock}"/>
            </StackPanel>
          </md:Card>

          <md:Card Grid.Column="2" Padding="16">
            <StackPanel>
              <TextBlock Text="Easypaisa" Opacity="0.7"/>
              <TextBlock Text="{Binding BalanceSummary.Easypaisa, StringFormat='PKR {0:#,##0.00}'}"
                         Style="{StaticResource MaterialDesignHeadline6TextBlock}"/>
            </StackPanel>
          </md:Card>

          <md:Card Grid.Column="4" Padding="16">
            <StackPanel>
              <TextBlock Text="Bank" Opacity="0.7"/>
              <TextBlock Text="{Binding BalanceSummary.Bank, StringFormat='PKR {0:#,##0.00}'}"
                         Style="{StaticResource MaterialDesignHeadline6TextBlock}"/>
            </StackPanel>
          </md:Card>
        </Grid>

        <!-- Day Book + Outstanding side by side -->
        <Grid Margin="0,0,0,16">
          <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="16"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>

          <!-- Day Book -->
          <md:Card Grid.Column="0" Padding="16">
            <StackPanel>
              <TextBlock Text="Day Book" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
              <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                <DatePicker SelectedDate="{Binding DayBookDate}" Width="140" Margin="0,0,8,0"/>
                <Button Content="Refresh" Command="{Binding RefreshDayBookCommand}"
                        Style="{StaticResource MaterialDesignOutlinedButton}"/>
              </StackPanel>
              <DataGrid ItemsSource="{Binding DayBookEntries}"
                        AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True" MaxHeight="300">
                <DataGrid.Columns>
                  <DataGridTextColumn Header="Description" Binding="{Binding Description}" Width="*"/>
                  <DataGridTextColumn Header="Amount"      Binding="{Binding Amount, StringFormat='PKR {0:#,##0.00}'}" Width="110"/>
                  <DataGridTextColumn Header="Channel"     Binding="{Binding Channel}" Width="90"/>
                  <DataGridTextColumn Header="Type"        Binding="{Binding Type}" Width="70"/>
                </DataGrid.Columns>
              </DataGrid>
            </StackPanel>
          </md:Card>

          <!-- Outstanding Balances -->
          <md:Card Grid.Column="2" Padding="16">
            <StackPanel>
              <TextBlock Text="Outstanding Balances" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
              <DataGrid ItemsSource="{Binding OutstandingItems}"
                        AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True" MaxHeight="300">
                <DataGrid.Columns>
                  <DataGridTextColumn Header="Order/Job"  Binding="{Binding Label}" Width="120"/>
                  <DataGridTextColumn Header="Customer"   Binding="{Binding Customer}" Width="*"/>
                  <DataGridTextColumn Header="Balance"    Binding="{Binding Balance, StringFormat='PKR {0:#,##0.00}'}" Width="110"/>
                </DataGrid.Columns>
              </DataGrid>
            </StackPanel>
          </md:Card>
        </Grid>

        <!-- Monthly Snapshot -->
        <md:Card Padding="16">
          <StackPanel>
            <TextBlock Text="Monthly Snapshot" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
              <TextBox md:HintAssist.Hint="Month (1-12)" Text="{Binding SelectedMonth, UpdateSourceTrigger=PropertyChanged}" Width="80" Margin="0,0,8,0"/>
              <TextBox md:HintAssist.Hint="Year"         Text="{Binding SelectedYear,  UpdateSourceTrigger=PropertyChanged}" Width="80" Margin="0,0,8,0"/>
              <Button Content="Go" Command="{Binding RefreshMonthlyCommand}" Style="{StaticResource MaterialDesignOutlinedButton}"/>
            </StackPanel>
            <StackPanel Orientation="Horizontal">
              <TextBlock Text="{Binding MonthlySummary.TotalIncome,   StringFormat='Income: PKR {0:#,##0.00}'}"   Margin="0,0,32,0" FontSize="14"/>
              <TextBlock Text="{Binding MonthlySummary.TotalExpenses, StringFormat='Expenses: PKR {0:#,##0.00}'}" Margin="0,0,32,0" FontSize="14"/>
              <TextBlock Text="{Binding MonthlySummary.Profit,        StringFormat='Profit: PKR {0:#,##0.00}'}"   FontSize="14" FontWeight="Bold"/>
            </StackPanel>
          </StackPanel>
        </md:Card>

      </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add SmartSolutions.App/ViewModels/DashboardViewModel.cs SmartSolutions.App/Views/DashboardView.xaml
git commit -m "feat(app): implement DashboardViewModel + DashboardView"
```

---

## Phase 9: PDF Invoice

### Task 24: InvoiceService and FastReport template

**Files:**
- Create: `SmartSolutions.Core/Interfaces/IInvoiceService.cs`
- Create: `SmartSolutions.Core/Services/InvoiceService.cs`
- Create: `SmartSolutions.App/Reports/Invoice.frx`

- [ ] **Step 1: Create `IInvoiceService.cs`**

```csharp
// SmartSolutions.Core/Interfaces/IInvoiceService.cs
namespace SmartSolutions.Core.Interfaces;

public interface IInvoiceService
{
    Task PrintInvoiceAsync(int orderId);
    Task SaveInvoiceToPdfAsync(int orderId, string filePath);
}
```

- [ ] **Step 2: Add `IInvoiceService` to `ServiceConfiguration.cs`**

In `SmartSolutions.App/ServiceConfiguration.cs`, add inside `ConfigureServices`:

```csharp
services.AddSingleton<IInvoiceService, InvoiceService>();
```

Also add `using SmartSolutions.Core.Services;` if not present.

- [ ] **Step 3: Create `InvoiceService.cs`**

> **Note:** FastReport Community requires creating a report object, loading the `.frx` template, and passing a data source. The frx file is an XML template created in the FastReport designer. The code below loads the template from the `Reports/` folder next to the executable.

```csharp
// SmartSolutions.Core/Services/InvoiceService.cs
using FastReport;
using FastReport.Export.Pdf;
using Microsoft.EntityFrameworkCore;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;
using System.Data;

namespace SmartSolutions.Core.Services;

public class InvoiceService(
    IDbContextFactory<AppDbContext> factory,
    IPrintOrderService orderService,
    ILookupService lookupService) : IInvoiceService
{
    private static string TemplatePath =>
        Path.Combine(AppContext.BaseDirectory, "Reports", "Invoice.frx");

    public async Task PrintInvoiceAsync(int orderId)
    {
        var report = await BuildReportAsync(orderId);
        report.Show();
    }

    public async Task SaveInvoiceToPdfAsync(int orderId, string filePath)
    {
        var report = await BuildReportAsync(orderId);
        var export = new PDFExport();
        report.Export(export, filePath);
    }

    private async Task<Report> BuildReportAsync(int orderId)
    {
        var order   = await orderService.GetOrderWithDetailsAsync(orderId);
        var bizInfo = await lookupService.GetBusinessInfoAsync();

        var report = new Report();
        report.Load(TemplatePath);

        // Pass scalar parameters
        report.SetParameterValue("OrderId",   order.Id);
        report.SetParameterValue("OrderDate", order.Date.ToLocalTime().ToString("dd/MM/yyyy"));
        report.SetParameterValue("CustomerName",    order.Customer.Name);
        report.SetParameterValue("CustomerAddress", order.Customer.Address ?? "");
        report.SetParameterValue("BizName",    bizInfo.Name);
        report.SetParameterValue("BizNtn",     bizInfo.Ntn);
        report.SetParameterValue("BizAddress", bizInfo.Address);
        report.SetParameterValue("BizPhone1",  bizInfo.Phone1);
        report.SetParameterValue("BizPhone2",  bizInfo.Phone2 ?? "");
        report.SetParameterValue("BizEmail",   bizInfo.Email  ?? "");

        var subtotal = order.Lines.Sum(l => l.ComputeTotal());
        var transport = order.TransportationCharges ?? 0;
        var total = subtotal + transport;
        var paid  = order.Payments.Sum(p => p.Amount);

        report.SetParameterValue("Subtotal",              subtotal);
        report.SetParameterValue("TransportationCharges", transport);
        report.SetParameterValue("OrderTotal",            total);
        report.SetParameterValue("TotalPaid",             paid);
        report.SetParameterValue("Balance",               total - paid);

        // Line items data table
        var dt = new DataTable("Lines");
        dt.Columns.Add("No",          typeof(int));
        dt.Columns.Add("Description", typeof(string));
        dt.Columns.Add("Qty",         typeof(int));
        dt.Columns.Add("Rate",        typeof(decimal));
        dt.Columns.Add("Total",       typeof(decimal));

        int i = 1;
        foreach (var line in order.Lines)
        {
            var desc = line.RateType == RateType.PerSqft
                ? $"{line.ItemName.Name} {line.Height}×{line.Width} {line.Unit}"
                : line.ItemName.Name;
            dt.Rows.Add(i++, desc, line.Quantity, line.Rate, line.ComputeTotal());
        }
        if (transport > 0)
            dt.Rows.Add(i, "Transportation Charges", 1, transport, transport);

        report.RegisterData(dt, "Lines");
        report.Prepare();
        return report;
    }
}
```

- [ ] **Step 4: Create the FastReport template `Invoice.frx`**

> **Note:** The `.frx` file is an XML file designed in the FastReport Designer (bundled with FastReport). After installing FastReport, launch the designer from Visual Studio or standalone and create the template based on the invoice spec in Section 10 of the PRD. Save it to `SmartSolutions.App/Reports/Invoice.frx`.
>
> Key objects to place in the designer:
> - **Report header band:** BizName, NTN, Subtitle text, "INVOICE" label, Serial No. (`[OrderId]`), Date (`[OrderDate]`)
> - **Data band:** bound to the `Lines` data table — columns for No, Description, Qty, Rate, Total RS
> - **Report footer band:** Subtotal, Transportation Charges (conditional), Total, Total Paid, Balance
> - **Page footer band:** Phone, Email, Address
>
> Bind each text object to the corresponding parameter: `[OrderId]`, `[CustomerName]`, `[Subtotal]`, etc.

In `SmartSolutions.App/SmartSolutions.App.csproj`, copy the Reports folder to output:

```xml
<ItemGroup>
  <None Update="Reports\Invoice.frx">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 5: Wire up "Print Invoice" button in `PrintOrderDetailView.xaml`**

Add to the header bar of `PrintOrderDetailView.xaml` (next to the "Save Header" button):

```xml
<Button Content="Print Invoice" Command="{Binding PrintInvoiceCommand}"
        Style="{StaticResource MaterialDesignOutlinedButton}" Margin="8,0,0,0"/>
```

Add the command to `PrintOrderDetailViewModel.cs`:

```csharp
// Inject IInvoiceService into PrintOrderDetailViewModel constructor:
// public partial class PrintOrderDetailViewModel(
//     IPrintOrderService orders,
//     ICustomerService customers,
//     ILookupService lookup,
//     IInvoiceService invoiceService,   ← add this
//     IServiceProvider services) ...

[RelayCommand]
private async Task PrintInvoiceAsync()
{
    if (OrderId == 0) { ErrorMessage = "Save the order before printing."; return; }
    await invoiceService.PrintInvoiceAsync(OrderId);
}
```

- [ ] **Step 6: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add .
git commit -m "feat(app): add InvoiceService + FastReport integration; wire Print Invoice button"
```

---

## Phase 10: Reports

### Task 25: Reports — Day Book, Outstanding, Monthly Summary, Per-Order Profit, Expense Breakdown

> These reports are read-only views that call `IDashboardService` and display formatted results. They are accessible from a "Reports" section or as modal dialogs. For simplicity in v1, reports are embedded as tabs/pages within a single `ReportsView`.

**Files:**
- Create: `SmartSolutions.App/ViewModels/ReportsViewModel.cs`
- Create: `SmartSolutions.App/Views/ReportsView.xaml`
- Modify: `SmartSolutions.App/ViewModels/MainViewModel.cs` — add NavigateToReports command
- Modify: `SmartSolutions.App/MainWindow.xaml` — add Reports button + DataTemplate

- [ ] **Step 1: Create `ReportsViewModel.cs`**

```csharp
// SmartSolutions.App/ViewModels/ReportsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Core.Services;
using SmartSolutions.Data;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;

namespace SmartSolutions.App.ViewModels;

public partial class ReportsViewModel(
    IDashboardService dashboard,
    IDbContextFactory<AppDbContext> factory) : ObservableObject
{
    // Day Book
    [ObservableProperty] private DateTime _dayBookDate = DateTime.Today;
    [ObservableProperty] private ObservableCollection<DayBookEntry> _dayBookEntries = [];
    [ObservableProperty] private decimal _dayBookIncomeTotal;
    [ObservableProperty] private decimal _dayBookExpenseTotal;

    // Outstanding
    [ObservableProperty] private ObservableCollection<OutstandingItem> _outstandingItems = [];
    [ObservableProperty] private decimal _outstandingTotal;

    // Monthly
    [ObservableProperty] private int _reportMonth = DateTime.Today.Month;
    [ObservableProperty] private int _reportYear  = DateTime.Today.Year;
    [ObservableProperty] private MonthlySummary? _monthly;

    // Per-Order Profit
    [ObservableProperty] private int _profitOrderId;
    [ObservableProperty] private string _perOrderProfitResult = "";

    // Expense Breakdown
    [ObservableProperty] private int _expBreakMonth = DateTime.Today.Month;
    [ObservableProperty] private int _expBreakYear  = DateTime.Today.Year;
    [ObservableProperty] private ObservableCollection<ExpenseBreakdownRow> _expenseBreakdown = [];
    [ObservableProperty] private bool _isBusy;

    public Task LoadAsync() => Task.CompletedTask;  // reports load on demand

    [RelayCommand]
    private async Task LoadDayBookAsync()
    {
        IsBusy = true;
        var entries = await dashboard.GetDayBookAsync(DayBookDate);
        DayBookEntries    = new(entries);
        DayBookIncomeTotal  = entries.Where(e => e.Type == "Income").Sum(e => e.Amount);
        DayBookExpenseTotal = entries.Where(e => e.Type == "Expense").Sum(e => e.Amount);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task LoadOutstandingAsync()
    {
        IsBusy = true;
        var items = await dashboard.GetOutstandingItemsAsync();
        OutstandingItems = new(items);
        OutstandingTotal = items.Sum(i => i.Balance);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task LoadMonthlyAsync()
    {
        IsBusy = true;
        Monthly = await dashboard.GetMonthlySummaryAsync(ReportYear, ReportMonth);
        IsBusy = false;
    }

    [RelayCommand]
    private async Task LoadPerOrderProfitAsync()
    {
        if (ProfitOrderId <= 0) { PerOrderProfitResult = "Enter an order ID."; return; }
        IsBusy = true;
        await using var db = factory.CreateDbContext();
        var order = await db.PrintOrders
            .Include(o => o.Lines)
            .Include(o => o.VendorAssignments)
            .FirstOrDefaultAsync(o => o.Id == ProfitOrderId);
        if (order is null) { PerOrderProfitResult = $"Order #{ProfitOrderId} not found."; IsBusy = false; return; }
        var revenue    = order.Lines.Sum(l => l.ComputeTotal()) + (order.TransportationCharges ?? 0);
        var vendorCost = order.VendorAssignments.Sum(a => a.VendorCost);
        var profit     = revenue - vendorCost;
        PerOrderProfitResult = $"Order #{ProfitOrderId}: Revenue PKR {revenue:#,##0.00} — Vendor Cost PKR {vendorCost:#,##0.00} = Profit PKR {profit:#,##0.00}";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task LoadExpenseBreakdownAsync()
    {
        IsBusy = true;
        await using var db = factory.CreateDbContext();
        var from = new DateTime(ExpBreakYear, ExpBreakMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = from.AddMonths(1);
        var rows = await db.Expenses
            .Include(e => e.Category)
            .Where(e => e.Date >= from && e.Date < to)
            .GroupBy(e => e.Category.Name)
            .Select(g => new ExpenseBreakdownRow(g.Key, g.Sum(e => e.Amount)))
            .ToListAsync();
        ExpenseBreakdown = new(rows.OrderByDescending(r => r.Total));
        IsBusy = false;
    }
}

public record ExpenseBreakdownRow(string Category, decimal Total);
```

- [ ] **Step 2: Add `ReportsViewModel` to `ServiceConfiguration.cs`**

```csharp
services.AddTransient<ViewModels.ReportsViewModel>();
```

- [ ] **Step 3: Add navigation to `MainViewModel.cs`**

Add this command to `MainViewModel.cs`:

```csharp
[RelayCommand]
private void NavigateToReports()
{
    CurrentSection = "Reports";
    CurrentView = services.GetRequiredService<ReportsViewModel>();
}
```

- [ ] **Step 4: Add to `MainWindow.xaml`**

Add the DataTemplate inside `Window.Resources`:

```xml
<DataTemplate DataType="{x:Type vm:ReportsViewModel}">
    <views:ReportsView />
</DataTemplate>
```

Add the sidebar button after "Expenses":

```xml
<Button Content="Reports" Command="{Binding NavigateToReportsCommand}"
        Style="{StaticResource MaterialDesignFlatButton}"
        Foreground="White" HorizontalContentAlignment="Left" Margin="8,2" />
```

- [ ] **Step 5: Create `ReportsView.xaml`**

```xml
<!-- SmartSolutions.App/Views/ReportsView.xaml -->
<UserControl x:Class="SmartSolutions.App.Views.ReportsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
      <StackPanel Margin="24" MaxWidth="1000">
        <TextBlock Text="Reports" Style="{StaticResource MaterialDesignHeadline5TextBlock}" Margin="0,0,0,16"/>

        <!-- Day Book Report -->
        <md:Card Padding="16" Margin="0,0,0,16">
          <StackPanel>
            <TextBlock Text="Day Book" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
              <DatePicker SelectedDate="{Binding DayBookDate}" Width="140" Margin="0,0,8,0"/>
              <Button Content="Load" Command="{Binding LoadDayBookCommand}" Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <DataGrid ItemsSource="{Binding DayBookEntries}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True" MaxHeight="240">
              <DataGrid.Columns>
                <DataGridTextColumn Header="Description" Binding="{Binding Description}" Width="*"/>
                <DataGridTextColumn Header="Amount"      Binding="{Binding Amount, StringFormat='PKR {0:#,##0.00}'}" Width="120"/>
                <DataGridTextColumn Header="Channel"     Binding="{Binding Channel}" Width="100"/>
                <DataGridTextColumn Header="Type"        Binding="{Binding Type}" Width="70"/>
              </DataGrid.Columns>
            </DataGrid>
            <StackPanel Orientation="Horizontal" Margin="0,8,0,0" HorizontalAlignment="Right">
              <TextBlock Text="{Binding DayBookIncomeTotal,  StringFormat='Total Income: PKR {0:#,##0.00}'}"  Margin="0,0,32,0"/>
              <TextBlock Text="{Binding DayBookExpenseTotal, StringFormat='Total Expenses: PKR {0:#,##0.00}'}" FontWeight="Bold"/>
            </StackPanel>
          </StackPanel>
        </md:Card>

        <!-- Outstanding Balances Report -->
        <md:Card Padding="16" Margin="0,0,0,16">
          <StackPanel>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
              <TextBlock Text="Outstanding Balances" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Width="220"/>
              <Button Content="Load" Command="{Binding LoadOutstandingCommand}" Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <DataGrid ItemsSource="{Binding OutstandingItems}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True" MaxHeight="200">
              <DataGrid.Columns>
                <DataGridTextColumn Header="Order/Job"  Binding="{Binding Label}" Width="120"/>
                <DataGridTextColumn Header="Customer"   Binding="{Binding Customer}" Width="*"/>
                <DataGridTextColumn Header="Total"      Binding="{Binding Total,   StringFormat='PKR {0:#,##0.00}'}" Width="120"/>
                <DataGridTextColumn Header="Paid"       Binding="{Binding Paid,    StringFormat='PKR {0:#,##0.00}'}" Width="120"/>
                <DataGridTextColumn Header="Balance"    Binding="{Binding Balance, StringFormat='PKR {0:#,##0.00}'}" Width="120"/>
                <DataGridTextColumn Header="Date"       Binding="{Binding Date, StringFormat=dd/MM/yyyy}" Width="100"/>
              </DataGrid.Columns>
            </DataGrid>
            <TextBlock Text="{Binding OutstandingTotal, StringFormat='Total Outstanding: PKR {0:#,##0.00}'}"
                       HorizontalAlignment="Right" FontWeight="Bold" Margin="0,8,0,0"/>
          </StackPanel>
        </md:Card>

        <!-- Monthly Summary Report -->
        <md:Card Padding="16" Margin="0,0,0,16">
          <StackPanel>
            <TextBlock Text="Monthly Summary" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
              <TextBox md:HintAssist.Hint="Month" Text="{Binding ReportMonth, UpdateSourceTrigger=PropertyChanged}" Width="70" Margin="0,0,8,0"/>
              <TextBox md:HintAssist.Hint="Year"  Text="{Binding ReportYear,  UpdateSourceTrigger=PropertyChanged}" Width="80" Margin="0,0,8,0"/>
              <Button Content="Load" Command="{Binding LoadMonthlyCommand}" Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <StackPanel Orientation="Horizontal">
              <TextBlock Text="{Binding Monthly.TotalIncome,   StringFormat='Income: PKR {0:#,##0.00}'}"   Margin="0,0,32,0" FontSize="14"/>
              <TextBlock Text="{Binding Monthly.TotalExpenses, StringFormat='Expenses: PKR {0:#,##0.00}'}" Margin="0,0,32,0" FontSize="14"/>
              <TextBlock Text="{Binding Monthly.Profit,        StringFormat='Profit: PKR {0:#,##0.00}'}"   FontSize="14" FontWeight="Bold"/>
            </StackPanel>
          </StackPanel>
        </md:Card>

        <!-- Per-Order Profit Report -->
        <md:Card Padding="16" Margin="0,0,0,16">
          <StackPanel>
            <TextBlock Text="Per-Order Profit" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
              <TextBox md:HintAssist.Hint="Order ID" Text="{Binding ProfitOrderId, UpdateSourceTrigger=PropertyChanged}" Width="100" Margin="0,0,8,0"/>
              <Button Content="Calculate" Command="{Binding LoadPerOrderProfitCommand}" Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <TextBlock Text="{Binding PerOrderProfitResult}" FontSize="14" FontWeight="Bold"/>
          </StackPanel>
        </md:Card>

        <!-- Expense Breakdown Report -->
        <md:Card Padding="16">
          <StackPanel>
            <TextBlock Text="Expense Breakdown by Category" Style="{StaticResource MaterialDesignSubtitle1TextBlock}" Margin="0,0,0,8"/>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
              <TextBox md:HintAssist.Hint="Month" Text="{Binding ExpBreakMonth, UpdateSourceTrigger=PropertyChanged}" Width="70" Margin="0,0,8,0"/>
              <TextBox md:HintAssist.Hint="Year"  Text="{Binding ExpBreakYear,  UpdateSourceTrigger=PropertyChanged}" Width="80" Margin="0,0,8,0"/>
              <Button Content="Load" Command="{Binding LoadExpenseBreakdownCommand}" Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
            <DataGrid ItemsSource="{Binding ExpenseBreakdown}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True">
              <DataGrid.Columns>
                <DataGridTextColumn Header="Category" Binding="{Binding Category}" Width="*"/>
                <DataGridTextColumn Header="Total"    Binding="{Binding Total, StringFormat='PKR {0:#,##0.00}'}" Width="150"/>
              </DataGrid.Columns>
            </DataGrid>
          </StackPanel>
        </md:Card>

      </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 6: Create `ReportsView.xaml.cs`**

```csharp
// SmartSolutions.App/Views/ReportsView.xaml.cs
using System.Windows.Controls;
namespace SmartSolutions.App.Views;
public partial class ReportsView : UserControl { public ReportsView() => InitializeComponent(); }
```

- [ ] **Step 7: Build and commit**

```powershell
dotnet build SmartSolutions.App
git add .
git commit -m "feat(app): add ReportsViewModel + ReportsView with all 5 report types"
```

---

## Phase 11: Final Integration

### Task 26: Run the app end-to-end and apply the database migration

- [ ] **Step 1: Ensure SQL Server Express is running**

Open SQL Server Configuration Manager, confirm `SQLEXPRESS` service is running.

- [ ] **Step 2: Verify connection string**

In `SmartSolutions.App/appsettings.json`, confirm the server name matches your SQL Server instance. Common values:
- `Server=.\SQLEXPRESS` — local named instance
- `Server=(localdb)\MSSQLLocalDB` — LocalDB

- [ ] **Step 3: Run the app for the first time**

Press F5 in Visual Studio 2026 (or `dotnet run --project SmartSolutions.App`).

The `OnStartup` in `App.xaml.cs` calls `Database.MigrateAsync()` automatically. This creates the `SmartSolutions` database and applies `InitialCreate`.

Expected: Main window opens, Dashboard loads, sidebar shows all nav items.

- [ ] **Step 4: Smoke test Settings**

1. Navigate to Settings
2. Add an Item Category ("Panaflex")
3. Add an Item Name ("Panaflex Frontlit") under that category
4. Add a Vendor ("Al-Noor Printing", phone optional)
5. Add a Technician ("Zahid", phone optional)
6. Save Business Info with correct name, NTN, phone

Expected: All items persist — close and reopen app, data is still there.

- [ ] **Step 5: Smoke test Print Orders**

1. Navigate to Print Orders → New Order
2. Type a customer name, press "+ Create customer"
3. Add a line item: category → item → PerSqft → Feet → 4 × 6 → Qty 2 → Rate 100
4. Verify preview total shows PKR 4,800.00
5. Add another line: PerPiece → Qty 500 → Rate 2 → verify PKR 1,000.00
6. Save header, then assign vendor, then add a payment
7. Verify balance updates correctly

Expected: Order saves, lines compute correctly, payment reduces balance.

- [ ] **Step 6: Smoke test Haier Jobs**

1. Navigate to Haier Jobs → New Job
2. Create a Warranty job with a Claim Reference Number
3. Verify Claim Reference field is visible only when Warranty is selected
4. Create an Out-of-Warranty job and add a payment

- [ ] **Step 7: Smoke test Invoice**

1. Open a saved Print Order
2. Click "Print Invoice"
3. Verify the FastReport preview opens with correct data

> **Note:** If the `.frx` template has not been created yet in the FastReport designer, the print button will fail with a file-not-found error. Create the template first (see Task 24, Step 4).

- [ ] **Step 8: Final commit**

```powershell
git add .
git commit -m "feat: complete integration — app runs end-to-end"
```

---

## Phase 12: Error Prevention and Validation (spec §9)

### Task 27: Confirmation dialogs, positive-number input, status guard, order list totals

**Files:**
- Create: `SmartSolutions.App/Helpers/DialogHelper.cs`
- Create: `SmartSolutions.App/Helpers/PositiveDecimalBehavior.cs`
- Create: `SmartSolutions.App/ViewModels/PrintOrderSummaryRow.cs`
- Modify: All ViewModels with delete commands
- Modify: `PrintOrderDetailViewModel.cs` — status guard
- Modify: `PrintOrdersViewModel.cs` and `PrintOrdersView.xaml` — computed totals in list

- [ ] **Step 1: Create `Helpers/DialogHelper.cs`**

```csharp
// SmartSolutions.App/Helpers/DialogHelper.cs
using System.Windows;

namespace SmartSolutions.App.Helpers;

public static class DialogHelper
{
    public static bool Confirm(string message, string title = "Confirm") =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
            == MessageBoxResult.Yes;
}
```

- [ ] **Step 2: Add confirmation guard to every delete command**

In each ViewModel, add `if (!DialogHelper.Confirm("...")) return;` at the top of:

| ViewModel | Command |
|-----------|---------|
| `PrintOrdersViewModel` | `DeleteOrderAsync` |
| `PrintOrderDetailViewModel` | `DeleteLineAsync`, `DeletePaymentAsync` |
| `HaierJobsViewModel` | `DeleteJobAsync` |
| `HaierJobDetailViewModel` | `DeletePaymentAsync` |
| `ExpensesViewModel` | `DeleteExpenseAsync` |
| `SettingsViewModel` | `DeleteItemCategoryAsync`, `DeleteItemNameAsync`, `DeleteVendorAsync`, `DeleteTechnicianAsync`, `DeleteExpenseCategoryAsync`, `DeletePaymentChannelAsync` |

Example for `PrintOrderDetailViewModel.DeleteLineAsync`:

```csharp
[RelayCommand]
private async Task DeleteLineAsync(PrintOrderLine line)
{
    if (!DialogHelper.Confirm("Delete this line item?")) return;
    await orders.DeleteLineAsync(line.Id);
    Lines.Remove(line);
    RefreshSummary();
}
```

- [ ] **Step 3: Create `Helpers/PositiveDecimalBehavior.cs`**

```csharp
// SmartSolutions.App/Helpers/PositiveDecimalBehavior.cs
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartSolutions.App.Helpers;

public static class PositiveDecimalBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached("Enabled", typeof(bool),
            typeof(PositiveDecimalBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        if ((bool)e.NewValue)
        {
            tb.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(tb, OnPaste);
        }
        else
        {
            tb.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(tb, OnPaste);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var tb       = (TextBox)sender;
        var proposed = tb.Text.Insert(tb.SelectionStart, e.Text);
        e.Handled = !Regex.IsMatch(proposed, @"^\d*\.?\d*$");
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string))) { e.CancelCommand(); return; }
        var text = (string)e.DataObject.GetData(typeof(string))!;
        if (!Regex.IsMatch(text, @"^\d*\.?\d*$")) e.CancelCommand();
    }
}
```

- [ ] **Step 4: Apply `PositiveDecimalBehavior.Enabled="True"` in XAML**

Add `xmlns:h="clr-namespace:SmartSolutions.App.Helpers"` to the top of each View that has decimal inputs, then add `h:PositiveDecimalBehavior.Enabled="True"` to these TextBoxes:

- `PrintOrderDetailView.xaml`: NewLineHeight, NewLineWidth, NewLineRate, TransportationCharges, NewPaymentAmount, VendorCost
- `HaierJobDetailView.xaml`: PartsCost, NewPaymentAmount
- `ExpensesView.xaml`: NewAmount

Example:
```xml
<TextBox h:PositiveDecimalBehavior.Enabled="True"
         md:HintAssist.Hint="Amount *"
         Text="{Binding NewPaymentAmount, UpdateSourceTrigger=PropertyChanged}" ... />
```

- [ ] **Step 5: Add "SentToVendor" status guard in `PrintOrderDetailViewModel.SaveOrderAsync`**

```csharp
[RelayCommand]
private async Task SaveOrderAsync()
{
    if (SelectedCustomer is null) { ErrorMessage = "Customer is required."; return; }
    if (OrderStatus == PrintOrderStatus.SentToVendor && VendorAssignment is null)
    {
        ErrorMessage = "Cannot set status to 'Sent to Vendor' without a vendor assignment.";
        return;
    }
    ErrorMessage = "";
    // ... rest of save logic unchanged
}
```

- [ ] **Step 6: Create `ViewModels/PrintOrderSummaryRow.cs`**

```csharp
// SmartSolutions.App/ViewModels/PrintOrderSummaryRow.cs
using SmartSolutions.Data.Entities;

namespace SmartSolutions.App.ViewModels;

public class PrintOrderSummaryRow(PrintOrder order)
{
    public PrintOrder Order      { get; } = order;
    public int        Id         => Order.Id;
    public string     Customer   => Order.Customer.Name;
    public string     DateStr    => Order.Date.ToLocalTime().ToString("dd/MM/yyyy");
    public PrintOrderStatus Status => Order.Status;
    public decimal    Total      => Order.Lines.Sum(l => l.ComputeTotal())
                                    + (Order.TransportationCharges ?? 0);
    public decimal    Paid       => Order.Payments.Sum(p => p.Amount);
    public decimal    Balance    => Total - Paid;
    public string?    ExpectedDate =>
        Order.VendorAssignments.FirstOrDefault()?.ExpectedDate?.ToLocalTime().ToString("dd/MM/yyyy");
}
```

- [ ] **Step 7: Update `PrintOrdersViewModel` to use `PrintOrderSummaryRow`**

```csharp
// Change the ObservableCollection type:
[ObservableProperty] private ObservableCollection<PrintOrderSummaryRow> _orderList = [];

// In LoadAsync, change the assignment:
OrderList = new(results.Select(o => new PrintOrderSummaryRow(o)));
```

Update `DeleteOrderAsync` and `OpenOrderCommand` to access `.Order`:

```csharp
[RelayCommand]
private async Task DeleteOrderAsync(PrintOrderSummaryRow row)
{
    if (!DialogHelper.Confirm($"Delete Print Order #{row.Id}?")) return;
    await orders.DeleteOrderAsync(row.Order.Id);
    OrderList.Remove(row);
}

[RelayCommand]
private void OpenOrder(PrintOrderSummaryRow row)
{
    var vm = services.GetRequiredService<PrintOrderDetailViewModel>();
    vm.InitEdit(row.Order.Id);
    NavigateTo(vm);
}
```

- [ ] **Step 8: Update `PrintOrdersView.xaml` DataGrid columns**

Replace the current Columns section with:

```xml
<DataGrid.Columns>
    <DataGridTextColumn Header="Order #"  Binding="{Binding Id}" Width="80"/>
    <DataGridTextColumn Header="Date"     Binding="{Binding DateStr}" Width="100"/>
    <DataGridTextColumn Header="Customer" Binding="{Binding Customer}" Width="*"/>
    <DataGridTextColumn Header="Total"    Binding="{Binding Total,   StringFormat='PKR {0:#,##0.00}'}" Width="130"/>
    <DataGridTextColumn Header="Paid"     Binding="{Binding Paid,    StringFormat='PKR {0:#,##0.00}'}" Width="110"/>
    <DataGridTextColumn Header="Balance"  Binding="{Binding Balance, StringFormat='PKR {0:#,##0.00}'}" Width="110"/>
    <DataGridTextColumn Header="Status"   Binding="{Binding Status}" Width="120"/>
    <DataGridTextColumn Header="Expected" Binding="{Binding ExpectedDate}" Width="110"/>
    <DataGridTemplateColumn Header="Actions" Width="160">
        <DataGridTemplateColumn.CellTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal">
                    <Button Content="Open"   Command="{Binding DataContext.OpenOrderCommand,  RelativeSource={RelativeSource AncestorType=DataGrid}}"
                            CommandParameter="{Binding}" Style="{StaticResource MaterialDesignFlatButton}" Margin="0,0,4,0"/>
                    <Button Content="Delete" Command="{Binding DataContext.DeleteOrderCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                            CommandParameter="{Binding}" Style="{StaticResource MaterialDesignFlatButton}"
                            Foreground="{DynamicResource MaterialDesignValidationErrorBrush}"/>
                </StackPanel>
            </DataTemplate>
        </DataGridTemplateColumn.CellTemplate>
    </DataGridTemplateColumn>
</DataGrid.Columns>
```

- [ ] **Step 9: Build full solution and run tests**

```powershell
dotnet build SmartSolutions.sln
dotnet test SmartSolutions.Tests -v n
```

Expected: `Build succeeded. 0 Error(s)` and all tests pass.

- [ ] **Step 10: Commit**

```powershell
git add .
git commit -m "feat(app): add delete confirmations, positive-decimal inputs, status guard, order list computed totals"
```

---
