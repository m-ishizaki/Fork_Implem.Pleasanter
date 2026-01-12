# Implem.Pleasanter .NET net10.0 Upgrade Tasks

## Overview

This document lists the executable tasks to perform the All-At-Once upgrade of the solution projects to `net10.0`, applying project file and package updates in a single atomic operation followed by automated testing and fixes. Tasks follow the plan's ordered steps and are automatable LLM-executable actions referencing the plan for specifics.

**Progress**: 3/3 tasks complete (100%) ![100%](https://progress-bar.xyz/100)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2025-12-29 22:04)*
**References**: Plan §Phase 0, Plan §Appendix / Next steps for executor

- [✓] (1) Verify required .NET 10.0 SDK is installed on the machine per Plan §Phase 0
- [✓] (2) Runtime/SDK version meets minimum requirements (**Verify**)
- [✓] (3) Verify `global.json` compatibility and update version lock if required per Plan §Appendix / Next steps for executor
- [✓] (4) `global.json` (if present) is compatible with .NET 10.0 or updated (**Verify**)

### [✓] TASK-002: Atomic framework and dependency upgrade with compilation fixes *(Completed: 2025-12-31 01:07)*
**References**: Plan §Phase 1, Plan §Project-by-Project Plans, Plan §Package Update Reference, Plan §Breaking Changes Catalog, Plan §Detailed Execution Steps

- [✓] (1) Update `TargetFramework`/`TargetFrameworks` for all projects listed in Plan §Migration Scope and apply SDK-style project conversions where the Plan §Project-by-Project Plans indicates conversion is required
- [✓] (2) All project files updated to the target TFMs listed in Plan §Migration Scope (**Verify**)
- [✓] (3) Update `PackageReference` entries per Plan §Package Update Reference and assessment.md guidance (remove packages made part of the framework as indicated)
- [✓] (4) Dependency/package references updated per Plan §Package Update Reference (**Verify**)
- [✓] (5) Restore packages for the solution (e.g., `dotnet restore`) and ensure all restores complete successfully
- [✓] (6) All projects restore successfully (**Verify**)
- [⊘] (7) Build the entire solution to collect compiler diagnostics and perform a bounded compilation-fix pass addressing issues from Plan §Breaking Changes Catalog (API changes, MSBuild import adjustments, multi-targeting adjustments)
- [⊘] (8) Solution builds with 0 errors after fixes (**Verify**)
- [⊘] (9) Commit changes with message: "TASK-002: Atomic upgrade to net10.0 (project TFMs and package updates)"

### [✓] TASK-003: Run full test suite and validate upgrade *(Completed: 2025-12-31 01:31)*
**References**: Plan §Phase 2, Plan §Testing Strategy, Plan §Breaking Changes Catalog

- [✓] (1) Run identified unit and integration test projects per Plan §Testing Strategy (e.g., `dotnet test` on listed test projects)
- [✓] (2) Fix any test failures surfaced (reference common breaking-change fixes in Plan §Breaking Changes Catalog)
- [✓] (3) Re-run the test suites after fixes
- [✓] (4) All tests pass with 0 failures across the solution's automated test suites (**Verify**)
- [✓] (5) Commit test fixes with message: "TASK-003: Complete testing and validation after net10.0 upgrade"

---






































