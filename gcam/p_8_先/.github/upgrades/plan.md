# .github/upgrades/plan.md

## Table of contents
- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
- [Package Update Reference](#package-update-reference)
- [Breaking Changes Catalog](#breaking-changes-catalog)
- [Testing & Validation Strategy](#testing--validation-strategy)
- [Risk Management](#risk-management)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)

---

## Executive Summary

- Scenario: .NET バージョンアップグレード (現在: `net8.0` → 目標: `net10.0`)
- Selected Strategy: **All-At-Once Strategy** — すべてのプロジェクトを同時にアップグレードする単一の原子的操作。

Rationale / 要約理由:
- プロジェクト数: 14（ソリューション規模は All-At-Once に適合）
- すべてのプロジェクトは SDK スタイルかつ現在 `net8.0` をターゲット
- 主要な依存関係は assessment にて .NET 10 対応バージョンが提示されているものが多い
- セキュリティ脆弱性（`SixLabors.ImageSharp` v3.1.7）を今回のアップグレードで同時対応する設定

現時点の既知の懸案:
- `Implem.Pleasanter` に API 互換性問題（Binary/Source/Behavioral）が集中している。特に Directory Services、System.Data.SqlClient 系の利用が多い。
- `Implem.SqlServer` はソース互換性問題が多数（報告: 232 件相当の影響）で、コード修正の必要度が高い。

---

## Migration Strategy

- Approach: **All-At-Once** (atomic upgrade)
- Scope: 全 13 対象プロジェクトを同時に `net10.0` に更新（`docker-compose.dcproj` は除外/非対象）
- Security: 指示どおり、脆弱性はアップグレード時に同時対応（ImageSharp 等）

Atomic operation (single coordinated pass):
1. Validate local environment: Ensure .NET 10 SDK available and `global.json` (if present) compatible.
2. Update `TargetFramework` for all projects to `net10.0` (or append for multi-targeted projects).
3. Update consolidated package references to the assessment's Suggested Version (security upgrades prioritized).
4. Restore packages and attempt full solution build to enumerate compilation errors.
5. Fix compilation and API incompatibilities as a single development pass.
6. Run automated tests and validate results.

Deliverable: 単一の原子的コミットでソリューションが `net10.0` をターゲットし、ビルドとテストが成功すること。

Source control: produce one comprehensive change set/patch. If Git becomes available, use branch `upgrade/net10.0` and a single PR.

---

## Detailed Dependency Analysis

Summary (from assessment.md):
- Projects: 14 (13 require framework update)
- Dependency graph shows a small depth and clear leaf nodes. Key leaf projects: `Implem.DisplayAccessor`, `Implem.Plugins`, `Implem.IRds`.
- Root application: `Implem.Pleasanter` (ASP.NET Core Razor Pages) depends on 7 internal projects and most NuGet surface.
- High-risk dependency: `Implem.SqlServer` and `Implem.Pleasanter` share heavy usage of `System.Data.SqlClient` and Directory Services APIs.

Implications for All-At-Once:
- Although the operation is atomic, understanding dependency topology is required for targeted validation and prioritizing breaking-change investigation (leaf → root for local reasoning).
- Use the dependency graph to focus code-compatibility fixes in `Implem.SqlServer` and then verify `Implem.Pleasanter` which consumes many libraries.

Migration validation order (conceptual):
- Phase A validation: library-level compile checks for leaf libraries (`Implem.DisplayAccessor`, `Implem.Plugins`, `Implem.IRds`, `Implem.Libraries`).
- Phase B validation: mid-tier libraries (`Implem.ParameterAccessor`, `Implem.DefinitionAccessor`, `Implem.Factory`, `Implem.CodeDefiner`).
- Phase C validation: application `Implem.Pleasanter` and `Implem.TestAutomation`.

Note: These phases are for validation sequencing only. Updates are applied simultaneously.

---

## Project-by-Project Plans

For each project: Current/Target, Key package updates, Expected breaking-change categories, Code-modification notes, Validation checklist.

### `Implem.DisplayAccessor\Implem.DisplayAccessor.csproj`
- Current: `net8.0` → Target: `net10.0`
- Files/LOC: 3 files, 46 LOC
- Packages: none flagged
- Expected work: minimal. Run compilation; resolve any API warnings.
- Validation: builds with 0 errors; referenced by multiple consumers.

### `Implem.Plugins\Implem.Plugins.csproj`
- Current: `net8.0` → Target: `net10.0`
- Files/LOC: 3 files, 31 LOC
- Expected work: minimal; ensure plugin activation contracts still compatible.
- Validation: build and run consumer integration tests.

### `Rds\Implem.IRds\Implem.IRds.csproj`
- Current: `net8.0` → Target: `net10.0`
- Files/LOC: 12 files, 210 LOC
- Expected work: minimal; verify public surface used by DB provider projects.
- Validation: compile and run DB provider unit checks.

### `Implem.Libraries\Implem.Libraries.csproj`
- Current: `net8.0` → Target: `net10.0`
- Files/LOC: 70 files, 6789 LOC
- Packages: `CsvHelper`, `Newtonsoft.Json` (recommend upgrade to 13.0.4), `SixLabors.ImageSharp` (security)
- Expected work: validate ImageSharp API compatibility after update; minimal code changes expected.
- Validation: build, run library unit tests (if any), ensure consumers compile.

### `Rds\Implem.MySql\Implem.MySql.csproj` / `Rds\Implem.PostgreSql\Implem.PostgreSql.csproj`
- Current: `net8.0` → Target: `net10.0`
- Expected work: low; underlying DB drivers (`MySqlConnector`, `Npgsql`) reported compatible.
- Validation: run DB provider integration tests where available.

### `Implem.Factory`, `Implem.ParameterAccessor`, `Implem.DefinitionAccessor`, `Implem.CodeDefiner`
- All: `net8.0` → `net10.0`; expected Low to Medium. `Implem.CodeDefiner` reports ~17 source-incompatible API occurrences — compile and fix once atomic update is applied.
- Validation: compile all libraries; ensure public APIs compile for dependants.

### `Rds\Implem.SqlServer\Implem.SqlServer.csproj`
- Current: `net8.0` → Target: `net10.0`
- Files/LOC: 12 files, 1381 LOC
- Assessment: 232 source-incompatible API occurrences (highest impact). Many `System.Data.SqlClient` usages reported.
- Expected work: Medium→High. Actions:
  - Review usages of `System.Data.SqlClient` types (SqlCommand, SqlParameter, SqlConnection, SqlDataAdapter).
  - Consider migrating to `Microsoft.Data.SqlClient` if required by compatibility; otherwise adjust call sites for API shape changes.
  - Rework any DataAdapter/DataSet patterns that changed.
- Validation: compile and run DB provider-specific tests; verify integration with `Implem.Factory`.

### `Implem.Pleasanter\Implem.Pleasanter.csproj` (ASP.NET Core Razor Pages)
- Current: `net8.0` → Target: `net10.0`
- Files/LOC: 5601 files, 409264 LOC (largest surface)
- Issues: Binary incompatible (1), Source incompatible (131), Behavioral (37), many NuGet updates (14 packages flagged)
- Key package updates: `SixLabors.ImageSharp` (3.1.7 → 3.1.12), `Microsoft.Extensions.Diagnostics.HealthChecks` (8.0.8 → 10.0.1), `System.Text.Json` (8.0.5 → 10.0.1), `Newtonsoft.Json` (13.0.3 → 13.0.4), `System.DirectoryServices` (8.0.0 → 10.0.1)
- Expected work:
  - Update Program/Startup bootstrap patterns if any minor framework changes introduced (Razor Pages preferred patterns apply).
  - Address Directory Services API shifts: add explicit `System.DirectoryServices*` NuGet packages and adjust code where APIs moved.
  - Fix usages of `System.Data.SqlClient` within the app that rely on changed behavior.
  - Replace deprecated NuGet packages (eg. older WebForms-era packages listed in assessment) with supported alternatives or framework refs.
- Validation:
  - Full solution build with 0 errors
  - Run critical integration tests and smoke scenarios for Razor Pages endpoints
  - Verify authentication/authorization, middleware, and configuration behaviors

### `Implem.TestAutomation`
- Current: `net8.0` → Target: `net10.0`
- Contains Selenium-based automation; update `System.Drawing.Common` and `SixLabors.ImageSharp` as flagged.
- Validation: run automation tests after upgrade (may require updated browser drivers).

---

## Package Update Reference

Priority groups:
1. Security fixes (apply during atomic upgrade):
   - `SixLabors.ImageSharp`: `3.1.7` → `3.1.12` (projects: `Implem.Libraries`, `Implem.Pleasanter`, `Implem.TestAutomation`)
2. Framework-aligned core libs:
   - `Microsoft.Extensions.Diagnostics.HealthChecks`: `8.0.8` → `10.0.1` (`Implem.Pleasanter`)
   - `System.Text.Json`: `8.0.5` → `10.0.1` (`Implem.Pleasanter`, `Implem.TestAutomation`)
   - `System.DirectoryServices`: `8.0.0` → `10.0.1` (`Implem.Pleasanter`)
3. Routine package bumps (non-breaking where possible):
   - `Newtonsoft.Json`: `13.0.3` → `13.0.4` (all projects using it)
4. Incompatible or deprecated packages (investigate replacements):
   - `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` — flagged incompatible in several projects; consider removing or updating CI tooling external to runtime.
   - `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.Razor`, `Microsoft.AspNet.WebPages`, `Microsoft.Web.Infrastructure` — framework functionality now provided by ASP.NET Core; remove where present and replace with ASP.NET Core idioms.

Include exact package updates in change set using assessment.md Suggested Version values.

---

## Breaking Changes Catalog (high-impact highlights)

1. Directory Services (LDAP/AD)
   - Many APIs moved to separate packages. Action: add `System.DirectoryServices`, `System.DirectoryServices.Protocols`, or `System.DirectoryServices.AccountManagement` as needed and adjust code paths.
2. System.Data.SqlClient → behavior and source differences
   - Consider migration to `Microsoft.Data.SqlClient` if compatibility issues persist.
   - Update call sites for `SqlParameter` constructors, `SqlCommand` parameter handling, and `SqlDataAdapter` patterns.
3. System.Text.Json behavior changes
   - Review custom serialization converters and property naming policy.
4. Binary incompatible API surface in `Implem.Pleasanter`
   - Investigate the exact Binary incompatible APIs (assessment references one high-impact binary incompatibility). Prepare specific code patches after build errors surface.
5. Deprecated packages from ASP.NET (legacy WebForms/MVC 5)
   - Remove/replace `Microsoft.AspNet.*` packages with ASP.NET Core equivalents.

---

## Testing & Validation Strategy

Automated checks (post-upgrade atomic pass):
- Commands (for executor):
  - `dotnet restore` (root)
  - `dotnet build -c Release` (solution)
  - `dotnet test` for test projects

Discovered/Primary test project(s):
- `Implem.TestAutomation` — Selenium-based automation; run after build
- If unit test projects exist, include them in the test run (none explicitly named other than TestAutomation in assessment)

Validation checklist (per-project and solution-wide):
- [ ] Solution restores and builds with 0 errors
- [ ] No new critical security vulnerabilities (verify updated package versions)
- [ ] All automated tests pass (unit/integration/automation)
- [ ] Razor Pages endpoints render basic pages (smoke)
- [ ] Database integrations ok (connection, queries)

---

## Risk Management

Top risks:
- Large API surface in `Implem.Pleasanter` — mitigations: run focused compile checks, create a compatibility catalog, and allocate time for authentication/DI/middleware regressions.
- `Implem.SqlServer` source incompatibilities — mitigations: prioritize review of DB access layer, consider driver migration, add integration tests.
- Deprecated NuGet packages — mitigations: replace with supported alternatives and remove unused references.

Contingency:
- Preserve pre-upgrade snapshot (full source backup or VCS commit). If Git is enabled later, create a rollback branch from the pre-upgrade state.
- If blocking binary incompatibilities appear, identify minimal shim or adapter to restore build while full fix is created.

---

## Complexity & Effort Assessment

Use assessment metrics; relative complexity (Low/Medium/High):
- High: `Implem.Pleasanter`, `Rds\Implem.SqlServer`
- Medium: `Implem.Libraries`, `Implem.CodeDefiner`
- Low: remaining small libraries (`Implem.DisplayAccessor`, `Implem.Plugins`, `Implem.ParameterAccessor`, `Implem.IRds`, `Rds` providers excluding SqlServer)

Notes: complexity reflects API issues and LOC impact from assessment; not a time estimate.

---

## Source Control Strategy

- Repository currently not under Git. When source control is available:
  - Create branch `upgrade/net10.0` from mainline snapshot
  - Apply atomic changes to all project files and package references in one commit
  - Open single PR with checklist: build success, tests green, security updates applied

---

## Success Criteria

- All projects target `net10.0`
- All package updates from assessment applied, security fixes included
- Solution builds with 0 errors
- All automated tests pass
- No remaining security vulnerabilities for flagged packages (verify via package audit)

---

## Next steps (Iteration plan)

1. Iteration 2.2: Expand Project-by-Project details with exact package update matrices and pinpointed API change locations (use assessment to list files where mandatory changes were detected).
2. Iteration 2.3: Build the Breaking Changes Catalog into a file-by-file short list for the top 3 impacted projects (`Implem.Pleasanter`, `Implem.SqlServer`, `Implem.CodeDefiner`).
3. Finalize plan and prepare `tasks.md` for execution stage (single atomic TASK-001 + prerequisites TASK-000 + testing TASK-002).

[End of iteration 2.1 content]
