# Smart Solutions — Project Context

> **For Claude:** Read this file at the start of each session to restore full project context without re-exploring the codebase. Update the Session Log at the end of each session.

---

## App Overview

Smart Solutions is a production WPF desktop application (.NET 10, C#) for a printing and Haier AC after-sales service business in Peshawar, Pakistan. It replaces manual Excel tracking with validated data entry, PDF invoice generation, financial reporting, and a full audit trail. Deployed via MSIX to multiple Windows PCs on a LAN, all connecting to a shared SQL Server.

**Tech stack:** WPF + XAML · CommunityToolkit.Mvvm · EF Core 10 · SQL Server Express 2022 · MaterialDesignInXamlToolkit · FastReport Community · MSIX

**Solution:** `SmartSolutions/SmartSolutions.slnx` (submodule at `SmartSolutions/`)

---

## Solution Structure

```
SmartSolutions.Data/        EF Core DbContext, 17 entities, migrations
SmartSolutions.Core/        8 services, 10 interfaces, all business logic
SmartSolutions.App/         19 ViewModels, 17 Views, WPF XAML, FastReport templates
SmartSolutions.Tests/       28 unit tests, xUnit + NSubstitute + FluentAssertions
```

Key file locations within `SmartSolutions/SmartSolutions.App/`:
- Views: `Views/*.xaml` and `Views/Steps/*.xaml` (wizard steps)
- ViewModels: `ViewModels/*.cs`
- Converters: `Converters/`
- Config/settings: `Helpers/SettingsManager.cs`
- DI wiring: `ServiceConfiguration.cs`
- Startup: `App.xaml.cs`

---

## Entities (17 tables)

| Domain | Entities |
|--------|---------|
| Auth | `AppUser` |
| Business Info | `BusinessInfo` (singleton, Id=1) |
| Print Business | `Customer`, `PrintOrder`, `PrintOrderLine`, `PrintOrderPayment`, `PrintOrderVendorAssignment`, `Vendor` |
| Haier AC | `HaierJob`, `HaierJobPayment`, `Technician` |
| Shared Reference | `ItemCategory`, `ItemName`, `ExpenseCategory`, `PaymentChannel`, `Expense` |

**Enums:** `PrintOrderStatus`, `HaierJobType`, `HaierJobStatus`, `RateType` (PerSqft/PerPiece), `DimensionUnit` (Feet/Inches)

---

## Services (8)

| Service | Responsibility |
|---------|---------------|
| `AuthService` | PBKDF2-SHA256 PIN hashing (100k iterations), user management |
| `SessionService` | **Singleton.** Holds logged-in user for process lifetime; used for audit stamping |
| `LookupService` | CRUD for all reference data (items, vendors, technicians, channels, categories) |
| `CustomerService` | Customer CRUD |
| `PrintOrderService` | Print orders, lines, payments, vendor assignments |
| `HaierJobService` | Haier jobs and payments |
| `ExpenseService` | Expense tracking |
| `DashboardService` | KPI summaries for dashboard cards |

All services use `IDbContextFactory<AppDbContext>` — never a shared singleton DbContext.

---

## Key Decisions (non-obvious — must know before touching code)

- **Nothing hardcoded** — all lookup data (items, vendors, technicians, channels, categories) is user-managed via sidebar pages. No enums or string constants for user-facing data.
- **Computed totals** — `Total` is never stored in the DB. `PerSqft: H × W × Qty × Rate` (÷144 if inches). `PerPiece: Qty × Rate`.
- **Partial payments** — balance = total invoiced − sum of payments. No single paid/unpaid boolean.
- **Auth = accountability only** — no roles, no permission checks. Auth solely stamps `CreatedById`/`RecordedById` on every record.
- **MSIX + LocalAppData config** — first-run wizard writes connection string + seed data to `%LOCALAPPDATA%\SmartSolutions\appsettings.json`. `ServiceConfiguration.cs` clears all other config sources and loads exclusively from that path.
- **`IDbContextFactory` pattern** — every service receives `IDbContextFactory<AppDbContext>` via DI, never a singleton.
- **`Mode=OneWay` on computed bool bindings** — computed read-only bool properties (e.g. `UseSqlAuth`, `IsDialogOpen`) need explicit `Mode=OneWay` on WPF bindings or WPF throws `InvalidOperationException` on window load. This has bitten us twice — always check this for new computed properties.

---

## Current Status

### Fully Implemented ✓
- First-run setup wizard (3-step: DB connection → business info → admin PIN)
- MSIX packaging with self-signed certificate
- Login window (PIN-based auth, PBKDF2-SHA256, `LoginWindow` → `MainWindow`)
- Main navigation shell (sidebar, opens maximized)
- Dashboard (KPI summary cards)
- Print Orders — list, detail, lines (per-sqft + per-piece), payments, vendor assignment
- Haier Jobs — list, detail, payments, technician assignment
- Expenses — list, add/edit/delete with categories and payment channels
- Reports view (basic financial summaries)
- Customers management page (add/edit/delete, address + notes)
- Items management page (categories left panel + item names right panel)
- Vendors management page
- Technicians management page
- Expense Categories management page
- Payment Channels management page
- Users management page (add user, reset PIN, deactivate/reactivate)
- Settings page (business info: name, NTN, address, phone)
- Audit trail (`CreatedById`/`RecordedById` on all records, nullable for existing rows)
- 28/28 unit tests passing

### Known Limitations (deferred, not bugs)
- Dashboard balance cards hardcode "Cash", "Easypaisa", "Bank" channel names in `DashboardService` — renaming these channels in Settings breaks the cards
- `ReportsViewModel` queries EF directly (bypasses service layer) for some report queries
- Invoice `.frx` FastReport template is a minimal stub — needs FastReport Designer work for production letterhead

### Open Items (confirm with owner before implementing)
- Haier job fields — may need additional fields specific to Haier's warranty system
- Transportation charges — fixed fee, per-order, or calculated separately?
- Invoice serial number — auto-increment or manual entry?
- PDF invoice styling — production-quality letterhead with logo

---

## Sidebar Navigation Order

Dashboard → Print Orders → Haier Jobs → Expenses → Reports → [separator] → Customers → Items → Vendors → Technicians → Expense Categories → Payment Channels → Users → [separator] → Settings

---

## Session Log

```
2026-06-10 — README.md and PROJECT-CONTEXT.md created for GitHub portfolio; screenshots committed
2026-06-10 — Startup crash fixed (DatabaseStepControl.xaml: UseSqlAuth needed Mode=OneWay)
2026-06-10 — MSIX packaging + first-run setup wizard complete; 28/28 tests passing
2026-06-10 — Auth feature complete (LoginWindow, SessionService, PBKDF2-SHA256 hashing, audit trail FKs)
2026-06-10 — Users page bugfix (IsDialogOpen computed prop needed Mode=OneWay on md:DialogHost)
2026-06-10 — Dedicated management pages complete (Items, Vendors, Technicians, Expense Categories, Payment Channels, Users, Settings)
2026-06-10 — Customers page complete (CustomerService with FK-safe delete, 6 new tests)
2026-06-09 — Base implementation (Print Orders, Haier Jobs, Expenses, Reports, Dashboard, all entities, 2 migrations)
```
