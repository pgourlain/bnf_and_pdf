# .NET 10 Upgrade Tasks

Coverage work blocks the framework upgrade. `PdfSharpDslCore` line coverage must be at least 90%; branch coverage is reported but does not gate progress.

**Current verified coverage (2026-09-18):** 90.76% lines (2260/2490), 83.56% branches (778/931), 83 tests passed.

## TASK-001: Coverage baseline

**Status:** Complete (2026-09-18)  
**Depends on:** None

**Objective:** Establish reproducible, core-only Cobertura reporting and record the current baseline.

**Affected files:** `.runsettings`, `scripts/coverage.ps1`, `tasks.md`

**Acceptance criteria:**

- The test suite runs through Coverlet's collector.
- The report contains only the `PdfSharpDslCore` assembly.
- The script prints line and branch totals and fails when the report is missing or contains another assembly.
- Generated reports remain under the ignored `artifacts/` directory.

**Validation:**

```powershell
powershell -NoProfile -File .\scripts\coverage.ps1
```

**Result:** 45 tests passed. Line coverage is 79.63% (1983/2490); branch coverage is 73.14% (681/931). The migration remains blocked.

**Handoff:** Begin TASK-002. Raise core line coverage above the current baseline without changing target frameworks or enforcing the final threshold yet.

## TASK-002: Evaluation coverage

**Status:** Not started  
**Depends on:** TASK-001

**Objective:** Test arithmetic, comparison, boolean, unary, null, string, and error paths; investigate the `%` parser/evaluator mismatch.

**Affected files:** `pdfsharpdslTests/FormulaTests.cs`, `PdfSharpDslCore/Evaluation/`, parser tests and parser implementation if the mismatch is confirmed

**Acceptance criteria:** Evaluation branches have focused tests, the `%` behavior is documented by a test and corrected if defective, and all existing tests pass.

**Validation:**

```powershell
dotnet test .\pdfsharpdslTests\pdfsharpdslTests.csproj --filter FullyQualifiedName~FormulaTests
powershell -NoProfile -File .\scripts\coverage.ps1
```

**Handoff:** Record the new coverage result here and identify the largest remaining uncovered core areas for TASK-003.

## TASK-003: Variables and UDFs

**Status:** Not started  
**Depends on:** TASK-002

**Objective:** Cover variable lifecycle, nested scopes, system variables, UDF dispatch, failure paths, and `__ONNEWPAGE`.

**Affected files:** `pdfsharpdslTests/`, `PdfSharpDslCore/` variable and UDF implementation

**Acceptance criteria:** Focused tests cover successful and failing variable/UDF behavior, including nested scope and new-page callbacks.

**Validation:**

```powershell
dotnet test .\pdfsharpdslTests\pdfsharpdslTests.csproj
powershell -NoProfile -File .\scripts\coverage.ps1
```

**Handoff:** Record coverage and list remaining visitor or drawing gaps for TASK-004.

## TASK-004: Visitor and drawing helpers

**Status:** Not started  
**Depends on:** TASK-003

**Objective:** Cover visitor dispatch, page defaults, debug options, tables, images, alignment, clipping, and width calculations.

**Affected files:** `pdfsharpdslTests/VisitorTests.cs`, `pdfsharpdslTests/RenderingTests.cs`, `PdfSharpDslCore/Drawing/`, visitor implementation

**Acceptance criteria:** Deterministic tests cover the listed helper paths without requiring committed generated PDFs.

**Validation:**

```powershell
dotnet test .\pdfsharpdslTests\pdfsharpdslTests.csproj
powershell -NoProfile -File .\scripts\coverage.ps1
```

**Handoff:** Record coverage and enumerate only the gaps needed to exceed 80% in TASK-005.

## TASK-005: Enforce coverage

**Status:** Not started  
**Depends on:** TASK-004

**Objective:** Cover remaining pagination and PDF drawing paths, then enforce core line coverage of at least 90%.

**Affected files:** Remaining core tests, `scripts/coverage.ps1`, CI workflow if present

**Acceptance criteria:** `PdfSharpDslCore` line coverage is at least 90%, the script fails below 90%, and branch coverage remains informational.

**Validation:**

```powershell
powershell -NoProfile -File .\scripts\coverage.ps1
```

**Handoff:** Unblock TASK-006 only after recording a passing line rate here.

## TASK-006: Generator safety

**Status:** Blocked by coverage  
**Depends on:** TASK-005

**Objective:** Remove the hard-coded Debug assembly path and add a real Roslyn generator compilation test.

**Affected files:** `PdfSharpDslCore.Generator/`, `pdfsharpdslTests/SourceGenerator/`

**Acceptance criteria:** Generator tests compile a representative input and Debug and Release builds succeed without configuration-specific paths.

**Validation:**

```powershell
dotnet test .\pdfsharpdslTests\pdfsharpdslTests.csproj --filter FullyQualifiedName~SourceGenerator
dotnet build .\bnf_and_pdf.sln --configuration Debug
dotnet build .\bnf_and_pdf.sln --configuration Release
```

**Handoff:** Document generator loading assumptions before dependency upgrades.

## TASK-007: Dependencies

**Status:** Blocked by coverage  
**Depends on:** TASK-006

**Objective:** Upgrade test, Coverlet, Roslyn, logging, image, font, and archive packages; resolve ImageSharp advisories.

**Affected files:** Project files and package lock/restore outputs where applicable

**Acceptance criteria:** Restored packages have no known ImageSharp advisories, core and generator retain `netstandard2.0`, and tests and coverage remain passing.

**Validation:**

```powershell
dotnet restore .\bnf_and_pdf.sln
dotnet list .\bnf_and_pdf.sln package --vulnerable --include-transitive
dotnet test .\pdfsharpdslTests\pdfsharpdslTests.csproj
powershell -NoProfile -File .\scripts\coverage.ps1
```

**Handoff:** Record selected package versions and any .NET 10 compatibility constraints.

## TASK-008: Framework migration

**Status:** Blocked by coverage  
**Depends on:** TASK-007

**Objective:** Pin .NET 10, move console and tests to `net10.0`, retain reusable projects on `netstandard2.0`, and update GitHub Actions.

**Affected files:** `global.json`, console/test project files, `.github/workflows/`

**Acceptance criteria:** The intended SDK is selected, console and tests target `net10.0`, core and generator target `netstandard2.0`, and workflows use `actions/setup-dotnet@v4` with `10.0.x`.

**Validation:**

```powershell
dotnet --version
dotnet build .\bnf_and_pdf.sln
dotnet test .\pdfsharpdslTests\pdfsharpdslTests.csproj
powershell -NoProfile -File .\scripts\coverage.ps1
```

**Handoff:** Record SDK and target framework versions for package validation.

## TASK-009: Package validation

**Status:** Blocked by coverage  
**Depends on:** TASK-008

**Objective:** Validate both configurations, NuGet packages, generator loading in a clean consumer, console samples, and rendering tests.

**Affected files:** Project packaging metadata, test fixtures, console samples

**Acceptance criteria:** Debug and Release builds pass, both packages can be packed, a clean `net10.0` consumer loads the generator, and samples/rendering tests pass.

**Validation:**

```powershell
dotnet build .\bnf_and_pdf.sln --configuration Debug
dotnet build .\bnf_and_pdf.sln --configuration Release
dotnet pack .\PdfSharpDslCore\PdfSharpDslCore.csproj --configuration Release
dotnet pack .\PdfSharpDslCore.Generator\PdfSharpDslCore.Generator.csproj --configuration Release
dotnet test .\pdfsharpdslTests\pdfsharpdslTests.csproj
```

**Handoff:** Record package paths and clean-consumer results for documentation.

## TASK-010: Documentation

**Status:** Blocked by coverage  
**Depends on:** TASK-009

**Objective:** Document the completed migration, framework support, and coverage workflow.

**Affected files:** `README.md`, `PdfSharpDslCore/Readme.md`, `CHANGELOG.md`, `tasks.md`

**Acceptance criteria:** The root README links this tracker, supported frameworks and coverage commands are accurate, and the changelog summarizes the migration.

**Validation:**

```powershell
powershell -NoProfile -File .\scripts\coverage.ps1
dotnet build .\bnf_and_pdf.sln --configuration Release
```

**Handoff:** Mark the roadmap complete only after all links, commands, and recorded versions are verified.