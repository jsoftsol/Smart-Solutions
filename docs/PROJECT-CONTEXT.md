# Smart Solutions — Project Context

> **For Claude:** Read this file at the start of each session to restore full project context without re-exploring the codebase. For product requirements and detailed current status, see `docs/PRD.md`. Update the Session Log at the end of each session when the user says "save progress" (see `CLAUDE.md` → "Session Workflow — Saving Progress").

---

## App Overview

Smart Solutions is a production WPF desktop application (.NET 10, C#) for a printing and Haier AC after-sales service business in Peshawar, Pakistan. It replaces manual Excel tracking with validated data entry, PDF invoice generation, financial reporting, and a full audit trail. Deployed via MSIX to multiple Windows PCs on a LAN, all connecting to a shared SQL Server.

**Tech stack:** WPF + XAML · CommunityToolkit.Mvvm · EF Core 10 · SQL Server Express 2022 · MaterialDesignInXamlToolkit · FastReport Community · MSIX

**Solution:** `SmartSolutions/SmartSolutions.slnx` (submodule at `SmartSolutions/`)

---

## Solution Structure

```
SmartSolutions.Data/        EF Core DbContext, 16 entities, migrations
SmartSolutions.Core/        8 services, 10 interfaces, all business logic
SmartSolutions.App/         19 ViewModels, 17 Views, WPF XAML, FastReport templates
SmartSolutions.Tests/       35 unit tests, xUnit + NSubstitute + FluentAssertions
```

(Counts verified against code on 2026-08-11 — re-verify before trusting on older reads.)

Key file locations within `SmartSolutions/SmartSolutions.App/`:
- Views: `Views/*.xaml` and `Views/Steps/*.xaml` (wizard steps)
- ViewModels: `ViewModels/*.cs`
- Converters: `Converters/`
- Config/settings: `Helpers/SettingsManager.cs`
- DI wiring: `ServiceConfiguration.cs`
- Startup: `App.xaml.cs`

---

## Entities (16 tables)

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
- 35/35 unit tests passing (verified 2026-08-11)

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
2026-08-11 — Auto-memory restored: user asked why memory wasn't loading automatically, then asked for it back after being told the local-only policy had traded it away. Found the actual mechanism instead of re-accepting the tradeoff: the harness's `autoMemoryDirectory` setting is only ignored when set in the checked-in `.claude/settings.json` (a security guard) — it is honored from `.claude/settings.local.json` (gitignored, machine-local). Set `autoMemoryEnabled: true` and `autoMemoryDirectory` to this repo's `memory/` folder in settings.local.json, giving local-only storage AND session-start auto-load with no tradeoff. Updated CLAUDE.md, memory/MEMORY.md, and memory/feedback_memory_location.md to reflect the corrected policy. No source code changes; re-ran dotnet test to confirm 35/35 still passing.
2026-08-11 — Memory consolidation: at the user's request, moved memory storage from dual-write (project-local memory/ + harness global auto-memory folder) to project-local only. Merged the global folder's content (which had a more detailed session history) into the local memory/ files, then deleted the global folder. Updated CLAUDE.md's "Session Workflow — Saving Progress" step 3 and the memory/feedback_memory_location.md and memory/feedback_save_progress_workflow.md memories to record the new policy and its tradeoff: the harness only auto-loads memory from the global folder at session start, so future sessions won't have memory auto-loaded — it has to be checked explicitly in the project's memory/ folder. No source code changes; re-ran dotnet test to confirm 35/35 still passing. CLAUDE.md change not yet committed.
2026-08-11 — Set GitHub repo "About" panel (jsoftsol/Smart-Solutions): description summarizing the app, and 13 topics covering tech stack + domain (wpf, csharp, dotnet, entity-framework-core, sql-server, mvvm, material-design, msix, desktop-application, xaml, invoicing, business-management, crud-application). No AI-tooling topics, deliberately, matching the README reframing. No code changes this pass; working tree already clean and in sync with origin.
2026-08-11 — Finished AdminPinStep_Loaded stub: renamed to AdminPinStep_IsVisibleChanged, wired to IsVisibleChanged (not Loaded, which only fires once at startup since wizard steps stay in the tree), added AdminPinStepControl.FocusPinInput(). Reframed README.md to drop "vibe coding"/"built entirely with Claude Code" language (recruiter-facing concern) in favor of "AI-assisted engineering workflow" with an expanded 6-step process (added Testing, Deployment); fixed stale entity/test counts (17/28 → 16/35) and repointed PRD link to docs/PRD.md. Rewrote all 22 repo commits via git filter-branch to strip Co-Authored-By: Claude trailers (Claude never force-pushes master — user ran `git push --force-with-lease origin master` themselves); verified on GitHub via API that master HEAD (83ef50c) and all 22 commits are clean of the trailer, single-authored. Established standing preference: no Claude co-author attribution in this repo's commits going forward (memory: feedback-no-claude-coauthor, both locations). Working tree clean, 35/35 tests passing, all pushed.
2026-08-11 — Documentation consolidation: created docs/PRD.md (merges old spec + this file's status into one maintained PRD), marked docs/superpowers/specs/2026-06-09-smart-solutions-design.md as superseded/historical, added "Session Workflow — Saving Progress" section to CLAUDE.md (manual trigger: user says "save progress"), synced memory/ (local + global) with the new workflow. Verified against code: 16 entities, 8 services, 35/35 tests passing, build clean. Noted uncommitted WIP in SetupWizardWindow.xaml/.xaml.cs (header text color White→Black, empty AdminPinStep_Loaded handler stub — not finished).
2026-06-10 — GitHub portfolio setup: README.md, PROJECT-CONTEXT.md, screenshots; submodule flattened into single repo; pushed to https://github.com/jsoftsol/Smart-Solutions
2026-06-10 — Startup crash fixed (DatabaseStepControl.xaml: UseSqlAuth needed Mode=OneWay)
2026-06-10 — MSIX packaging + first-run setup wizard complete; 28/28 tests passing
2026-06-10 — Auth feature complete (LoginWindow, SessionService, PBKDF2-SHA256 hashing, audit trail FKs)
2026-06-10 — Users page bugfix (IsDialogOpen computed prop needed Mode=OneWay on md:DialogHost)
2026-06-10 — Dedicated management pages complete (Items, Vendors, Technicians, Expense Categories, Payment Channels, Users, Settings)
2026-06-10 — Customers page complete (CustomerService with FK-safe delete, 6 new tests)
2026-06-09 — Base implementation (Print Orders, Haier Jobs, Expenses, Reports, Dashboard, all entities, 2 migrations)
```
