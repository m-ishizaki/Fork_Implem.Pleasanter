# Implem .NET 10 Upgrade Tasks

## Overview

This document tracks the execution of the repository upgrade from `net8.0` to `net10.0`, updating project files and package references across all projects and validating build and tests. Work is organized into a prerequisites verification, a single consolidated code upgrade pass, test execution/fixes, and a final commit.

**Progress**: 2/4 tasks complete (50%) ![0%](https://progress-bar.xyz/50)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2025-12-29 10:10)*
**References**: Plan §Migration Strategy, Plan §Detailed Dependency Analysis

- [✓] (1) Verify .NET 10 SDK is installed and available on the execution environment per Plan §Migration Strategy
- [✓] (2) Runtime/SDK version meets minimum requirements (**Verify**)
- [✓] (3) Check `global.json` (if present) for compatibility with .NET 10 and update per Plan §Migration Strategy
- [✓] (4) `global.json` is compatible or updated (**Verify**)
- [✓] (5) Verify required toolchain components (dotnet CLI, required SDK/workload components) are installed per Plan §Migration Strategy
- [✓] (6) Toolchain components installed and usable (**Verify**)

---

### [✓] TASK-002: Atomic framework and package upgrade with compilation fixes *(Completed: 2025-12-29 11:11)*
**References**: Plan §Migration Strategy, Plan §Project-by-Project Plans, Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [✓] (1) Update `TargetFramework` in all projects listed in Plan §Project-by-Project Plans to `net10.0` (append for multi-targeted projects as specified)
- [✓] (2) All project files updated to target `net10.0` (**Verify**)
- [✓] (3) Update package references across all projects per Plan §Package Update Reference (include security updates such as `SixLabors.ImageSharp` and framework-aligned versions)
- [✓] (4) All package references updated to versions from Plan §Package Update Reference (**Verify**)
- [✓] (5) Restore dependencies (`dotnet restore`) at repository root per Plan §Testing & Validation Strategy
- [✓] (6) All dependencies restored successfully (**Verify**)
- [✓] (7) Build the full solution and fix all compilation/API incompatibility errors found per Plan §Breaking Changes Catalog (single bounded pass)
- [✓] (8) Solution builds with 0 errors (**Verify**)

---

### [▶] TASK-003: Run full test suite and validate upgrade
**References**: Plan §Testing & Validation Strategy, Plan §Project-by-Project Plans, Plan §Breaking Changes Catalog

- [▶] (1) Run tests in `Implem.TestAutomation` and any unit/integration test projects via `dotnet test` per Plan §Testing & Validation Strategy
- [ ] (2) Fix any test failures, referencing Plan §Breaking Changes Catalog for known compatibility issues
- [ ] (3) Re-run tests after fixes
- [ ] (4) All tests pass with 0 failures (**Verify**)

---

### [▶] TASK-004: Final commit
**References**: Plan §Source Control Strategy

- [▶] (1) Commit all remaining changes with message: "TASK-004: Complete upgrade to net10.0"









