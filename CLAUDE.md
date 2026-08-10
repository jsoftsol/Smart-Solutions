# Smart Solutions — Record Keeping App

## Project Overview

WPF desktop application for Smart Solutions, Peshawar (NTN: 7569020-2) — a printing and Haier AC after-sales service business. Replaces manual Excel tracking with validated, guided data entry, PDF invoice generation, and financial reporting.

**Full PRD:** `docs/PRD.md` — requirements, data model, and current implementation status (single source of truth; older `docs/superpowers/specs/2026-06-09-smart-solutions-design.md` is superseded/historical)
**Session context / "what's the code state right now":** `docs/PROJECT-CONTEXT.md`
**Auth & Startup Design:** `docs/superpowers/specs/2026-06-10-auth-startup-design.md`

---

## Tech Stack

| Component | Choice |
|-----------|--------|
| Runtime | .NET 10 |
| Language | C# |
| UI Framework | WPF (XAML) |
| MVVM | CommunityToolkit.Mvvm |
| Database | SQL Server Express 2022 (central LAN server) |
| ORM | Entity Framework Core 10 (code-first, migrations) |
| UI Theme | MaterialDesignInXamlToolkit |
| PDF & Print | FastReport Community |
| IDE | Visual Studio 2026 |

---

## Deployment

- Distributed as `.msix` (single-project MSIX via `<WindowsPackageType>MSIX</WindowsPackageType>` in the `.csproj`).
- Self-signed cert (`SmartSolutions.pfx` / `.cer`) in `SmartSolutions.App/`. The `.pfx` is gitignored (private key); `.cer` is committed (public, safe to distribute).
- On first launch: `SettingsManager.IsSetupRequired()` detects missing `%LOCALAPPDATA%\SmartSolutions\appsettings.json` → shows `SetupWizardWindow` before building the DI host.
- Wizard writes the connection string + `FirstRunData` to LocalAppData. After migrations, `App.xaml.cs` seeds `BusinessInfo` (row Id=1) and admin PIN from `FirstRunData`, then calls `ClearFirstRunData()`.
- `ServiceConfiguration.cs` clears default config sources and loads exclusively from `SettingsManager.SettingsFilePath`.
- See `docs/INSTALL.md` for per-PC certificate install steps.

---

## Solution Structure

```
SmartSolutions/
├── SmartSolutions.Data/          # EF Core DbContext, entities, migrations
│   ├── Entities/
│   ├── Migrations/
│   └── AppDbContext.cs
├── SmartSolutions.Core/          # Business logic, services, interfaces
│   ├── Services/
│   └── Interfaces/
├── SmartSolutions.App/           # WPF app — Views, ViewModels, Controls
│   ├── Views/
│   │   ├── ItemsView.xaml              # Items management (categories + item names)
│   │   ├── VendorsView.xaml            # Vendors management
│   │   ├── TechniciansView.xaml        # Technicians management
│   │   ├── ExpenseCategoriesView.xaml  # Expense categories management
│   │   ├── PaymentChannelsView.xaml    # Payment channels management
│   │   ├── UsersView.xaml              # User accounts management
│   │   ├── SettingsView.xaml           # Business Info only
│   │   └── ...
│   ├── ViewModels/
│   │   ├── ItemsViewModel.cs
│   │   ├── VendorsViewModel.cs
│   │   ├── TechniciansViewModel.cs
│   │   ├── ExpenseCategoriesViewModel.cs
│   │   ├── PaymentChannelsViewModel.cs
│   │   ├── UsersViewModel.cs
│   │   ├── SettingsViewModel.cs        # Business Info only
│   │   └── ...
│   ├── Controls/                 # Reusable UserControls
│   ├── Converters/
│   ├── Reports/                  # FastReport .frx templates
│   └── appsettings.json          # Connection string (not hardcoded)
└── SmartSolutions.sln
```

---

## Architecture Rules

### MVVM — strictly enforced
- No business logic in code-behind. Code-behind is for UI-only concerns only (focus, animations, scroll behavior).
- Every View has exactly one ViewModel.
- ViewModels are injected with services via constructor.
- Use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit — no manual INotifyPropertyChanged boilerplate.

### Services
- All data access goes through service classes in `SmartSolutions.Core/Services/`.
- Services receive `IDbContextFactory<AppDbContext>` — never a shared singleton DbContext.
- `ISessionService` is the one **singleton** exception — it holds the in-memory logged-in user for the current process lifetime. Inject it wherever `CreatedById` / `RecordedById` must be stamped.
- No other static state. No service locators.

### Async
- All database calls are async (`await`).
- No `.Result` or `.Wait()` on Tasks anywhere.
- ViewModels use `async Task` commands via `[RelayCommand]`.

---

## Key Decisions — Do Not Change Without Discussion

### Nothing hardcoded
All lookup data lives in the database and is managed by the user via dedicated sidebar pages:
- Item categories and item names → **Items page**
- Vendors → **Vendors page**
- Technicians → **Technicians page**
- Expense categories → **Expense Categories page**
- Payment channels (Cash, Easypaisa, Bank are defaults — not fixed) → **Payment Channels page**
- Business info (name, NTN, address, phone — used on invoices) → **Settings page**
- User accounts → **Users page**

Do NOT add string constants, enum values, or hardcoded lists for anything the user interacts with.

### Computed totals

Two rate types exist per line item:
- **PerSqft:** `Total = Height × Width × Quantity × Rate` (if unit is Inches, convert to ft² first: divide H×W by 144)
- **PerPiece:** `Total = Quantity × Rate` (Height and Width are null/hidden)

Total is never stored in the database — computed in queries or ViewModels. Do not add a `Total` column to `PrintOrderLines`.

### Multi-PC deployment
Connection string is in `appsettings.json` on each PC. Do not hardcode a server name, IP, or database name anywhere in code.

### Authentication — accountability only, no permissions
Users log in with username + PIN once per app launch. The session lasts until the app is closed. All users have equal access — there are no roles or permission checks. The sole purpose of auth is stamping `CreatedById` / `RecordedById` on every record so the owner can see who entered what.

- Default user seeded: username `admin`, PIN `0000` (owner should change after first launch)
- `LoginWindow` is shown before `MainWindow` at startup; closing it without logging in exits the app
- `MainWindow` opens with `WindowState = Maximized`
- User management (add / reset PIN / deactivate) lives in the **Users page** (sidebar), not Settings

### Partial payments
Print orders and Haier jobs collect payments over time. Balance = Total Invoiced − Sum(Payments). There is no single "paid/unpaid" boolean — always derive payment status from the payment collection.

---

## Coding Conventions

- **Naming:** ViewModels → `*ViewModel.cs`, Views → `*View.xaml`, Services → `*Service.cs`
- **One ViewModel per View** — no shared ViewModels across multiple views
- **No magic strings** — use `nameof()` for property names, constants for route/key strings
- **English only** — no Urdu strings in code, XAML, or resources
- **Currency** — always PKR; format as `PKR #,##0.00`
- **Dates** — store as UTC in the database; display in local time (Pakistan Standard Time, UTC+5)
- **EF Migrations** — always use `dotnet ef migrations add <Name>` for schema changes; never modify the DB manually

---

## Error Prevention — Design Principles

These are requirements, not optional:
- Dropdowns for all structured fields (item names, vendors, technicians, expense categories, channels)
- Required fields block save and highlight red
- Amount and dimension fields enforce positive decimal values at the input level
- Totals are computed — never typed
- Delete actions require a confirmation dialog
- Duplicate payment detection (same order + amount + date → warning)

---

## Open Items (confirm with owner before implementing)

See `docs/PRD.md` Section 14 ("Current Implementation Status → Open Items") for the current list — kept there as the single copy so it doesn't drift out of sync with this file.

---

## Session Workflow — Saving Progress

The user triggers documentation/memory updates manually by saying **"save progress"** (or a clear equivalent, e.g. "wrap up this session"). There is no automated hook for this — do not assume it happens on every commit or every session end. When it's triggered, do this update pass:

1. **`docs/PRD.md`** — update Section 14 (Current Implementation Status): move newly finished work into "Fully Implemented", update "In Progress / Uncommitted" from actual `git status`/`git diff`, and refresh "Verified Metrics" (entity/service counts, test pass count — re-run `dotnet test` rather than assuming). Add a row to the Section 13 Design Decisions Log for any non-obvious decision made this session.
2. **`docs/PROJECT-CONTEXT.md`** — append a dated entry to the Session Log at the bottom describing what happened this session (features built, bugs fixed, commits made).
3. **Memory (project-local only — see `feedback-memory-location` memory in `memory/`)** — update the project-local `memory/` folder in this repo (untracked in git). Do not write to the harness's global auto-memory folder outside the project — the user opted out of that on 2026-08-11, trading away session-start auto-load for keeping everything under this directory. Update the relevant `project_*` memory file with current status; add a new `feedback_*` memory only if the user gave new durable guidance this session. Keep `memory/MEMORY.md` as a short index, not a content dump.
4. Do not invent progress — base every update on what actually happened this session (commits made, code read, tests run), not on assumptions.
