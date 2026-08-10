# Smart Solutions — Record Keeping App
## Product Requirements Document (PRD)

**Version:** 2.1 (consolidated)
**Date:** 2026-08-11
**Supersedes:** `docs/superpowers/specs/2026-06-09-smart-solutions-design.md` (kept as historical record — see banner at top of that file)
**Status verified against code:** 2026-08-11 — build clean, 35/35 tests passing, working tree clean (all WIP committed and pushed)

> **For Claude:** This is the single source of truth for product requirements. For "what's the state of the code right now / where do I start reading" instead, see `docs/PROJECT-CONTEXT.md`. When the user says **"save progress"**, update Section 14 (Current Implementation Status) and the Design Decisions Log (Section 13) here — see the workflow documented in `CLAUDE.md`.

---

## 1. Overview

Desktop record-keeping application for **Smart Solutions**, Peshawar, Pakistan (NTN: 7569020-2). The business operates two service lines:

1. **Printing** — all types of printing: Panaflex, business cards, flyers, stationery, binding, etc. All jobs are outsourced to vendors.
2. **Haier AC After-Sales Service** — warranty and out-of-warranty AC installation and repair.

**Problem being solved:** The owner and two data-entry boys currently track everything in Excel. The boys make frequent errors — wrong amounts, missing fields, duplicate entries, wrong item names. The app replaces Excel with validated, guided data entry that makes mistakes hard to make.

---

## 2. Goals

- Track the full print order lifecycle: customer → order → vendor → delivery → payment
- Track Haier AC service jobs (warranty and out-of-warranty) with customer payment collection
- Record all business expenses (shared across both lines)
- Generate and print professional PDF invoices matching the Smart Solutions letterhead
- Provide daily, per-order, outstanding-balance, and monthly financial views
- Prevent data-entry mistakes through dropdowns, validation, and computed fields

---

## 3. Non-Goals (v1)

- No mobile or web interface
- No remote/cloud access (LAN only)
- No Urdu language UI (English only; users write in Roman/English)
- No role-based permissions — all three users have equal access (auth exists for accountability/audit only)
- No automated database backup

---

## 4. Users

| User | Count | Role |
|------|-------|------|
| Owner | 1 | Reviews reports, manages settings, oversees all records |
| Data Entry Boys | 2 | Create orders/jobs, record payments, update statuses |

---

## 5. Tech Stack

| Component | Choice |
|-----------|--------|
| Runtime | .NET 10 |
| Language | C# |
| UI Framework | WPF (XAML) |
| MVVM Library | CommunityToolkit.Mvvm |
| Database | SQL Server Express 2022 (central LAN server) |
| ORM | Entity Framework Core 10 (code-first, migrations) |
| UI Theme | MaterialDesignInXamlToolkit |
| PDF & Printing | FastReport Community |
| IDE | Visual Studio 2026 |

---

## 6. Deployment

- The app is distributed as a sideloadable `.msix` package signed with a self-signed certificate.
- Each PC must have the `SmartSolutions.cer` certificate installed to the Trusted Root store before installing (one-time, see `docs/INSTALL.md`).
- On first launch, a **Setup Wizard** runs before the login screen. It collects: SQL Server connection details, business information (name, NTN, address, phone — used on invoices), and a new admin PIN.
- The connection string is written to `%LOCALAPPDATA%\SmartSolutions\appsettings.json` on each PC. The MSIX install directory is read-only; this path is always writable.
- To reconfigure the database on a PC, delete `%LOCALAPPDATA%\SmartSolutions\appsettings.json` and relaunch.
- No internet connection required.
- App runs on Windows 10 (build 17763+) and Windows 11.

---

## 7. Data Model

### 7.1 Lookup Tables (all user-managed via dedicated sidebar pages — nothing hardcoded)

| Table | Key Fields | Managed Via |
|-------|-----------|-------------|
| ItemCategory | Id, Name | Items page |
| ItemName | Id, Name, CategoryId | Items page |
| Vendor | Id, Name, Phone, Notes | Vendors page |
| Technician | Id, Name, Phone | Technicians page |
| ExpenseCategory | Id, Name | Expense Categories page |
| PaymentChannel | Id, Name (initial defaults: Cash, Easypaisa, Bank) | Payment Channels page |

### 7.2 Customer

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| Name | string | Required |
| Phone | string | |
| Address | string | |
| Notes | string | |

### 7.3 Print Order

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK, auto-increment — also used as the Invoice Serial Number |
| CustomerId | FK | Required |
| Date | date | Required |
| Status | enum | Draft, Confirmed, SentToVendor, Ready, Delivered |
| TransportationCharges | decimal? | Optional; null or 0 = omitted from invoice |
| Notes | string | |

### 7.4 Print Order Line Item

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| OrderId | FK | |
| ItemNameId | FK | Selected from managed list |
| RateType | enum | PerSqft, PerPiece |
| Unit | enum | Feet, Inches — only relevant when RateType = PerSqft |
| Height | decimal? | Null when RateType = PerPiece |
| Width | decimal? | Null when RateType = PerPiece |
| Quantity | int | |
| Rate | decimal | Per sqft (if PerSqft) or per piece (if PerPiece) |
| Total | computed | PerSqft: H×W×Qty×Rate (inches auto-converted to ft²); PerPiece: Qty×Rate (never stored) |

### 7.5 Print Order Vendor Assignment

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| OrderId | FK | |
| VendorId | FK | Selected from managed list |
| SentDate | date | |
| ExpectedDate | date | |
| VendorCost | decimal | What vendor will charge |
| VendorPaid | bool | |
| VendorPaidDate | date? | Null until paid |

### 7.6 Print Order Payment

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| OrderId | FK | |
| Amount | decimal | Must be > 0 |
| ChannelId | FK | Cash / Easypaisa / Bank |
| Date | date | |
| Notes | string | |

### 7.7 Haier Job

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| CustomerId | FK | |
| AcModel | string | |
| AcSerial | string | |
| ProblemDescription | string | |
| TechnicianId | FK | Selected from managed list |
| JobType | enum | Warranty, OutOfWarranty |
| Status | enum | Pending, InProgress, Completed |
| ClaimReferenceNumber | string? | Warranty jobs only — Haier's claim ref for reimbursement tracking |
| PartsUsed | string | Free text description |
| PartsCost | decimal | |
| Date | date | |
| Notes | string | |

### 7.8 Haier Job Payment

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| JobId | FK | |
| Amount | decimal | Must be > 0 |
| ChannelId | FK | |
| Date | date | |
| Notes | string | |

### 7.9 Expense

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK |
| CategoryId | FK | From user-managed list |
| Description | string | |
| Amount | decimal | Must be > 0 |
| ChannelId | FK | |
| Date | date | |

### 7.10 BusinessInfo & AppUser

| Table | Notes |
|-------|-------|
| BusinessInfo | Singleton row (Id=1). Name, NTN, address, phone — used on invoices and set via the Settings page / first-run wizard. |
| AppUser | Username (unique), PinHash (PBKDF2-SHA256, 100k iterations), IsActive. `CreatedById`/`RecordedById` FKs on transactional entities reference this table. |

---

## 8. Modules & Features

### 8.1 Dashboard

| Section | Content |
|---------|---------|
| Day Book | Today's received payments (printing + Haier) and paid expenses |
| Balance Cards | Live totals: Cash in Hand, Easypaisa, Bank |
| Outstanding | Orders and jobs with unpaid or partially-paid balances |
| Monthly Snapshot | Income vs. Outgoing vs. Profit for current month |

### 8.2 Printing Orders

**Create / Edit Order:**
1. Select existing customer or create new one inline
2. Add line items:
   - Select item category → then item name (cascading dropdowns)
   - Select Rate Type: **Per Sqft** or **Per Piece**
   - Per Sqft: enter unit (feet/inches), height, width, quantity, rate → total auto-computed: H×W×Qty×Rate (inches converted to ft²)
   - Per Piece: enter quantity and rate → total auto-computed: Qty×Rate (dimensions hidden)
   - Multiple line items per order
3. Optional: enter Transportation Charges (appears on invoice only if > 0)
4. Assign vendor: select from list, enter sent date, expected date, vendor cost
5. Set order status (defaults to Draft on creation)

**Customer Payments:**
- Record payment at any time (advance or balance)
- Each payment: amount, channel, date, optional notes
- Running balance shown: Total Invoiced − Total Received

**Vendor Payment:**
- Mark vendor as paid with date

**Invoice PDF:**
- Generate from any order
- Print to attached printer
- Matches Smart Solutions letterhead (see Section 11)

**List View:**
- Columns: Order #, Date, Customer, Total, Paid, Balance, Status, Expected Date
- Filters: status, customer, date range, outstanding only

**Order Status Pipeline:**
Draft → Confirmed → Sent to Vendor → Ready → Delivered

### 8.3 Haier AC Service

**Create / Edit Job:**
- Select or create customer
- Enter AC model, serial number
- Describe the problem
- Assign technician (dropdown)
- Select job type: Warranty or Out-of-Warranty
- If Warranty: enter Haier Claim Reference Number (optional until claim is submitted)
- Enter parts used (free text) and parts cost
- Set job status

**Payments (Out-of-Warranty):**
- Same payment flow as printing orders (amount, channel, date)

**Warranty Jobs:**
- Track job for Haier reimbursement claim (no customer payment)

**List View:**
- Columns: Job #, Date, Customer, AC Model, Technician, Type, Status, Balance
- Filters: status, job type, technician, date range

### 8.4 Expenses

- Add expense: category (dropdown), description, amount, channel, date
- List view: filter by category, date range
- Monthly total per category visible in list header

### 8.5 Settings

Settings contains only:

| Section | Managed Items |
|---------|--------------|
| Business Info | Name, NTN, address, phones, email, logo (used on invoices) |

> **Note:** Database connection string is configured via the first-run Setup Wizard (stored in `%LOCALAPPDATA%\SmartSolutions\appsettings.json`). It is not editable from within the app after first run. To reconfigure, delete the file and relaunch.

All lookup table management (items, vendors, technicians, expense categories, payment channels, users) has moved to dedicated pages accessible from the sidebar.

### 8.6 User Authentication

- Users log in with username + PIN once per app launch
- Session lasts until the app is closed — no auto-lock
- All users have equal access; auth is for accountability only ("who did what")
- Every record (orders, jobs, expenses, payments, customers) stores `CreatedById` / `RecordedById` referencing the logged-in user
- Default user seeded on first run: username `admin`, PIN `0000`
- `LoginWindow` shown at startup before the main UI; closing it exits the app
- App opens maximized

### 8.7 Items Management Page

Dedicated full-page view accessible from the sidebar. Manages item categories and their item names in a split-panel layout.

**Left panel — Item Categories:**
- DataGrid listing all categories with Edit and Delete buttons per row
- "Add Category" button opens a popup dialog (MaterialDesign `DialogHost`) with a Name field
- Edit button pre-fills the dialog to rename the category
- Delete with confirmation (also deletes all item names in the category)

**Right panel — Item Names (for selected category):**
- DataGrid listing item names for the selected category with Edit and Delete buttons
- "Add Item" button opens a popup dialog with a Name field (disabled until a category is selected)
- Edit button pre-fills the dialog to rename the item name
- Delete with confirmation

### 8.8 Vendors Management Page

- DataGrid with columns: Name, Phone, Notes — with Edit and Delete buttons per row
- "Add Vendor" button opens a popup dialog with Name (required), Phone, Notes fields
- Edit pre-fills the dialog; Save updates the record
- Delete with confirmation

### 8.9 Technicians Management Page

- DataGrid with columns: Name, Phone — with Edit and Delete buttons per row
- "Add Technician" button opens a popup dialog with Name (required) and Phone fields
- Edit pre-fills the dialog; Save updates the record
- Delete with confirmation

### 8.10 Expense Categories Management Page

- DataGrid with column: Name — with Edit and Delete buttons per row
- "Add Category" button opens a popup dialog with a Name field
- Edit pre-fills the dialog to rename; Delete with confirmation

### 8.11 Payment Channels Management Page

- DataGrid with column: Name — with Edit and Delete buttons per row
- "Add Channel" button opens a popup dialog with a Name field
- Edit pre-fills the dialog to rename; Delete with confirmation

### 8.12 Users Management Page

- DataGrid with columns: Username, Status (Active/Inactive) — with Reset PIN and Toggle Active buttons per row
- "Add User" button opens a popup dialog with Username and PIN fields
- "Reset PIN" button on each row opens a popup dialog with a new PIN field
- Currently logged-in user cannot be deactivated
- Delete not supported — deactivate to preserve FK integrity on old records

### 8.13 Customers Management Page

- DataGrid with columns: Name, Phone, Address, Notes — with Edit and Delete buttons per row
- "Add Customer" button opens a popup dialog; Delete is FK-safe (friendly error if the customer has orders/jobs)

---

## 9. Navigation Structure

Sidebar navigation (top to bottom):
1. Dashboard
2. Print Orders
3. Haier Jobs
4. Expenses
5. Reports
— separator —
6. Customers
7. Items
8. Vendors
9. Technicians
10. Expense Categories
11. Payment Channels
12. Users
— separator —
13. Settings
14. "Logged in as: {username}"

---

## 10. Error Prevention Strategy

| Risk | Prevention |
|------|-----------|
| Wrong item names | Item names come from a managed dropdown — no free-text |
| Wrong vendor / technician | Selected from managed lists |
| Missing required fields | Fields highlight red on save attempt; save blocked |
| Negative or zero amounts | Input fields enforce positive numbers at keystroke level |
| Wrong totals | Totals are computed — never manually entered |
| Duplicate payments | Warning if same order + same amount + same date already exists |
| Accidental deletes | Confirmation dialog required for all delete actions |
| Skipped vendor assignment | Status cannot advance to "Sent to Vendor" without a vendor record |

---

## 11. PDF Invoice Specification

Matches the existing Smart Solutions invoice template:

- **Header:** Smart Solutions logo (left), NTN number (right)
- **Subtitle:** "Supply and Provision of Printing, Stationery, Panaflex, Binding, Allied Services, and General Order Supplies"
- **Label:** INVOICE (bold, centered)
- **Meta:** Serial No. (= Order ID, auto-incremented), Date (right-aligned)
- **Bill To:** Customer name and address
- **Line Items Table:**
  - Columns: No, DESCRIPTION, QTY, RATE, TOTAL RS
  - Auto-populated from order line items
  - Description = item name + dimensions for sqft items (e.g., "Panaflex 4×6 ft") or item name + qty for per-piece items (e.g., "Business Cards ×500")
  - Transportation Charges line (shown only when TransportationCharges > 0)
- **Footer:** Subtotal, Total Paid Amount (PKR format)
- **Bottom:** Phone numbers, email, shop address

**Status:** the `.frx` template is currently a minimal stub — production letterhead styling is not done (see Section 14).

---

## 12. Reports

| Report | Description | Filters |
|--------|-------------|---------|
| Day Book | All transactions for a selected date | Date |
| Outstanding Balances | Orders and jobs with unpaid balances | Business type, date range |
| Monthly Summary | Income vs. Expenses vs. Profit, Cash/Easypaisa/Bank balances | Month, Year |
| Per-Order Profit | Customer total − Vendor cost for a single order | Order |
| Expense Breakdown | Total by category | Month, Year |

---

## 13. Design Decisions Log

| Item | Decision | Date |
|------|----------|------|
| Haier job fields | Added `ClaimReferenceNumber` (nullable string) to Haier jobs, shown only for Warranty type | 2026-06-09 |
| Dimension units | Per-line unit selector (Feet / Inches). Rate always per sqft; inches auto-converted to ft² in Total formula | 2026-06-09 |
| Rate calculation | Per-line Rate Type (PerSqft / PerPiece). PerSqft: H×W×Qty×Rate; PerPiece: Qty×Rate with Height/Width hidden | 2026-06-09 |
| Transportation charges | Optional per-order decimal field (`TransportationCharges`). Omitted from invoice when null or 0 | 2026-06-09 |
| Invoice serial number | Invoice Serial No. = Order ID (auto-increment). No separate counter | 2026-06-09 |
| User authentication | Username + PIN login, once per launch, accountability only (no roles) | 2026-06-10 |
| Auth bootstrap | Seed default admin/0000 user; owner changes PIN via Settings | 2026-06-10 |
| Audit trail columns | CreatedById/RecordedById nullable int FK on PrintOrder, HaierJob, Expense, payments, Customer | 2026-06-10 |
| Window startup | MainWindow opens with WindowState = Maximized | 2026-06-10 |
| Lookup management | Each lookup type (items, vendors, technicians, expense categories, payment channels, users) gets a dedicated full-page view with sidebar entry; Settings keeps only Business Info | 2026-06-10 |
| Add/Edit popup style | MaterialDesign `DialogHost` bound to `IsDialogOpen` on ViewModel — no code-behind for popup open/close except PasswordBox reading in Users page | 2026-06-10 |
| MSIX packaging | Single-project MSIX; sideloading with self-signed cert; `runFullTrust` for SQL Server + LocalAppData access | 2026-06-10 |
| First-run wizard | Three steps (DB connection, business info, admin PIN); runs before DI host is built; uses raw `SqlConnection` for test | 2026-06-10 |
| Settings file location | `%LOCALAPPDATA%\SmartSolutions\appsettings.json` — writable under MSIX; `FirstRunData` section removed after first-launch seed | 2026-06-10 |
| Documentation consolidation | Merged `docs/superpowers/specs/2026-06-09-smart-solutions-design.md` + `docs/PROJECT-CONTEXT.md` status into this single PRD; established manual "save progress" doc/memory update workflow (see `CLAUDE.md`) | 2026-08-11 |
| Admin PIN auto-focus | `AdminPinStepControl` gets keyboard focus via `IsVisibleChanged` (not `Loaded`, which only fires once at window startup since all wizard steps stay in the tree) when step 3 becomes visible | 2026-08-11 |
| README repositioning | Removed "vibe coding" / "built entirely with Claude Code" framing from `README.md` — reads as AI-assisted engineering (spec-driven design, architecture review, implementation, testing, deployment) rather than "the AI wrote this," for recruiter/client audiences | 2026-08-11 |
| No Claude co-author on commits | Rewrote all 22 repo commits (`git filter-branch --msg-filter`) to strip `Co-Authored-By: Claude` trailers; force-pushed by the user (Claude never force-pushes `master`). Future commits in this repo omit the trailer entirely — see `feedback-no-claude-coauthor` memory | 2026-08-11 |
| GitHub repo metadata | Set the GitHub "About" panel: description ("Production WPF desktop app for order & payment management — printing and Haier AC after-sales service, built with .NET 10, EF Core, and SQL Server") and 13 topics focused on tech stack + domain (wpf, csharp, dotnet, entity-framework-core, sql-server, mvvm, material-design, msix, desktop-application, xaml, invoicing, business-management, crud-application) — deliberately no AI-tooling topics, consistent with the README reframing | 2026-08-11 |

---

## 14. Current Implementation Status

> Kept up to date on every "save progress" pass. Last verified against code and test run: **2026-08-11** (working tree clean, all committed and pushed to GitHub; GitHub repo description + 13 topics set for discoverability).

### Fully Implemented ✓
- Setup wizard: header contrast fix (White→Black text) and admin PIN box auto-focus on step 3
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
- Vendors, Technicians, Expense Categories, Payment Channels management pages
- Users management page (add user, reset PIN, deactivate/reactivate)
- Settings page (business info: name, NTN, address, phone)
- Audit trail (`CreatedById`/`RecordedById` on all records, nullable for existing rows)

### In Progress / Uncommitted
- None — working tree is clean as of 2026-08-11.

### Known Limitations (deferred, not bugs)
- Dashboard balance cards hardcode "Cash", "Easypaisa", "Bank" channel names in `DashboardService` — renaming these channels in Settings breaks the cards
- `ReportsViewModel` queries EF directly (bypasses service layer) for some report queries
- Invoice `.frx` FastReport template is a minimal stub — needs FastReport Designer work for production letterhead

### Open Items (confirm with owner before implementing)
- Haier job fields — may need additional fields specific to Haier's warranty system
- Transportation charges — fixed fee, per-order, or calculated separately?
- Invoice serial number — auto-increment or manual entry?
- PDF invoice styling — production-quality letterhead with logo

### Verified Metrics (2026-08-11, re-verified end of session)
- 16 entities (`SmartSolutions.Data/Entities/`), 8 services (`SmartSolutions.Core/Services/`)
- 35/35 unit tests passing, build clean (0 errors; 1 pre-existing NuGet version-constraint warning, unrelated to app code)
- GitHub: https://github.com/jsoftsol/Smart-Solutions — `master` HEAD `83ef50c`, 22 commits, all authored solely by Ammad Sarfraz (no Claude co-author trailers), verified directly via GitHub API

For the running session log (what happened each session, in order), see `docs/PROJECT-CONTEXT.md`.
