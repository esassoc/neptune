# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Neptune is the Orange County Stormwater Tools — a full-stack stormwater management platform for tracking BMPs (Best Management Practices), trash generation/capture, hydrologic modeling, and project lifecycle management. Built by Sitka Technology Group for Orange County Public Works.

## Build & Development Commands

### .NET Backend

```bash
# Build entire solution
dotnet build Neptune.sln

# Run API server
dotnet run --project Neptune.API/Neptune.API.csproj

# Run tests (MSTest + ApprovalTests + Coverlet coverage)
dotnet test Neptune.Tests/Neptune.Tests.csproj

# Run a single test by name
dotnet test Neptune.Tests/Neptune.Tests.csproj --filter "FullyQualifiedName~SomeTestName"

# Run tests with coverage
dotnet test Neptune.Tests/Neptune.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Angular Frontend (Neptune.Web/)

```bash
cd Neptune.Web

# First-time setup (installs Node via nvm, creates SSL cert)
npm run prestart

# Dev server with SSL (https://neptune.localhost.sitkatech.com:8213)
npm start

# Build for environments
npm run build-dev
npm run build-qa
npm run build-prod

# Regenerate TypeScript API client from OpenAPI spec
npm run gen-model

# Lint and fix
npm run lint
npm run lint-fix

# Unit tests (Karma)
npm test
```

### Database & Code Generation (from Build/ directory)

```powershell
# EF Core scaffolding + POCO generation from database
.\Scaffold.ps1 -iniFile .\build.ini

# Build SQL Server DacPac
.\DatabaseBuild.ps1

# Restore database from backup
.\DatabaseRestore.ps1
```

## Architecture

### Solution Structure

| Project | Role |
|---|---|
| **Neptune.API** | REST API (JWT Bearer auth, Swagger/OpenAPI) — consumed by Angular SPA |
| **Neptune.ExternalAPI** | Public-facing external API (Scalar docs, API-key auth) — e.g. PowerBI/Web Services consumers |
| **Neptune.Web** | Angular 21 SPA — outputs to `wwwroot/`, served as static files |
| **Neptune.EFModels** | Database-first EF Core entities, generated from SQL Server schema |
| **Neptune.Models** | Hand-written DTOs (Dto, UpsertDto, SimpleDto, GridDto variants) |
| **Neptune.Common** | Shared utilities: email (SendGrid), GIS helpers, file handling, exceptions |
| **Neptune.Database** | SQL Server project (tables, views, stored procs, DacPac) |
| **Neptune.Jobs** | Hangfire background jobs (HRU refresh, trash stats, network solves) |
| **Neptune.GDALAPI** | Microservice wrapping GDAL/OGR2OGR for file format conversions |
| **Neptune.QGISAPI** | Microservice wrapping QGIS for spatial analysis |
| **Neptune.Tests** | MSTest unit tests with ApprovalTests |

### Data Flow

API controllers inherit `SitkaController<T>` which provides `DbContext` and `CallingUser` (from JWT claims). Data access uses **static repository classes** per entity (e.g., `Projects.cs`, `TreatmentBMPs.cs`) with methods like `GetByID()`, `GetByIDAsDto()`, `GetByIDWithChangeTracking()`, `ListAsDtoAsync()`, `CreateNew()`, `Update()`. Entity-to-DTO conversion uses extension methods (`entity.AsDto()`, `entity.AsGridDto()`).

### Database: Database-First

The database schema is the source of truth. EF entities are **generated** via `Build/Scaffold.ps1` which runs EF Core scaffolding + a custom `EFCorePOCOGenerator` tool. Generated files live in `Neptune.EFModels/Entities/Generated/`. Config is in `Build/build.ini` (database name, connection, exclude list, TypeScript enum output path).

### Generated API Client (Angular)

`Neptune.Web/src/app/shared/generated/` contains auto-generated TypeScript services and models from `Neptune.API/swagger.json`. **Never edit these files by hand** — run `npm run gen-model` after API changes. The generator also produces an Auth0 allowed-list for HTTP interceptor token injection.

### Authentication

- **API**: Auth0 JWT Bearer tokens. Authority: `ocstormwatertools.us.auth0.com`, Audience: `OCSTApi`. All endpoints require auth by default (use `[AllowAnonymous]` for public). `Person` records are created/updated from JWT claims on first authenticated request.
- **SPA**: Auth0 via `@auth0/auth0-angular`. Config loaded at runtime from `assets/config.json` in `app.init.ts`. Route guards in `src/app/shared/guards/`.

### Authorization

Custom `BaseAuthorizationAttribute` (implements `IAuthorizationFilter`). Derived attributes check role membership + optional entity-level checks:

| Attribute | Granted Roles |
|---|---|
| `SitkaAdminFeature` | SitkaAdmin |
| `AdminFeature` | Admin, SitkaAdmin |
| `JurisdictionEditFeature` | Admin, SitkaAdmin, JurisdictionManager, JurisdictionEditor (+ jurisdiction match) |
| `UserViewFeature` | Any authenticated user |
| `LoggedInUnclassifiedFeature` | Unassigned users |

Roles: Admin(1), SitkaAdmin(2), JurisdictionManager(3), JurisdictionEditor(4), Unassigned(5), Viewer(6), ExternalViewer(7).

### Background Jobs (Hangfire)

Configured with SQL Server storage, 1 worker, 0 auto-retries. Dashboard at `/hangfire`. Key jobs:
- **HRURefreshJob** — calls OCGIS to fetch hydrologic response unit characteristics for LoadGeneratingUnits
- **TrashGeneratingUnitRefreshJob** — refreshes trash load statistics
- **ProjectNetworkSolveJob / TotalNetworkSolveJob** — calls Nereid service for stormwater load reduction modeling
- **LandUseBlockUploadBackgroundJob** — bulk GIS file import processing
- **RegionalSubbasinRefreshJob** — updates subbasin network models

### External Service Integrations

| Service | Purpose | Config Key |
|---|---|---|
| Nereid | Stormwater load reduction modeling | `NereidUrl` |
| OCGIS | HRU characteristics lookup (land use, precip zone) | `OCGISBaseUrl` |
| GDAL API | OGR2OGR file format conversions | `GDALAPIBaseUrl` |
| QGIS API | Spatial analysis | `QGISAPIBaseUrl` |
| Azure Blob Storage | File/photo storage | `AzureBlobStorageConnectionString` |
| SendGrid | Email notifications | `SendGridApiKey` |
| Anthropic Claude | AI chat + WQMP document extraction | `AnthropicApiKey`, `ClaudeModelId` |

### Configuration & Secrets

Secrets loaded from a JSON file path specified by `SECRET_PATH` environment variable, overriding `appsettings.json`. For test config, see `Neptune.Tests/environment.json`.

## Key Domain Entities

- **Project** — stormwater improvement initiative containing TreatmentBMPs; has a multi-step submission workflow (Draft → WIP → Submitted → Approved)
- **TreatmentBMP** — individual stormwater control facility with a Delineation (drainage area polygon), CustomAttributes, and modeling results
- **TrashGeneratingUnit** — area generating trash; links a LandUseBlock (source) to a TreatmentBMP (capture measure) and OnlandVisualTrashAssessmentArea
- **LoadGeneratingUnit** — smallest hydrologic modeling element with HRUCharacteristics
- **OnlandVisualTrashAssessment** — field assessment campaign with observations and photos
- **LandUseBlock** — zoning area with land use type, trash generation rates, permit type (Phase1MS4 filtering is important)
- **Person** — user account linked to Auth0 via `GlobalID`, has Role and StormwaterJurisdiction assignments

All major entities include audit fields: `CreatePersonID`, `CreateDate`, `UpdatePersonID`, `UpdateDate`.

## Angular Conventions (Neptune.Web)

- **Node version**: 22.17.0 (see `.nvmrc`)
- Use modern `@if`/`@for` control flow — not legacy `*ngIf`/`*ngFor`
- Prefer standalone components with lazy-loading via `loadComponent`
- Use the generated API client — don't hand-roll DTOs that duplicate generated models
- Import paths: full paths from `src/app/` (e.g., `import { X } from 'src/app/services/my-service'`)
- SCSS: `@use "/src/scss/abstracts" as *;` — reuse existing variables/mixins
- Use reactive forms with the reusable form field component at `src/app/shared/components/forms/form-field/`
- RxJS: use `async` pipe in templates, prefer `switchMap` for dependent calls
- Prettier: 4-space indent, 180 printWidth, double quotes, trailing commas (es5), semicolons

## Docker & Deployment

Services are containerized with multi-stage Dockerfiles (aspnet:10.0 runtime / sdk:10.0 build). `docker-compose/docker-compose.yml` defines services; `docker-compose.override.yml` adds local dev ports:

| Service | Local Port |
|---|---|
| neptune.api | 8211 |
| neptune.gdalapi | 8231 |
| neptune.qgisapi | 8232 |
| geoserver | 8780 |

CI/CD is Azure Pipelines (`Build/azure-pipelines.yml`) deploying to Azure Kubernetes via Helm. Infrastructure managed by Terraform (`neptune.tf/`).

## Local Dev: Trusting the Corp SSL-Inspection Root

> **Status (2026-05-05):** ESA IT added `api.anthropic.com` to the Cato TLS-inspection bypass list, so the AI-extraction path no longer needs this patch. Keep the recipe documented anyway — Cato intercepts traffic by category, and a future policy change or a different intercepted endpoint (Azure Blob, SendGrid, OCGIS, Auth0, etc.) will reproduce the same `UntrustedRoot` symptom. Apply the steps below if and when that happens.

ESA corp-managed laptops route outbound HTTPS through the Cato SASE agent, which terminates TLS and re-issues certs signed by `CN=Cato Networks Root CA`. The Linux .NET base image inside Docker only trusts public CAs, so any in-container outbound HTTPS that Cato intercepts (historically `api.anthropic.com` for AI extraction) fails with `UntrustedRoot`. Symptom: 500 from the offending endpoint with a `System.Security.Authentication.AuthenticationException` in the API logs. Windows host (browser, native `dotnet run`) trusts Cato via `LocalMachine\Root` so this is invisible outside the container. **QA/prod are unaffected** — AKS pods don't route through Cato.

Fix locally (do **not** commit any of this — each dev does it on their own machine):

1. Export the Cato root from your Windows trust store to `docker-compose/corp-root.crt`:
   ```powershell
   $cert = Get-ChildItem Cert:\LocalMachine\Root | Where-Object { $_.Subject -match "Cato Networks" }
   $pem = "-----BEGIN CERTIFICATE-----`n" + [Convert]::ToBase64String($cert.RawData, 'InsertLineBreaks') + "`n-----END CERTIFICATE-----`n"
   Set-Content -Path "docker-compose\corp-root.crt" -Value $pem -Encoding ascii
   ```
2. Add to `.dockerignore` after the `**/docker-compose*` line: `!docker-compose/corp-root.crt`
3. Add to `Neptune.API/Dockerfile` in the **base stage** (right after `FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base`, before `USER app`):
   ```
   USER root
   COPY docker-compose/corp-root.crt /usr/local/share/ca-certificates/corp-root.crt
   RUN update-ca-certificates
   ```
   It must go in `base` (not `final`) because Visual Studio's Fast Mode only builds the `base` stage and bind-mounts `/src` over it — the `final` stage isn't used for debug runs.
4. Rebuild from Visual Studio (Build → Rebuild forces VS to discard the cached base image). Outbound HTTPS to Cato-intercepted endpoints now resolves cleanly inside the container.

The cert is valid until 2034 — re-export only if Cato rotates it. If `api.anthropic.com` starts working without the patch (Cato bypass added by IT), drop the local edits.
