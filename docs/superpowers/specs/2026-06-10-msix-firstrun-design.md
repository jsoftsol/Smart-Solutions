# Smart Solutions — MSIX Packaging & First-Run Wizard
## Design Specification
**Date:** 2026-06-10
**Version:** 1.0

---

## 1. Overview

Package the Smart Solutions WPF app as an MSIX installer for sideloading on LAN PCs. On first launch, a three-step setup wizard runs before the DI host is built — collecting the SQL Server connection string, business information, and admin PIN — so every machine is fully configured after a single guided session.

---

## 2. Goals

- Distribute the app as a signed `.msix` file installable by double-clicking
- Detect first launch automatically and guide the user through full configuration
- Store the mutable connection string in `%LOCALAPPDATA%` (not the read-only install directory)
- Seed business info and admin PIN into the database from wizard input on first run
- Leave the existing login → main window flow entirely unchanged

---

## 3. Non-Goals

- No Microsoft Store distribution
- No auto-update / AppInstaller manifest
- No per-user roles or permission changes
- No changes to the existing Settings page or Users page

---

## 4. MSIX Configuration

### 4.1 Single-Project MSIX

Add to `SmartSolutions.App.csproj`:

```xml
<WindowsPackageType>MSIX</WindowsPackageType>
```

This causes Visual Studio to generate and manage `Package.appxmanifest` as part of the project. No separate `.wapproj` is needed.

### 4.2 Package Manifest

| Property | Value |
|---|---|
| Identity Name | `SmartSolutions.App` |
| Display Name | `Smart Solutions` |
| Publisher | `CN=SmartSolutions` |
| Version | `1.0.0.0` (incremented on each release) |
| Min Windows Version | `10.0.17763.0` (Windows 10 Oct 2018 Update) |
| Capability | `runFullTrust` |

`runFullTrust` is required for:
- Unrestricted TCP access to SQL Server on the LAN
- Writing to the real `%LOCALAPPDATA%` path (not the MSIX private package folder)

### 4.3 Package Assets

Logo tiles at required MSIX sizes: 44×44, 150×150, 310×150 (PNG). Placed in `SmartSolutions.App/Assets/`. A simple branded tile using the Smart Solutions name is sufficient for v1.

### 4.4 Code Signing

A self-signed certificate (`SmartSolutions.pfx`) is generated once and checked into the repo (password-protected). The `.csproj` references it via `<PackageCertificateKeyFile>`.

**Per-machine install prerequisite:** Each PC must install the certificate to the **Trusted Root Certification Authorities** store before the `.msix` will install. One-time step per machine:

```powershell
Import-Certificate -FilePath SmartSolutions.cer -CertStoreLocation Cert:\LocalMachine\Root
```

A `INSTALL.md` in the repo root documents this step.

---

## 5. Settings File Management

### 5.1 SettingsManager

A `SettingsManager` static class in `SmartSolutions.App` owns all interaction with the persistent local config file.

**File path:** `%LOCALAPPDATA%\SmartSolutions\appsettings.json`

Because `runFullTrust` apps receive the real `Environment.SpecialFolder.LocalApplicationData` (not the MSIX package's private folder), this path is stable and user-accessible.

**Responsibilities:**
- `IsSetupRequired()` → `true` when the file does not exist
- `GetSettingsFilePath()` → returns the absolute path
- `SaveConnectionString(string connectionString)` → writes (or overwrites) the file with only `ConnectionStrings`
- `SaveWithFirstRunData(string connectionString, FirstRunData data)` → writes file including `FirstRunData` section
- `ClearFirstRunData()` → rewrites file keeping only `ConnectionStrings`
- `Load()` → deserialises and returns the current file content

### 5.2 File Structure

Full file (during first run only):

```json
{
  "ConnectionStrings": {
    "Default": "Server=SERVER\\SQLEXPRESS;Database=SmartSolutions;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "FirstRunData": {
    "BusinessName": "Smart Solutions",
    "NTN": "7569020-2",
    "Address": "Shop 5, Main Bazaar, Peshawar",
    "Phone1": "0300-0000000",
    "Phone2": "",
    "Email": "",
    "AdminPin": "1234"
  }
}
```

After seeding, `FirstRunData` is removed and only `ConnectionStrings` remains.

### 5.3 IHostBuilder Configuration Override

`ServiceConfiguration.cs` is updated to load config exclusively from the LocalAppData file:

```csharp
builder.ConfigureAppConfiguration((_, config) =>
{
    config.Sources.Clear();
    config.AddJsonFile(SettingsManager.GetSettingsFilePath(), optional: false, reloadOnChange: false);
});
```

The bundled `appsettings.json` in the install directory becomes a template/reference only — it is never read at runtime.

---

## 6. Startup Flow

`App.xaml.cs` `OnStartup` sequence:

```
1. SettingsManager.IsSetupRequired()?
   → YES: new SetupWizardWindow().ShowDialog()
          if wizard cancelled/closed → Application.Shutdown(); return
   → NO:  continue

2. Host.CreateDefaultBuilder()
       .ConfigureSmartSolutions()   ← reads from LocalAppData appsettings.json
       .Build()

3. await db.Database.MigrateAsync()

4. Seed default admin user if none exists (existing logic, unchanged)

5. FirstRunData section present in settings?
   → YES: if no BusinessInfo row exists → insert BusinessInfo (Id=1) from FirstRunData values
          if FirstRunData.AdminPin not empty:
              look up admin user → await auth.UpdatePinAsync(adminUser.Id, pin)
          SettingsManager.ClearFirstRunData()
   → NO:  continue

6. Show LoginWindow (existing flow, unchanged)

7. On login success → Show MainWindow maximized (existing flow, unchanged)
```

**Re-run safety:** If the app crashes between step 5 seed and `ClearFirstRunData`, the next launch re-enters step 5. The seed logic checks for an existing `BusinessInfo` row (Id=1) and skips insert if found. `UpdatePinAsync` is idempotent. No data is corrupted.

**Settings deleted after setup:** If a user deletes the LocalAppData file, the wizard shows again. This is correct — the connection string is gone and must be re-entered.

---

## 7. SetupWizard Window

### 7.1 Shell

`SetupWizardWindow` is a plain `Window` (no DI). It uses `MaterialDesignThemes` for styling to match the rest of the app.

**Layout (top to bottom):**
- Header bar: app logo + "Smart Solutions Setup" title
- Step indicator: three numbered circles (`1` `2` `3`), active step in primary colour, completed steps show a checkmark icon
- Content area: hosts the active step `UserControl`
- Button row: `← Back` (left) | `Next →` / `Finish` (right), `Skip for now` text button (step 3 only)

Window size: 520×480, `ResizeMode=NoResize`, `WindowStartupLocation=CenterScreen`.

### 7.2 Step 1 — Database Connection

| Control | Detail |
|---|---|
| Server\Instance | `TextBox`, placeholder `e.g. SERVER-PC\SQLEXPRESS` |
| Database Name | `TextBox`, default value `SmartSolutions` |
| Authentication | `RadioButton` group: Windows Authentication (default) / SQL Server Authentication |
| Username | `TextBox`, visible only when SQL Auth selected |
| Password | `PasswordBox`, visible only when SQL Auth selected |
| Test Connection | `Button` — opens raw `SqlConnection`, shows inline result |
| Connection result | `TextBlock` below button: green "Connected successfully ✓" or red error message |

**Connection string assembly:** Built programmatically from the individual fields — user never types a raw connection string.

**Next button:** Disabled until a successful connection test has been completed in the current session. If the user edits any field after a successful test, the result clears and Next is re-disabled.

**SQL Auth connection string format:**
```
Server={server};Database={db};User Id={user};Password={pass};TrustServerCertificate=True
```

**Windows Auth connection string format:**
```
Server={server};Database={db};Trusted_Connection=True;TrustServerCertificate=True
```

### 7.3 Step 2 — Business Information

| Field | Required | Notes |
|---|---|---|
| Business Name | Yes | Blocks Next if empty |
| NTN | No | |
| Address | No | Multi-line `TextBox` (2 rows) |
| Phone 1 | No | |
| Phone 2 | No | |
| Email | No | |

All fields are blank by default. Placeholder hint text (grey) shows example values. Next is enabled as soon as Business Name is non-empty.

### 7.4 Step 3 — Admin PIN

Introductory text: *"The default admin PIN is 0000. You can set a new PIN now, or skip and change it later from the Users page."*

| Control | Detail |
|---|---|
| New PIN | `PasswordBox`, max length 4, digits only |
| Confirm PIN | `PasswordBox`, max length 4, digits only |
| Match indicator | Inline `TextBlock`: red "PINs do not match" when mismatch |

**Finish button:** Enabled when (a) both fields are empty (skip — keep 0000) or (b) both fields match and are exactly 4 digits.

**Skip for now:** Text button that sets both fields empty and calls Finish. Equivalent to leaving both fields blank.

---

## 8. Data Models

### 8.1 FirstRunData (in-memory / JSON only)

```csharp
public class FirstRunData
{
    public string BusinessName { get; set; } = "";
    public string NTN         { get; set; } = "";
    public string Address     { get; set; } = "";
    public string Phone1      { get; set; } = "";
    public string Phone2      { get; set; } = "";
    public string Email       { get; set; } = "";
    public string AdminPin    { get; set; } = "";
}
```

Never stored in the database. Exists only in `appsettings.json` between wizard completion and first-launch seed.

### 8.2 BusinessInfo (database)

Single-row table (`Id = 1`) seeded from `FirstRunData`. Managed thereafter by the existing Settings page. Schema is the existing `BusinessInfo` entity: `Name`, `Ntn`, `Address`, `Phone1`, `Phone2`, `Email`, `Logo`.

---

## 9. IAuthService — No Changes Needed

The existing `UpdatePinAsync(int userId, string newPin)` method on `IAuthService` is sufficient for the startup seed. No new interface method is required. The seed looks up the admin user via `GetAllAsync()`, finds the one with `Username == "admin"`, and calls `UpdatePinAsync` with its `Id`.

---

## 10. Design Decisions Log

| Item | Decision | Date |
|---|---|---|
| MSIX approach | Single-project via `<WindowsPackageType>MSIX</WindowsPackageType>`, no separate `.wapproj` | 2026-06-10 |
| Distribution | Sideloading with self-signed certificate; no Store, no AppInstaller auto-update | 2026-06-10 |
| Settings location | `%LOCALAPPDATA%\SmartSolutions\appsettings.json` — real path, not MSIX private folder | 2026-06-10 |
| Wizard runs pre-host | Wizard shown before `IHost` is built; raw `SqlConnection` used for connection test | 2026-06-10 |
| First-run data transport | `FirstRunData` JSON section written by wizard, consumed by startup seed, then removed | 2026-06-10 |
| Wizard scope | Three steps: DB connection → business info → admin PIN | 2026-06-10 |
| Connection string input | Fields (server, db, auth mode, user, pass) assembled into string — no raw string input | 2026-06-10 |
| Next after connection test | Next button locked until successful test; re-locked if any field changes after passing | 2026-06-10 |
| Admin PIN skip | Leaving both PIN fields empty = skip (keeps default 0000) | 2026-06-10 |
