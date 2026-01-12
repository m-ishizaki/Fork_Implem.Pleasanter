# .github/upgrades/plan.md

## Executive Summary

### Scenario

- Scenario: .NET version upgrade to `net10.0` (project-wide).
- Solution: `C:\D\Dev\blog\mig\p_2_êÊ\Implem.Pleasanter.sln`
- Assessment source: `.github/upgrades/assessment.md` (analysis completed).

### High-level metrics (from assessment.md)

- Total projects: 19
- Projects requiring upgrade: 14
- Total NuGet packages: 125 (51 need upgrade)
- Key high-risk projects: `Implem.Pleasanter.NetFramework`, `Implem.Pleasanter`

### Selected Strategy

**All-At-Once Strategy** ? All projects upgraded simultaneously in a single, atomic operation.

Rationale:
- Solution size: 19 projects (within All-At-Once target range <30).
- Assessment provides per-project proposed target frameworks and package suggestions.
- Team will accept elevated risk to complete upgrade in a unified pass.

Caveat: Several `net472` (classic) and WPF projects introduce higher risk; plan includes risk mitigation and rollback guidance.

### Complexity classification

Based on assessment metrics (19 projects, 2705 issues, multiple high-risk projects) the solution is classified as **Complex** by size and risk profile:
- Reasons:
  - Project count > 15
  - Several `net472` classic projects with heavy `System.Web` and WPF usage (high-impact API incompatibilities)
  - Many package updates and security flags (user chose to defer remediation)

Despite the complexity classification, the selected strategy remains **All-At-Once** to meet the user's request. The plan includes stronger mitigations (comprehensive build & test pass, backups, explicit rollback guidance).

### Critical package update matrix (selected key updates)

| Package | Current | Target (from assessment) | Projects affected | Reason |
|---|---:|---:|---|---|
| Microsoft.Extensions.* | 1.x/2.2.0 | 10.0.1 | Multiple (`Implem.Pleasanter`, `NetCore`, `NetFramework`) | Align to `net10.0` runtime libraries |
| Newtonsoft.Json | 12.0.1 | 13.0.4 | Multiple libraries | Compatibility and bug fixes |
| System.Data.SqlClient | 4.6.0 | 4.9.0 | `Implem.SqlServer`, `Implem.Pleasanter.NetFramework` | Security fix |
| Npgsql | 4.0.7 | 10.0.1 | PostgreSql projects | Security fix |
| Prism.Wpf | 7.1.0.431 | 8.1.97 | WPF support tools | Compatibility |
| Sustainsys.Saml2 | 2.2.0 | 2.11.0 | `Implem.Pleasanter` | Security fix |

Full package matrix with exact current and suggested versions is available in `assessment.md` and should be referenced during implementation.

---

## Implementation Timeline (phases for human understanding)

### Phase 0: Preparation
- Verify toolchain and SDK availability for `net10.0` (developer action).
- Confirm branch/working copy strategy (no Git repo detected during analysis; see Source control section).

### Phase 1: Atomic Upgrade (single coordinated operation)
- Update all project files to target frameworks specified in ÅòProject-by-Project Plans.
- Update all package references (see ÅòPackage Update Reference).
- Restore dependencies and build solution.
- Fix compilation errors introduced by framework/package updates (one bounded pass).
- Verify solution builds with 0 errors.

### Phase 2: Test Validation
- Execute automated tests (unit/integration) where available.
- Address test failures discovered after atomic upgrade.

### Phase 3: Stabilization
- Address non-automated validation items (manual smoke tests) outside task automation.

---

## Detailed Execution Steps (WHAT and ORDER)

1. Preparation (local environment): validate `dotnet` SDK for `net10.0` and any `global.json` adjustments.
2. Single-atomic update (performed once, across all projects):
   - Update `TargetFramework`/`TargetFrameworks` entries in every project per ÅòProject-by-Project Plans.
   - Update package references per ÅòPackage Update Reference and assessment suggestions.
   - Restore packages and build the entire solution to reveal compilation errors.
   - Fix compilation errors (API changes, namespace adjustments, conditional MSBuild imports).
   - Rebuild and verify solution builds with zero compilation errors.
3. Run automated tests listed in ÅòTesting Strategy.
4. Final verification and prepare for deployment.

Note: This is a single bounded atomic pass; do not split per-project updates into separate tasks.

---

## Dependency Analysis

Summary (from assessment.md):
- Projects form a dependency graph where many `netstandard2.0` libraries are shared by `Implem.Pleasanter` variants and support tools.
- Leaf projects (no project dependencies): `Implem.DisplayAccessor` (leaf), `Implem.IRds`.
- Root/application projects: `Implem.Pleasanter.NetFramework`, `Implem.Pleasanter.NetCore`, `Implem.SupportTools`.

Critical path:
- Libraries and `netstandard2.0` projects must be updated in the same atomic pass because dependent applications will be upgraded simultaneously.
- WPF and `net472` projects require `-windows` TFMs (see per-project notes).

Circular dependencies: none reported beyond the graph documented in assessment.md.

---

## Migration Scope ? Projects to be upgraded simultaneously

All projects will be upgraded in one atomic operation. The following target frameworks will be set as proposed in assessment.md:

- `Implem.CodeDefiner.NetCore` Å® `net10.0`
- `Implem.CodeDefiner.NetFramework` Å® `net10.0`
- `Implem.CodeDefiner` Å® (remains `netstandard2.0` unless assessment suggests change)
- `Implem.DefinitionAccessor` Å® (netstandard2.0)
- `Implem.DisplayAccessor` Å® (netstandard2.0)
- `Implem.Factory` Å® (netstandard2.0)
- `Implem.Libraries` Å® (netstandard2.0)
- `Implem.ParameterAccessor` Å® (netstandard2.0)
- `Implem.Pleasanter.NetCore` Å® `net10.0`
- `Implem.Pleasanter.NetFramework` Å® `net10.0` (note: see WAP Å® ASP.NET Core considerations)
- `Implem.Pleasanter` Å® (netstandard2.0)
- `Implem.SupportTools.Common` Å® `net10.0`
- `Implem.SupportTools` (Launcher) Å® `net10.0-windows` (WPF/desktop)
- `Implem.SupportTools.LdapSyncTester` Å® `net10.0-windows`
- `Implem.SupportTools.MailTester` Å® `net10.0-windows`
- `Implem.SupportTools.SysLogViewer` Å® `net10.0-windows`
- `Rds/Implem.IRds` Å® (netstandard2.0)
- `Rds/Implem.PostgreSql` Å® (netstandard2.0)
- `Rds/Implem.SqlServer` Å® (netstandard2.0)

All of the above are changed in the same commit/operation.

---

## Project-by-Project Plans (concise)

Pattern applied to each project (applies to all simultaneously):
1. Update project file to new `TargetFramework` (or append new TFM for multi-targeting where assessment suggests). For classic (non-SDK) projects, convert to SDK-style only if `Project.0001` requires it ? conversion may be needed for some projects; conversion is planned as part of atomic pass for SDK-consistent builds.
2. Update/replace `PackageReference` entries per ÅòPackage Update Reference.
3. Address MSBuild imports such as `Directory.Build.props`, `Directory.Packages.props` and ensure conditional logic is compatible with new TFMs.
4. Restore packages and build to collect compiler diagnostics.
5. Fix compilation errors discovered (API changes, type replacements, namespace updates).
6. Run project-level unit tests (if applicable).
7. Mark validation complete for the project.

Per-project quick reference (current Å® target, risk):

- `Implem.CodeDefiner.NetCore.csproj` ? `netcoreapp2.2` Å® `net10.0` ? Low risk
- `Implem.CodeDefiner.NetFramework.csproj` ? `net472` Å® `net10.0` ? Low risk
- `Implem.CodeDefiner.csproj` ? `netstandard2.0` Å® remain `netstandard2.0` ? Low risk
- `Implem.DefinitionAccessor.csproj` ? `netstandard2.0` ? No change
- `Implem.DisplayAccessor.csproj` ? `netstandard2.0` ? No change
- `Implem.Factory.csproj` ? `netstandard2.0` ? No change
- `Implem.Libraries.csproj` ? `netstandard2.0` ? Low risk (some package suggestions)
- `Implem.ParameterAccessor.csproj` ? `netstandard2.0` ? No change
- `Implem.Pleasanter.NetCore.csproj` ? `netcoreapp2.2` Å® `net10.0` ? Medium risk (ASP.NET Core changes)
- `Implem.Pleasanter.NetFramework.csproj` ? `net472` Å® `net10.0` ? High risk (System.Web migration, large LOC change)
- `Implem.Pleasanter.csproj` ? `netstandard2.0` ? Medium risk (many API changes)
- `Implem.SupportTools.Common.csproj` ? `net472` Å® `net10.0` ? Low risk
- `Implem.SupportTools.csproj` (Launcher) ? `net472` Å® `net10.0-windows` ? Medium risk (WPF)
- `Implem.SupportTools.LdapSyncTester.csproj` ? `net472` Å® `net10.0-windows` ? Medium risk (LDAP packages)
- `Implem.SupportTools.MailTester.csproj` ? `net472` Å® `net10.0-windows` ? Medium risk
- `Implem.SupportTools.SysLogViewer.csproj` ? `net472` Å® `net10.0-windows` ? Medium risk
- `Rds/Implem.IRds.csproj` ? `netstandard2.0` ? No change
- `Rds/Implem.PostgreSql.csproj` ? `netstandard2.0` ? No change
- `Rds/Implem.SqlServer.csproj` ? `netstandard2.0` ? Low/Medium risk (System.Data.SqlClient items)

For each project, the atomic operation will apply the same 7-step pattern above.

---

## Package Update Reference (grouped)

### Common package updates affecting multiple projects

- `Microsoft.Extensions.*` family: 2.2.0 Å® `10.0.1` (affects `Implem.Pleasanter`, `Implem.Pleasanter.NetCore`, `Implem.Pleasanter.NetFramework`, etc.) ? reason: framework alignment
- `Newtonsoft.Json`: 12.0.1 Å® `13.0.4` (affects multiple projects) ? recommended
- `System.Data.SqlClient`: 4.6.0 Å® 4.9.0` (security) ? affects `Implem.SqlServer`, `Implem.Pleasanter.NetFramework`
- `Npgsql`: 4.0.7 Å® `10.0.1` (security) ? affects PostgreSql projects
- `Prism.Wpf`: 7.1.0.431 Å® `8.1.97` (support) ? affects WPF support tools
- `Sustainsys.Saml2`: 2.2.0 Å® `2.11.0` (security) ? affects authentication code in `Implem.Pleasanter`

### Category-specific notes
- Packages which are part of the framework in `net10.0` should be removed as `PackageReference` and rely on framework references (assessment flags these).
- Deprecated/obsolete packages: list in assessment.md (e.g., `Microsoft.IdentityModel.*`) ? plan to replace or defer if not used by critical code paths.

Full matrix with exact current and suggested versions is in `assessment.md` ? include those exact values during implementation.

---

## Breaking Changes Catalog (expected categories)

1. System.Web / ASP.NET MVC Å® ASP.NET Core patterns (routing, filters, `HttpContext.Current`, `Controller`/`ActionResult` differences).
2. WPF desktop adjustments: set `UseWindowsDesktop` and target `net10.0-windows`; adjust package references and XAML bindings if needed.
3. System.Data.SqlClient -> update APIs and possibly replace with `Microsoft.Data.SqlClient` if desired.
4. Obsolete packages removed from framework: remove redundant `PackageReference` entries.
5. Behavior changes in `System.Uri`, `HttpContent` (require test validation).

Each breaking change will be addressed during the compilation-fix pass in Phase 1.

---

## Testing Strategy

- Per-project unit tests: run after atomic upgrade. List of test projects discovered during assessment should be executed (developer to run `dotnet test` on identified test projects).
- Integration tests: run against dependencies (DB) as configured in CI or local environment.
- Post-upgrade: run full test suite across solution.

Validation checklist (per project):
- [ ] Project builds with zero errors
- [ ] No new high-severity compiler warnings introduced
- [ ] Unit tests pass (if present)
- [ ] Integration tests pass (if present)

---

## Risk Management

High-risk items
- `Implem.Pleasanter.NetFramework` ? High risk due to heavy `System.Web` usage and API incompatibilities. Mitigations:
  - Prepare a feature-flagged branch for diagnostics before switching runtime.
  - Keep original project files backed up.
  - Consider porting web functionality to ASP.NET Core project if migration complexity is large.

- `Implem.Pleasanter` (core library) ? Medium risk; many API incompatibilities. Mitigations: comprehensive compile-and-fix pass; rely on unit tests.

General mitigations
- Run the atomic upgrade in an isolated branch or copy (no Git detected; create repo/branch as first step if possible).
- Create automated build verification and test runs after the atomic update.
- Keep an explicit rollback plan: restore original project files and packages from VCS or backup.

Security vulnerabilities: user chose "defer". The plan documents those packages and leaves their remediation for a follow-up pass. It is recommended to address critical CVEs as soon as possible after framework upgrade.

---

## Source control

- Assessment found no Git repository. Recommended approach when repository exists:
  - Create a single upgrade branch such as `upgrade/net10.0` from the main line.
  - Perform the atomic upgrade as a single commit (or one commit per logical sub-step but keep logically atomic), open a single PR for review.
  - Include detailed PR checklist referencing this `plan.md` and `assessment.md`.

If no VCS is used, ensure you take a backup of the repo root before applying changes.

---

## Success Criteria

The migration is complete when:
- All projects target the proposed TFMs in this plan (`net10.0` or `net10.0-windows` where specified) and package versions from assessment are updated.
- Solution builds with zero compilation errors.
- All automated tests pass.
- No regressions in the smoke test scenarios defined by product owners.

---

## Appendix / Next steps for executor

1. Ensure machine has .NET `10.0` SDK installed and `global.json` updated if required.
2. If VCS exists: create branch `upgrade/net10.0` and ensure workspace is clean (commit/stash changes).
3. Execute the atomic upgrade per ÅòDetailed Execution Steps.
4. Run builds and tests, fix compilation issues discovered.
5. Create PR with single atomic set of changes and request reviews.

---

## References
- `assessment.md` ? full analysis and per-project detail (source for all exact version values and API counts).

*Plan generated from `assessment.md`. This document is planning-only; it does not apply changes.*

---

## Detailed Dependency Analysis

- Summary: The solution contains a mix of SDK-style `netstandard2.0` libraries and classic `net472` applications/tools. Many `netstandard2.0` libraries are shared dependencies used by both `Implem.Pleasanter` variants and support tools.

- Leaf nodes (no dependencies):
  - `Implem.DisplayAccessor` (leaf)
  - `Rds/Implem.IRds` (leaf)

- Root nodes (applications):
  - `Implem.Pleasanter.NetFramework` (WAP)
  - `Implem.Pleasanter.NetCore` (ASP.NET Core)
  - `Implem.SupportTools` (Launcher)

- Critical path projects (most dependants or high impact):
  - `Implem.Pleasanter` (core library) ? used by both web and core variants
  - `Implem.Libraries` ? shared utilities

- Circular dependencies: none found.

- Notes on multi-targeting: For projects that previously targeted `netcoreapp2.2`, consider appending `net10.0` to `TargetFrameworks` for a smoother transition if retaining older runtime compatibility is required. Otherwise replace single TFM with `net10.0`.

- MSBuild imports and common props:
  - Check for `Directory.Build.props`/`Directory.Packages.props` in repo root and subfolders; ensure their conditionals are compatible with `net10.0`.
  - Some projects may implicitly rely on older `TargetFramework` values via imported targets ? locate and update these imports in the same atomic pass.

## Migration Strategy

Selected approach: **All-At-Once Strategy** ? perform a single atomic upgrade across the entire solution.

Why All-At-Once:
- Keeps the repository in a single cohesive state after upgrade.
- Resolves dependency and package alignment in one pass, minimizing version skew.
- Preferred when assessment shows availability of target package versions and clear replacement guidance.

Execution notes for All-At-Once:
- Prepare prerequisites (SDK, backup, optional VCS branch) before making changes.
- Update all project TFMs and package references in one coordinated change set.
- Restore, build and fix compilation errors in one bounded pass (see Task Generation guidance in strategy doc).
- Run tests after atomic upgrade completes.

Parallelization: Within the atomic operation, developers can work in parallel on fixing different compilation issues, but project file and package updates must be consolidated into the single atomic change set.

Rollback considerations:
- Keep a copy of original project files and a list of original package versions.
- If using VCS: revert the upgrade branch.

---

## Project Stubs & Risk/Complexity Overview

For every project, include the following stub that the executor will fill when performing the upgrade. The plan includes a concise summary for each project above; below are stubs for record and execution consistency.

### Project Stub Template
- Project: `<project path>`
- Current TFM: `<current>`
- Target TFM: `<target>`
- SDK-style: `<true|false>`
- Risk: `<Low|Medium|High>`
- Packages to update: `<list from assessment.md>`
- Key breaking-change areas: `<high-level list>`
- Validation checklist: build, tests, no vulnerabilities introduced

Add one stub per project during execution. Use `assessment.md` as source for exact package versions and known issues.

---
