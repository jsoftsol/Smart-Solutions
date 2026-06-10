# GitHub Portfolio — README & Project Context File Design

**Date:** 2026-06-10
**Author:** Ammad Sarfraz (with Claude Code)
**Goal:** Add a README.md and PROJECT-CONTEXT.md to the public GitHub repo to showcase AI-assisted development to recruiters and Upwork clients.

---

## Context

Smart Solutions is a production WPF desktop application built for a real client in Peshawar. It was built entirely using Claude Code (AI-assisted vibe coding) by a software engineer with 20+ years of experience. The GitHub repo is intended as a portfolio piece demonstrating the ability to leverage AI to ship complex, production-quality software.

Target audiences:
- **Recruiters** — want to see architecture depth, code quality, real-world scope
- **Upwork clients** — want to see delivered features, professionalism, and a repeatable process

---

## Decisions

- Keep real client name (Smart Solutions, Peshawar, NTN: 7569020-2) — proves it's a real production system
- Include all `docs/superpowers/specs/` and `docs/superpowers/plans/` files — they show the full AI workflow: PRD → design → plan → code
- Include 3 app screenshots (the error screenshot `afterlogin.png` was removed)
- README follows Approach C: Product → Architecture → AI Process

---

## File 1: README.md

**Location:** `README.md` (outer repo root, same level as `docs/` and `SmartSolutions/` — visible on GitHub when the outer repo is pushed)

### Structure

#### Header
- Title: `Smart Solutions — Record Keeping App`
- Subtitle: Production WPF desktop app for a printing and Haier AC after-sales service business (Peshawar, Pakistan)
- Badges: `.NET 10` · `WPF` · `SQL Server` · `Material Design` · `MSIX` · `Built with Claude Code`

#### Screenshots
- 3 screenshots from `docs/screenshots/` displayed in a responsive grid
- Moved from `docs/` root to `docs/screenshots/` for cleanliness

#### What It Does
Two business domains in one app:
- **Print Orders** — customers, order lines (per-sqft or per-piece pricing), vendor assignments, status tracking, PDF invoices, partial payments
- **Haier AC Jobs** — warranty and out-of-warranty jobs, technician assignment, payment tracking
- **Supporting modules** — expenses, financial reports, full lookup data management (items, vendors, technicians, payment channels, expense categories), user accounts with audit trail
- **Deployment** — MSIX installer, multi-PC LAN deployment against shared SQL Server

#### Tech Stack
Table format:

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10, C# |
| UI | WPF + XAML |
| MVVM | CommunityToolkit.Mvvm |
| Database | SQL Server Express 2022 |
| ORM | Entity Framework Core 10 (code-first) |
| UI Theme | MaterialDesignInXamlToolkit |
| PDF/Print | FastReport Community |
| Packaging | MSIX (single-project) |
| Tests | xUnit + NSubstitute + FluentAssertions |

#### Architecture
Short description of the 3-layer structure with scale indicators:
- `SmartSolutions.Data` — 17 entities, EF Core migrations, AppDbContext
- `SmartSolutions.Core` — 8 services, 10 interfaces, all business logic
- `SmartSolutions.App` — 19 ViewModels, 17 Views, Material Design UI, FastReport templates

#### Built with AI (Claude Code)
- Explains the vibe coding workflow: requirements described in natural language → Claude generated PRD → design specs → implementation plans → code
- Links to `docs/superpowers/specs/` and `docs/superpowers/plans/`
- Positions the author: *"Built by a software engineer with 20+ years of experience, using Claude Code as a development accelerator to go from idea to production-ready app."*
- Notes: no boilerplate generated blindly — every spec was reviewed and approved before implementation

---

## File 2: docs/PROJECT-CONTEXT.md

**Location:** `docs/PROJECT-CONTEXT.md` (outer repo, alongside the submodule)
**Purpose:** (1) Public architecture reference for repo viewers; (2) Claude reads this at session start to restore context without re-exploring the codebase

### Structure

#### App Overview
One paragraph covering what the app does, who it's for, and the tech stack in one breath.

#### Architecture Snapshot
- 3-layer structure with key file counts
- 17 entities grouped by domain (Auth, Print Business, Haier AC, Shared Reference Data)
- 8 services with a one-line description each

#### Key Decisions (non-obvious, affects every session)
- No hardcoded lookups — all reference data managed via sidebar pages
- Computed totals — never stored, always derived
- Partial payment model — balance = invoiced − payments
- Auth is accountability-only — no roles/permissions, only audit stamping
- MSIX deployment — first-run wizard writes config to `%LocalAppData%`
- `IDbContextFactory` pattern — no shared singleton DbContext

#### Current Status
Checklist of implemented features, in-progress work, and pending open items (from PRD).

#### Session Log
Dated one-liners, newest first:
```
2026-06-10 — MSIX packaging, setup wizard complete; 28/28 tests passing
```

---

## Docs Folder Commits

The following currently-untracked files will be committed to make the AI workflow visible:

- `docs/superpowers/plans/2026-06-09-smart-solutions-full-app.md`
- `docs/superpowers/plans/2026-06-10-dedicated-management-pages.md`
- `docs/screenshots/` (3 renamed screenshot files)

The `docs/superpowers/specs/` files are already tracked.

---

## Out of Scope

- No GitHub Actions / CI setup in this task
- No license file (add separately if desired)
- No contribution guidelines (not an open-source project)
