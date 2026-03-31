---
name: db-build-scaffold
description: Build the SQL Server DACPAC and scaffold EF Core entities. Run this after finishing all SQL schema edits in Neptune.Database/.
allowed-tools: [Bash(powershell*DatabaseBuild*), Bash(powershell*Scaffold*), Bash(git diff*)]
---

Deploy database schema changes and regenerate EF Core entities to keep the C# model layer in sync with the database.

## When to Use

Run `/db-build-scaffold` after you have finished editing SQL files in `Neptune.Database/`. Batch your SQL edits first, then run this once.

## Steps

1. **Build and deploy the DACPAC** to the local SQL Server:
   ```
   cd C:/git/esassoc/neptune/Build && powershell.exe -ExecutionPolicy Bypass -File DatabaseBuild.ps1
   ```
   - Must run from the `Build/` directory so PowerShell module imports resolve correctly.
   - If the build fails, report the error and stop.

2. **Scaffold EF Core entities** from the updated database:
   ```
   cd C:/git/esassoc/neptune/Build && powershell.exe -ExecutionPolicy Bypass -File Scaffold.ps1
   ```
   - This regenerates files in `Neptune.EFModels/Entities/Generated/`.
   - If the scaffold fails, report the error and stop.

3. **Report what changed** by running:
   ```
   git diff --stat Neptune.EFModels/Entities/Generated/
   ```
   Summarize the regenerated entity files so the user knows what was updated.

## Notes

- Both scripts **must** be run from the `Build/` directory — they use relative module imports that fail from other directories.
- After this skill completes, you will typically also need to run `/codegen` if the schema changes affect API DTOs or endpoints.
- Generated files in `Neptune.EFModels/Entities/Generated/` should not be edited directly — they are overwritten by the scaffold.
