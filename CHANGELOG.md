
# Change log

## Unreleased

* `FLOW`/`ENDFLOW` layout with `PARAGRAPH` (wrapping, splits across pages) and `SPACE`; `IMAGE` and `TABLE` inside a flow are placed relative to the cursor and page-break automatically. New `$CURSORY` system variable. `IPdfDocumentDrawer` gains `WrapText` and `MeasureImage`, and `DrawTable` now returns the `PdfRect` it occupied; `TableDefinition.BottomMargin` keeps rows off the page bottom.
* Better errors: `PdfDslDiagnostics` reports unknown instructions ("Did you mean ...?"), missing `;`, and unclosed or mismatched blocks with their line/column; the console uses it. Undefined variables and unknown functions now throw `PdfParserException` with the source position and a suggestion (was `ArgumentOutOfRangeException` / `KeyNotFoundException`).
* `MASTER`/`ENDMASTER` pages: `NEWPAGE ... Master=name;` runs the master's statements (header/footer) after `__ONNEWPAGE` on every page, including pages a `ROWTEMPLATE` break creates; `MarginTop` becomes the default `NewPageTopMargin` for a `ROWTEMPLATE` that doesn't specify its own.
* `LINETEXT`'s `Fit=shrink` (reduce font size, down to 4pt, until the text fits its rect) and `Overflow=ellipsis` (truncate the last visible line with `…`) options.
* `TextWidth(text)` / `TextHeight(text[, maxWidth])` formula functions, measuring text in points with the current font.
* Built-in formula functions (Math, String, `Format`, `Now`/`Today`, `Iif`), usable without host registration; a host `RegisterFormulaFunction` of the same name still overrides.
* `$PAGECOUNT` system variable: resolved once every page has been recorded (publish time), usable in `TITLE`/`LINETEXT`, including from `__ONNEWPAGE` footers.
* Add `PAGE` scope to `DEBUGOPTIONS` (`DEBUGOPTIONS PAGE DEBUG_RULE;`), options are reset on each new page. `GLOBAL` (default) keeps document-wide behavior.
* Restore red debug overlays lost with the TerraPDF migration (DEBUG_TEXT, DEBUG_ROWTEMPLATE, DEBUG_RULE), and implement DEBUG_RECT and DEBUG_IMAGE. DEBUG_ALL now includes the rule.
* `$PAGEINDEX` is available on the implicit first page.

## Version 2.0.1 (September 21, 2026)

* Fix missing DslLanguage dependency

## Version 2.0.0 (September 19, 2026)

* Migrated PDF generation from PdfSharpCore and its image/font/archive dependencies to TerraPDF 2.2.0.
* Added engine-independent drawing primitives and `PdfSharpDsl.Language` for the netstandard2.0 source generator.
* Added `PublishPdf(Stream)`, `PublishPdf(string)` and `PublishPdf()` to `PdfDocumentDrawer`.
* **Breaking:** `PdfSharpDslCore` now multi-targets `net8.0` and `net10.0` (was `netstandard2.0`).
* Centralized the build in a `_build/` folder: `Version.props` holds the single product version, `Common.props` the shared package metadata, licence and target-framework aliases, and `Packages.props` every NuGet version (central package management). `Directory.Build.props`, `Directory.Build.targets` and `Directory.Packages.props` at the repository root are thin stubs that import them.
* All assemblies now ship the same version. `PdfSharpDslCore.Generator` moves from `1.0.2` to `2.0.0` and `PdfSharpDslConsole` no longer carries its own `0.1.0`.
* The release workflow now derives the published package version from the GitHub release tag, and builds, tests and packs in `Release` so the tested binaries are the ones packed.
* Updated `Microsoft.Extensions.Logging.Abstractions` and `Microsoft.Extensions.Logging.Console` to 10.0.12.

## Version 1.0.6 (September 20, 2026)

* Migrated console and test projects to .NET 10 while retaining reusable projects on .NET Standard 2.0.
* Added reproducible core-only coverage enforcement at 90% line coverage.
* Added Roslyn source-generator compilation and clean NuGet consumer validation.

## Version 1.0.5 (March 3, 2024)
* Update nugets packages and upgrade to .Net 8

## Version 1.0.4 (May 19, 2023)
* Add callback onNewpage in order to draw a custom template on each page.
  - Define UDF "__ONNEWPAGE()" in .ipdf file or register it via method 'RegisterCustomUdf(..)' in your dotnet language.

---

## Version 1.0.3 (April 16, 2023)
* Add DEBUG_RULE, in order to show area rule on each page
* Add automatic breaking page if needed in ROWTEMPLATE

---

## Version 1.0.2 (March 5, 2023)
* Add DEBUGOPTIONS, in order to show area of each figure

---

## Version 1.0.1 (March 4, 2023)
* Add ROWTEMPLATE and TEXT statements

---

## Version 1.0.0 (January 8, 2023)
* fix TITLE alignment

---

## Version 0.9.9 (January 7, 2023)
* Add If and conditonnal expressions
* Breaking changes: SET FONT name size style=> SET FONT Name=name Size=size style
* refactoring for future C# source generator feature

---

## Version 0.9.8 (December 17, 2022)
* Add pen style (solid, dot, dashdot, dashdotdot)
* Add support callback function in formula (custom function)

---

## Version 0.9.7 (December 17, 2022)
* Add For statement
* Add UDF statement
* Add "Data" argument on Image that support Base64 images (like in html)
* fixes and code coverage

---

## Version 0.9.6 (December 15, 2022)
* HIGHLIGHTTEXT
* Breaking changes : due to grammar conflicts on formulas and to have a self documented language
	- see readme for language ehancements

---

## Version 0.9.5 (November 7, 2022)
* Able to use variables in LINETEXT and IMAGE

---

## Version 0.9.4 (November 6, 2022)

* Add Variables management
* Add Formula support in place of number or string see [readme](./README.md) for details

---





