# Smart Solutions — User Authentication & Startup Design
**Date:** 2026-06-10
**Version:** 1.0

---

## 1. Overview

Add simple user authentication so every record created in the app is stamped with the identity of the logged-in user. The purpose is accountability ("who did what"), not access control — all users have equal permissions.

Additionally, the app window is maximized on startup.

---

## 2. Goals

- Every transaction and entry records which user created it
- Users log in with username + PIN once per app launch
- Session lasts until the app is closed
- Users are managed via Settings (add, reset PIN, deactivate)
- App opens maximized

---

## 3. Non-Goals

- No role-based permissions
- No auto-lock after inactivity
- No audit log view in v1 (data is stored; reporting is future work)
- No password complexity requirements (PINs are 4+ digits, no policy enforced)

---

## 4. Data Model Changes

### 4.1 New Entity: `AppUser`

| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK, auto-increment |
| Username | string | Required, unique, max 50 chars |
| PinHash | string | PBKDF2-SHA256 hash with embedded salt (Rfc2898DeriveBytes) |
| IsActive | bool | Default true; inactive users cannot log in |

**Seed data:** One default user is seeded alongside existing seed data:
- Username: `admin`, PIN: `0000`, IsActive: `true`

### 4.2 `CreatedById` / `RecordedById` Added to Existing Entities

All new FK columns are `int?` (nullable) so existing rows created before this migration are not broken.

| Entity | New Column | Notes |
|--------|-----------|-------|
| PrintOrder | CreatedById | FK → AppUser |
| HaierJob | CreatedById | FK → AppUser |
| Expense | CreatedById | FK → AppUser |
| PrintOrderPayment | RecordedById | FK → AppUser |
| HaierJobPayment | RecordedById | FK → AppUser |
| Customer | CreatedById | FK → AppUser |

Navigation properties are optional (no cascade delete — deleting a user is not allowed; use deactivate instead).

---

## 5. Service Layer

### 5.1 `IAuthService` (`SmartSolutions.Core/Interfaces/`)

```
Task<AppUser?> ValidateAsync(string username, string pin)
Task<IList<AppUser>> GetAllAsync()
Task CreateAsync(string username, string pin)
Task UpdatePinAsync(int userId, string newPin)
Task SetActiveAsync(int userId, bool isActive)
```

- `ValidateAsync` returns `null` if username not found, PIN wrong, or `IsActive = false`
- All PIN operations hash via PBKDF2-SHA256, 100,000 iterations, 32-byte output, 16-byte salt — salt is prepended to the hash value and stored as a single Base64 string in `PinHash`
- `CreateAsync` throws `InvalidOperationException` if username already exists

### 5.2 `ISessionService` (`SmartSolutions.Core/Interfaces/`)

Registered as a **singleton**.

```
AppUser CurrentUser { get; }
bool IsLoggedIn { get; }
void Login(AppUser user)
```

- `CurrentUser` throws `InvalidOperationException` if accessed before `Login()` is called
- `Login()` can only be called once per process lifetime (subsequent calls throw)

ViewModels and services that create records call `_sessionService.CurrentUser.Id` to stamp `CreatedById` / `RecordedById`.

---

## 6. UI

### 6.1 `LoginWindow`

A standalone WPF `Window` (not a `UserControl` inside `MainWindow`).

- Username `TextBox` and PIN `PasswordBox`
- "Login" `Button` — disabled until both fields are non-empty
- On invalid credentials: red error label "Invalid username or PIN"
- On success: window closes, `MainWindow` opens maximized
- Closing `LoginWindow` without logging in calls `Application.Current.Shutdown()`

### 6.2 `MainWindow` — Logged-In User Display

A small label in the navigation header shows: **"Logged in as: {username}"**. This makes the active session visible at a glance.

`WindowState = Maximized` is set before `Show()` is called.

### 6.3 Settings — Users Section

New section in `SettingsView` consistent with Vendors/Technicians management:

| Action | Behaviour |
|--------|-----------|
| List users | Shows Username and Active status |
| Add user | Enter username + PIN; blocks if username already exists |
| Reset PIN | Enter new PIN for any user |
| Deactivate | Soft-delete; preserves FK references on old records |
| Reactivate | Re-enables login for a deactivated user |
| Delete | Not supported — use deactivate to preserve history |

Constraint: the currently logged-in user cannot be deactivated.

---

## 7. Startup Flow

```
App.OnStartup()
  │
  ├─ Build DI host
  ├─ Run EF migrations
  ├─ Set ShutdownMode = OnExplicitShutdown
  ├─ Show LoginWindow
  │     ├─ [Login success] → ISessionService.Login(user)
  │     │                  → LoginWindow.Close()
  │     │                  → MainWindow.WindowState = Maximized
  │     │                  → MainWindow.Show()
  │     │                  → ShutdownMode = OnMainWindowClose
  │     │
  │     └─ [Window closed without login] → Application.Current.Shutdown()
```

---

## 8. Security Notes

- PBKDF2-SHA256 with 100k iterations is appropriate for a local desktop app with a small user base
- PINs are never stored in plaintext anywhere
- No network transmission of credentials — all auth is local DB only
- This is not a high-security system; the goal is accountability, not preventing determined access

---

## 9. Changes to Existing Documents

### CLAUDE.md updates
- Remove "No user role or permission system" from Key Decisions
- Add `ISessionService` to the Services architecture section
- Update Non-Goals to clarify: no *role-based* permissions, but auth exists for audit tracking
- Add window startup behavior: `WindowState = Maximized`

### PRD updates (section 3 Non-Goals, section 4 Users, new section)
- Remove "No user role or permission system" from Non-Goals
- Add auth as a feature in section 8 (new 8.6 User Authentication)
- Update Design Decisions Log

---

## 10. Design Decisions Log

| Item | Decision | Date |
|------|----------|------|
| Auth purpose | Accountability only — no permissions, no roles | 2026-06-10 |
| Session lifetime | Once per app launch; no auto-lock | 2026-06-10 |
| Credential type | Username + 4-digit PIN | 2026-06-10 |
| PIN storage | PBKDF2-SHA256, 100k iterations, salt embedded in hash string | 2026-06-10 |
| Bootstrap | Seed default user admin/0000; owner changes PIN via Settings | 2026-06-10 |
| Existing rows | CreatedById/RecordedById are nullable; pre-auth rows remain valid | 2026-06-10 |
| User deletion | Not supported — deactivate only, preserves FK integrity | 2026-06-10 |
| Window startup | WindowState = Maximized before MainWindow.Show() | 2026-06-10 |
