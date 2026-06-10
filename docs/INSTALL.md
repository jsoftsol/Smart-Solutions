# Smart Solutions — Installation Guide

## Prerequisites (one-time per PC)

Before installing the `.msix` package, the signing certificate must be trusted on the target machine.

### Install the signing certificate

1. Copy `SmartSolutions.cer` to the target PC.
2. Open PowerShell **as Administrator** and run:

```powershell
Import-Certificate -FilePath "SmartSolutions.cer" -CertStoreLocation Cert:\LocalMachine\Root
```

Or manually:
1. Double-click `SmartSolutions.cer`
2. Click **Install Certificate**
3. Select **Local Machine** → Next
4. Select **Place all certificates in the following store** → Browse → **Trusted Root Certification Authorities**
5. Finish

## Install the app

Double-click `SmartSolutions.App_1.0.0.0_x64.msix` and follow the prompts.

## First-run setup

On first launch the Setup Wizard will appear:

1. **Database** — enter the server name (e.g. `SERVER-PC\SQLEXPRESS`), confirm the database name (`SmartSolutions`), choose authentication, and click **Test Connection**. Proceed when the connection test passes.
2. **Business Info** — enter your business name (required), NTN, address, and phone numbers. These appear on all printed invoices.
3. **Admin PIN** — set a new PIN for the `admin` account (4 digits), or click **Skip for now** to keep the default `0000` and change it later from the Users page.

## Connection string location

The connection string is stored at:

```
%LOCALAPPDATA%\SmartSolutions\appsettings.json
```

To reconfigure the database connection (e.g. after moving SQL Server to a different PC), delete this file and relaunch the app — the Setup Wizard will reappear.
