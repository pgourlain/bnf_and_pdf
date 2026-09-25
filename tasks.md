# PdfSharpDsl – task backlog

Backlog of improvements to the `.ipdf` DSL. Each task is meant to be done in **its own session**, by any coding agent, without the conversation that produced it.

**How to use this file**
1. Read [Shared context](#shared-context) first. It holds the facts every task relies on.
2. Take the first task in the [status table](#status) whose status is `todo` and whose dependencies are `done`.
3. Do only that task. Follow its **Steps**, and meet every **Done when** item.
4. Set its status to `done` in the table, and add one line under [Notes from sessions](#notes-from-sessions) if you learned something the next session needs.
5. Do not commit unless the user asks.

Stop and ask the user when a step says **ASK**, or when a decision is not covered here.

---

## Shared context

### Repository layout

| Path | Role |
|---|---|
| `PdfSharpDsl.Language/Parser/PdfGrammar.cs` | **The** Irony grammar (the one compiled). |
| `PdfSharpDslCore/Parser/PdfGrammar.cs` | Stale copy, excluded from compile (`<Compile Remove>` in the csproj). Never edit it; T03 deletes it. |
| `PdfSharpDsl.Language/Parser/PdfVisitor.cs` | Base visitor: `Visit()` switches on `node.Term.Name`, calls `protected virtual Execute…()` hooks. Unknown nodes go to `CustomVisit()`. |
| `PdfSharpDsl.Language/Evaluation/Evaluator.cs` | Formula evaluator (operators, variables, custom functions). |
| `PdfSharpDslCore/Parser/PdfDrawerVisitor.cs` | Real visitor: overrides the `Execute…` hooks, evaluates formulas, drives the drawer. System variables live here (`SystemVariableGet`, `OnNewPage`, `ROWINDEX`, `LASTTEMPLATEHEIGHT`). |
| `PdfSharpDslCore/Parser/VariablesDictionary.cs` | Variable store; intercepts live system variables (`PAGEWIDTH`, `PAGEHEIGHT`). |
| `PdfSharpDslCore/Drawing/IPdfDocumentDrawer.cs` | Drawer interface (mocked in tests with Moq). |
| `PdfSharpDslCore/Drawing/PdfDocumentDrawer.cs` | TerraPDF drawer. Records per-page `Action<VectorCanvas>` commands and renders them in `CreateDocument()` at publish time. |
| `PdfSharpDslCore/Drawing/DrawingContext.cs` | Debug flags, row-template block recorder. |
| `PdfSharpDslCore.Generator/` | Roslyn source generator (C# from `.ipdf`). Its `CSharpVisitor.CustomVisit` ignores unknown nodes, so new statements do not break it. |
| `PdfSharpDslConsole/demo.ipdf` | Feature demo; one page per feature. Output: `PdfSharpDslConsole/helloworld.pdf` (tracked in git). |
| `pdfsharpdslTests/` | xUnit + Moq tests. |
| `../TerraPDF/src/TerraPDF` | Rendering library, **separate git repo**, referenced by `ProjectReference`. |

### Commands

```bash
dotnet test                                   # from repo root, whole suite
dotnet test --filter "FullyQualifiedName~X"   # one test class/method
cd PdfSharpDslConsole && dotnet run -- demo.ipdf   # regenerate helloworld.pdf
```

Baseline: all tests pass except `PdfDocumentDrawerTests.DslOrientationsProduceRotatedTextMatrices` (fixed by T01). A task must not add new failures.

### How a new statement is added (pattern)

1. **Grammar**: declare `var XSmt = new NonTerminal("XSmt");`, set `XSmt.Rule = ToInstructionTerm("X") + …;`, add it to `PdfPrimaryInstruction.Rule`. Named args use the existing helpers `Arg("Name")` / `OptArg("Name", expr)`. Keywords that are pure syntax go in `MarkPunctuation(...)`.
2. **Base visitor**: add a `case "XSmt":` in `PdfVisitor.Visit`, a private `VisitX` that extracts child nodes, and a `protected virtual void ExecuteX(TState state, …) { }` in the "to be override" region.
3. **Drawer visitor**: override `ExecuteX` in `PdfDrawerVisitor`, evaluate formulas with `EvaluateForDouble` / `EvaluateForObject`, call the drawer.
4. **Drawer**: add a method to `IPdfDocumentDrawer` + `PdfDocumentDrawer`, and a stub to `pdfsharpdslTests/Parser/TextDocumentDrawer.cs` (implements the interface).
5. **Drawing inside `PdfDocumentDrawer`**: follow the `InternalDrawRect` shape. `AddCommand(canvas => …)` records the drawing, then `_drawingCtx.PushInstruction(offset => InternalX(… y + offset …), rect)` makes it replayable inside ROWTEMPLATE blocks. Apply `ScaleX/ScaleY/ScalePen/ScaleFont` (VIEWSIZE) and `DrawingHelper.CoordRectToPage` (negative coordinates) like the other primitives. Add a debug overlay if relevant (`DebugRect(rect)` when `_drawingCtx.DebugRect`).

### How a formula function is added

`PdfVisitor.RegisterFormulaFunction(name, Func<object[], object>)` stores host functions (names are upper-cased). Built-ins (T04) are registered the same way in the `PdfDrawerVisitor` constructor, so a host can still override them.

### Tests

- Parse + visit with a mocked drawer: see `pdfsharpdslTests/VisitorTests.cs` (`ParseText(...)`, `new Mock<IPdfDocumentDrawer>()`, `drawer.Verify(...)`).
- Check real PDF output: see `pdfsharpdslTests/PdfDocumentDrawerTests.cs`, which uses `ReadContent(pdf)` / `ReadStreams(pdf)` to inflate content streams and regex-match PDF operators (e.g. `1.0000 0.0000 0.0000 RG` = red stroke, `x y w h re` = rect with PDF bottom-up Y).

### Every task also updates

- `README.md`: syntax section for the feature.
- `CHANGELOG.md`: a line under `## Unreleased`.
- `PdfSharpDslConsole/demo.ipdf`: show the feature (new page or existing page), then regenerate the demo and check it runs without error.

### TerraPDF rule

Some tasks need a capability TerraPDF may not have. Check `../TerraPDF/src/TerraPDF/Core/VectorCanvas.cs` and `Core/PathDescriptor.cs` first. If it is missing: **ASK** the user before editing the TerraPDF repo. It is another repository, and it may need its own release.

---

## Status

| ID | Task | Size | Depends | Status |
|---|---|---|---|---|
| T01 | Fix rotated text test | S | – | done |
| T02 | Dash styles on ellipse, pie, polygon | S | – (TerraPDF) | todo |
| T03 | Remove stale grammar, fix demo page comments | S | – | todo |
| T04 | Built-in formula functions | M | – | done |
| T05 | `$PAGECOUNT` system variable | M | – | done |
| T06 | Text measuring functions | S | T04 | done |
| T07 | Named styles (`STYLE` / `USE`) | S | – | todo |
| T08 | Control flow: `ELSE IF`, `WHILE`, `STEP` | M | – | todo |
| T09 | `DEBUG_GRID` debug option | S | – | todo |
| T10 | Rounded rectangles | S | – (TerraPDF?) | todo |
| T11 | Text fitting (`Fit=shrink`, `Overflow=ellipsis`) | M | T06 | done |
| T12 | Inline text styles (markup) | L | – | todo |
| T13 | Gradient brushes | M | – (TerraPDF?) | todo |
| T14 | `INCLUDE` other `.ipdf` files | M | – | todo |
| T15 | Lists and `FOREACH` | L | – | todo |
| T16 | Host data records (`$orders[i].amount`) | L | T15 | todo |
| T17 | UDF return values | M | – | todo |
| T18 | Links and bookmarks | M | – (TerraPDF?) | todo |
| T19 | QR codes and barcodes | M | – | todo |
| T20 | One-line charts (`CHART`) | L | – | todo |
| T21 | Better parse errors | M | – | todo |
| T22 | Master pages | L | T05 | done |
| T23 | Flow layout (`FLOW`) | XL | T06, T11 | todo |
| T24 | VS Code extension | XL | – | todo |

Sizes: S < 2h, M half day, L a day, XL several sessions. The order is the recommended execution order.

---

## Tasks

### T01 – Fix rotated text test

**Goal**: `PdfDocumentDrawerTests.DslOrientationsProduceRotatedTextMatrices` passes.

**Steps**
1. Run it alone and print the actual content stream: find which `Tm` matrix TerraPDF writes now for `Orientation=vertical` and `Orientation=30`.
2. Decide which side is wrong. Check `DrawLineText` in `PdfDocumentDrawer.cs` (angle mapping: vertical = 90) and `VectorCanvas.Text(..., angle)` in TerraPDF (clockwise, around the baseline point).
   - If the rendering is visually right and only the number format changed, update the regex.
   - If the rotation direction or origin is wrong, fix the drawer.
3. Check page 5 ("Text layout, orientation and fonts") of the regenerated demo.

**Done when**: full suite green; the fix and its reason are written in the Notes section.

---

### T02 – Dash styles on ellipse, pie, polygon

**Goal**: `SET PEN red 1 dash;` affects `ELLIPSE`, `PIE` and `POLYGON`, like it does `LINE` and `RECT`.

**Context**: `PdfDocumentDrawer.GetDashPattern(pen)` already builds the pattern. TerraPDF `VectorCanvas.StrokeEllipse`, `StrokePie`, `DrawPie` and `PathDescriptor.Stroke` take no dash parameter; `Line` and `StrokeRect` do (`PrepareDashPattern`, and `BeginDashScope` in `Drawing/PdfPage.cs`).

**Steps**
1. **ASK** the user to approve the TerraPDF change.
2. TerraPDF: add `double[]? dashPattern = null, double dashPhase = 0` to `StrokeEllipse`, `StrokePie`, `DrawPie`, and a `PathDescriptor.Dash(double[] pattern, double phase = 0)`. Carry the pattern into the draw commands and wrap the PDF ops in `BeginDashScope/EndDashScope`, like `AddStrokedRect`. Add TerraPDF tests.
3. PdfSharpDsl: pass `GetDashPattern(pen)` in `InternalDrawEllipse`, `InternalDrawPie`, `InternalDrawPolygon`.
4. Test in `PdfDocumentDrawerTests`: a dashed ellipse emits a `[...] 0 d` dash operator.

**Done when**: test green; demo page 2 ("Drawing primitives and styles") shows a dashed ellipse.

---

### T03 – Remove stale grammar, fix demo page comments

**Steps**
1. Delete `PdfSharpDslCore/Parser/PdfGrammar.cs`, and its `<Compile Remove>` line in `PdfSharpDslCore/PdfSharpDslCore.csproj`. Check the same for `PdfSharpDslCore/Extensions/ParseTreeNodeExtensions.cs`, which is also excluded; delete it only if nothing includes it.
2. In `demo.ipdf`, the `# Page N:` section comments are wrong (pages were added). Regenerate the demo, count pages (every `NEWPAGE`, plus ROWTEMPLATE page breaks), and renumber the comments. Do the same with any hard-coded page number in the text.

**Done when**: solution builds, tests green, comments match the PDF footer numbers.

---

### T04 – Built-in formula functions

**Goal**: common functions work without host registration.

**Functions**

| Group | Functions |
|---|---|
| Math | `Min(a,b,…)`, `Max(…)`, `Sum(…)`, `Abs(x)`, `Round(x[,digits])`, `Floor(x)`, `Ceil(x)`, `Sqrt(x)`, `Pow(x,y)` |
| String | `Upper(s)`, `Lower(s)`, `Len(s)`, `Substr(s,start[,len])`, `Replace(s,a,b)`, `Trim(s)` |
| Format | `Format(value, fmt)`: .NET format string, invariant culture (`Format(1284.5,"N2")` → `1,284.50`, `Format(Now(),"yyyy-MM-dd")`) |
| Date | `Now()`, `Today()` (return `DateTime`; `Format` handles them; `+` with a string concatenates `ToString("yyyy-MM-dd")`) |
| Logic | `Iif(cond, a, b)` |

**Steps**
1. Create `PdfSharpDslCore/Parser/BuiltInFunctions.cs` with a static method `Register(PdfVisitor<T> visitor)` or a dictionary, and call it in the `PdfDrawerVisitor` constructor **before** any host registration, so hosts can override.
2. Arguments arrive as `object[]` (double or string). Convert with `Convert.ToDouble(x, CultureInfo.InvariantCulture)`, and throw `PdfParserException` with the function name on bad arity.
3. Check the `Evaluator` can return non-double/non-string values (DateTime). If not, keep dates internal to `Format` (e.g. `Format(Now(), "…")` only) and document it.
4. The tests currently register their own `RANDOM` / `Sum` (`VisitorTests.TestFormulaEvaluator`); they must keep passing (host override wins).
5. One `[Theory]` per group in a new `pdfsharpdslTests/BuiltInFunctionsTests.cs`.
6. README: a "Built-in functions" table under "Formula features".

**Done when**: each function covered by a test; demo has a "Built-in functions" page.

---

### T05 – `$PAGECOUNT` system variable

**Goal**: `TITLE Margin=-18 Text=("page "+$PAGEINDEX+" / "+$PAGECOUNT);` prints the final page count on every page, including from `__ONNEWPAGE`.

**Design (use this, unless it turns out impossible)**: the total count is unknown while visiting, but texts are only rendered at publish time (`PdfDocumentDrawer.CreateDocument`).
1. `SystemVariableGet("PAGECOUNT")` returns a sentinel string, e.g. `"\u0001PAGECOUNT\u0001"`. Also add `PAGECOUNT` to the intercepted keys in `VariablesDictionary.TryGetValue`.
2. In `PdfDocumentDrawer.InternalDrawText`, measure with the sentinel replaced by `"999"` (a stable width guess). Inside the recorded `AddCommand` closure, replace the sentinel with `_pages.Count` at render time.
3. Arithmetic on `$PAGECOUNT` (e.g. `$PAGECOUNT-1`) cannot work with a sentinel. Document it, and make the evaluator raise a clear error if a sentinel ends up in a numeric operation.

**Tests**: 3-page document with a footer using `$PAGECOUNT`; every page stream contains `/ 3`.

**Done when**: test green; the demo footer in `__ONNEWPAGE` shows `page X / Y`; the system variables page and README list `$PAGECOUNT`.

---

### T06 – Text measuring functions

**Goal**: `TextWidth(text)` and `TextHeight(text[, maxWidth])` return the size in points of the text, using the **current font**. Use cases: centering, sizing a box to its text.

**Steps**
1. These need the drawer. Add `PdfSize MeasureText(string text, double? maxWidth)` to `IPdfDocumentDrawer`, implemented in `PdfDocumentDrawer` with the existing private `MeasureText` / `WrapText` (line height = `font.Size * 1.2`) and `ScaleFont`.
2. Register the two functions in `PdfDrawerVisitor.Draw` (they need the `state`), as closures over the drawer.
3. Tests: `TextWidth("abc")` with a font set equals `VectorCanvas.MeasureTextWidth`; `TextHeight` of a long text with `maxWidth` gives more than one line.

**Done when**: demo shows a box sized with `TextWidth`.

---

### T07 – Named styles (`STYLE` / `USE`)

**Goal**
```
STYLE h1
    SET FONT Name="Arial" Size=20 bold;
    SET BRUSH darkblue;
ENDSTYLE
USE h1;
```

**Steps**
1. First check whether a UDF already does this: pen, brush and font set in a UDF stay set after the call (only variables are scoped). If yes, implement `STYLE` as sugar: grammar like `UdfSmt` without arguments, body restricted to `SET` statements (Pen, Brush, HBrush, Font; not Var). `USE name;` replays the body.
2. Store styles like `UserDefinedFunctions` (hoisted in `Draw`, so use-before-define works). Unknown style → `PdfParserException("Unknown style 'x'")`.
3. The source generator ignores unknown nodes; that is acceptable.

**Done when**: parser + visitor tests; demo "Text layout" page uses styles.

---

### T08 – Control flow: `ELSE IF`, `WHILE`, `STEP`

**Goal**
```
IF $X > 10 THEN … ELSE IF $X > 5 THEN … ELSE … ENDIF
WHILE $Y < $PAGEHEIGHT-50 DO … ENDWHILE
FOR I=10 TO 0 STEP -2 DO … ENDFOR
```

**Steps**
1. `ELSE IF`: extend `Else_clause_opt` in the grammar with a nested-if alternative, or add an `ElseIfList`. Watch for Irony shift/reduce conflicts (`PreferShiftHere()` is already used there). The grammar must build without new conflicts (the Irony grammar explorer or the parser's `Language.Errors` reports them).
2. `WHILE`: new statement, body `EmbbededSmtList`, and a hard iteration cap (e.g. 10 000) that throws `PdfParserException` to avoid infinite loops.
3. `STEP`: optional `STEP formula` in `ForSmt`. Default 1; a negative step counts down; step 0 → error. Keep the current behavior: inclusive bounds.
4. Tests for each, including the loop cap.

---

### T09 – `DEBUG_GRID` debug option

**Goal**: `DEBUGOPTIONS PAGE DEBUG_GRID;` draws a light grid every 50pt with coordinates, to help place elements.

**Steps**
1. Add `DebugGrid = 32` to `PdfSharpDsl.Language/Drawing/DebugOptions.cs`, map `"DEBUG_GRID"` in `PdfDrawerVisitor.MapToDebugOption`, and add `DebugGrid` to `DrawingContext` (include `DebugAll`).
2. In `PdfDocumentDrawer`, draw it where `DrawDebugRule` is called (new page, implicit first page, `PageDebugOptions` setter). Use `AddCommand` directly with a thin light-red pen (e.g. `#FFCCCC`, 0.3) and `DebugText` labels.
3. Test in `DebugOptionsAreDrawnInRed`-style: grid color present on the page.

---

### T10 – Rounded rectangles

**Goal**: `RECT x,y,w,h Radius=6;` and `FILLRECT … Radius=6;`

**Steps**
1. Check TerraPDF for a rounded rect or path arcs (`PathDescriptor.Arc`, `CurveTo` exist). If there's no direct API, build the path in PdfSharpDsl with `MoveTo/LineTo/CurveTo` (4 Bézier quarter circles, k = 0.5523). No TerraPDF change needed, but the dash pattern needs T02's `PathDescriptor.Dash`.
2. Grammar: `OptArg("Radius", FormulaExpression)` on `RectSmt` / `FillRectSmt`. Clamp the radius to `min(w,h)/2`.
3. Keep `DEBUG_RECT` and ROWTEMPLATE replay working (see the pattern, step 5).

---

### T11 – Text fitting

**Goal**: on `LINETEXT` with a rect (`x,y,w,h`): `Fit=shrink` reduces the font size until the text fits (minimum 4pt); `Overflow=ellipsis` truncates the last visible line with `…`.

**Steps**
1. Grammar: two `OptArg`s on `LineTextSmt`.
2. Drawer: extend `DrawLineText` with a fit/overflow parameter (or an options record, to avoid growing the signature again). Implement in `InternalDrawText` using `WrapText` / `MeasureText`.
3. Tests: long text with `Fit=shrink` produces a smaller `Tf` size; `Overflow=ellipsis` output contains `…` (check how TerraPDF encodes it; WinAnsi has it at 0x85).

---

### T12 – Inline text styles (markup)

**Goal**: `LINETEXT 40,100 Markup=true Text="Total: **1284 €** in [color=green]+12%[/color], *final*";`

**Rules**
- Opt-in with `Markup=true`, so existing texts with `*` are not affected.
- Supported markup: `**bold**`, `*italic*`, `[color=name|0xAARRGGBB]…[/color]`.
- Escape: `\*` and `\[`.

**Steps**
1. Parser for the markup, in a separate class (`TextMarkupParser`) producing runs `(text, bold, italic, color)`. Unit-test it alone first.
2. Rendering: one `canvas.Text` per run, advancing x by the measured run width. Wrapping splits on words across runs; this is the hard part, so do single-line first, then wrapping.
3. Alignment (`HAlign`) uses the total line width.
4. `DEBUG_TEXT` box = union of the runs.

---

### T13 – Gradient brushes

**Goal**: `SET BRUSH linear(steelblue, white, 90);` and `radial(color1, color2)`, used by `FILLRECT`, `FILLELLIPSE`, `FILLPOLYGON`, `FILLPIE`.

**Steps**
1. The grammar already has a placeholder: `BrushType.Rule = Empty /* | GradientBrush*/;`
2. Check TerraPDF for shading support (`sh` operator / shading patterns). Likely missing → **ASK** before adding it to TerraPDF. Alternative without TerraPDF: none that is clean (don't fake gradients with many thin rects).
3. `PdfBrush` needs a gradient variant. Every fill path in `PdfDocumentDrawer` must handle it.

---

### T14 – `INCLUDE` other `.ipdf` files

**Goal**: `INCLUDE "styles.ipdf";` shares UDFs, styles and colors across documents.

**Steps**
1. Resolve paths relative to the including file (the visitor has `BaseDirectory`; the console uses the working dir).
2. Easiest is a **text-level preprocessor** before parsing (replace the line with the file content, recursively), with cycle detection and a depth limit. Parse errors in the included file must report the included file name and line. If line mapping is too hard, do a parse-tree merge instead: parse the included file separately and append its root children.
3. The source generator (`PdfSharpDslCore.Generator`) reads `.ipdf` files too. Check whether it goes through the same entry point; if not, document that `INCLUDE` is not supported there.

---

### T15 – Lists and `FOREACH`

**Goal**
```
SET VAR ITEMS=("a","b","c");
FOREACH ITEM IN $ITEMS DO … ENDFOREACH
SET VAR N=Count($ITEMS);  SET VAR FIRST=$ITEMS[0];
```

**Steps**
1. Grammar: list literal `( expr, expr, … )`. Careful: it conflicts with parenthesized expressions; require at least 2 elements, or use `[ … ]` brackets instead (**recommended**, and no conflict). Add an index expression `$VAR[expr]` and a `FOREACH` statement.
2. `Evaluator` must carry `object[]` values. `Count(list)` is a built-in (T04 file).
3. `$ROWINDEX`-style loop variable scoping: follow `ForSmt`.

---

### T16 – Host data records

**Goal**: the host passes structured data; the script reads fields.
```csharp
visitor.SetData("orders", ordersList);   // IEnumerable of objects or dictionaries
```
```
FOREACH O IN $orders DO LINETEXT 40,$Y Text=$O.amount; ENDFOREACH
ROWTEMPLATE Count=Count($orders) … $orders[$ROWINDEX].customer …
```

**Steps**
1. Public API `SetData(string name, object value)` on `PdfDrawerVisitor`. It stores into `Variables` before `Draw` (note: `Draw` recreates `Variables`, so keep a separate data dictionary and copy it in).
2. Member access `.field` in the grammar and evaluator: dictionaries by key, objects by public property (reflection, case-insensitive).
3. Also allows presetting simple variables from the host (`SetData("TITLE", "Report")`).

---

### T17 – UDF return values

**Goal**: `UDF DOUBLE(X) RETURN $X*2; ENDUDF` then `SET VAR Y=DOUBLE(21);` in formulas.

**Steps**
1. `RETURN formula;` statement allowed only inside a UDF body.
2. Formula calls currently resolve only `CustomFunctions`. Add a fallback to `UserDefinedFunctions`: run the body with parameters bound (reuse `ExecuteUdfInvokeStatement` logic), and capture the `RETURN` value (an exception-free signal, e.g. a flag on the visitor that stops executing the body).
3. A UDF called as a formula may also draw; allow it and document it.

---

### T18 – Links and bookmarks

**Goal**: `LINK x,y,w,h Url="https://…";`, `LINK x,y,w,h Page=3;`, `BOOKMARK Text="Chapter 1" [Level=1];` (outline entry pointing to the current page and y).

**Steps**: check TerraPDF for link annotations and outlines (likely missing → **ASK**). PdfSharpDsl side: new statements, and link rects recorded per page in `RecordedPage`.

---

### T19 – QR codes and barcodes

**Goal**: `QRCODE x,y,size Text=$URL;`, `BARCODE x,y,w,h Type=code128 Text="ABC-123";`

**Steps**
1. **ASK** before adding a NuGet dependency. A small pure-C# QR encoder (e.g. `QRCoder`, MIT) is fine if approved; package versions go in `Directory.Packages.props`.
2. Draw modules as filled rects in a single path (not one command per module) through `AddCommand`, so vector output stays small.
3. Code128 can be implemented directly (a table plus a checksum).

---

### T20 – One-line charts (`CHART`)

**Goal**
```
CHART bar 40,100,300,200 Data=[12,30,18] Labels=["Q1","Q2","Q3"] Colors=[steelblue];
CHART pie 380,100,150,150 Data=[…];
CHART line …
```

**Steps**
1. Depends on list syntax (T15) if available. Otherwise accept `Data="12,30,18"` strings in a first version.
2. Build only from existing primitives (`FILLRECT`, `FILLPIE`, `LINE`, `LINETEXT`), inside the drawer or the visitor, so debug and ROWTEMPLATE work for free.
3. Page 3 of `demo.ipdf` builds charts by hand: add a CHART version next to it to compare.

---

### T21 – Better parse errors

**Goal**: errors that help beginners.
- `Unknown instruction 'LINETXT' at line 12, col 1. Did you mean 'LINETEXT'?` (Levenshtein distance over the keywords).
- `Variable '$TOTL' is not defined at line 30. Did you mean '$TOTAL'?`
- Missing `;`, missing `ENDFOR` / `ENDIF` / `ENDROWTEMPLATE`: name the block and the line where it was opened.

**Steps**
1. Find where parse errors are reported today (console `Program.cs`, `ParseTree.ParserMessages`), and centralize the formatting in a `PdfDslDiagnostics` helper.
2. The undefined variable error comes from the evaluator at run time; include the source location (the node has `Span.Location`).
3. Tests assert the message text.

---

### T22 – Master pages

**Goal**
```
MASTER report MarginTop=60
    TITLE Margin=20 Text="ACME report";
    TITLE Margin=-18 Text=("page "+$PAGEINDEX+" / "+$PAGECOUNT);
ENDMASTER
NEWPAGE A4 portrait Master=report;
```
Pages created by ROWTEMPLATE breaks inherit the current master.

**Steps**: masters are hoisted like UDFs. On each new page (`OnNewPage`), run the current master body after `__ONNEWPAGE`, using the special-UDF global scope (`ExecuteSpecialUdfByName`). `MarginTop` becomes the default `NewPageTopMargin` for row templates.

---

### T23 – Flow layout (`FLOW`)

**Goal**: place content without computing y.
```
FLOW Margin=40 [Top=80]
    PARAGRAPH Text="…";          # wraps to the flow width, moves cursor down
    SPACE 12;
    IMAGE … ;                    # x,y relative to cursor
    TABLE … ENDTABLE
ENDFLOW
```
- `$CURSORY` exposes the current y.
- Automatic page break when the next element does not fit, keeping the page's master (T22).

**Steps (split into sessions)**
1. Design note first (write it into this file under the task): which statements are allowed in a FLOW, how their height is measured (T06 measuring, ROWTEMPLATE block measurement already exists in `InstructionBlock`), and how page breaks interact with ROWTEMPLATE.
2. PARAGRAPH + SPACE + page break.
3. Images and tables inside the flow.

**Design note (2026-09-24, session that did T01/T04–T06/T11/T22 — implementation not started, do steps 2–3 in a later session)**

*How page breaks already work, and why FLOW should reuse it rather than invent a second mechanism.* Every draw primitive (`RECT`, `LINETEXT`, …) does two things: (a) records a replayable `Action<VectorCanvas>` into the current page via `PdfDocumentDrawer.AddCommand`, emitted lazily at `PublishPdf()`/`CreateDocument()` time (this is the layer T05's `$PAGECOUNT` and T11's shrink/ellipsis hook into); (b) calls `DrawingContext.PushInstruction(action, rect, …)`, which records an `InstructionAction` (a rect + a `double offsetY => void` callback) into whatever `InstructionBlock` is currently open (root if none). `ROWTEMPLATE` opens one via `DrawingContext.OpenBlock(name, offsetY, newPageTopMargin)` (→ `PdfDrawerVisitor.ExecuteRowTemplateStatement`, `PdfDocumentDrawer.BeginDrawRowTemplate`/`EndDrawRowTemplate`) and closes it after visiting the row body; `InstructionBlock.UpdateRect` accumulates the block's `PdfRect` (hence its height) from every instruction pushed into it, bubbling up to `Parent` too. When the block closes, `InstructionBlock.Draw` (called synchronously during the DSL visit, not deferred) decides, from the accumulated `Rect.Height` vs `drawer.PageHeight`/`PageWidth`, whether everything fits on the current page, needs one `drawer.NewPage()`, or (when `ShouldBeEntirePrinted` is false, i.e. the ROWTEMPLATE case) needs `DrawByChunck`/`DrawInstructionsByChunk` to split row-by-row across several new pages. `drawer.NewPage()` synchronously fires `_onNewPageHooks`, which is what runs `__ONNEWPAGE` (T-pre-existing) and now the active MASTER's body (T22) and resets `$PAGEINDEX` — so a page FLOW creates already gets the header/footer and master inheritance for free, with zero FLOW-specific code, *as long as FLOW's break goes through `drawer.NewPage()` the same way*.

*Consequence:* `FLOW … ENDFLOW` should open its own `InstructionBlock` via `DrawingContext.OpenBlock("flow:<n>", offsetY=Top ?? 0, newPageTopMargin=Margin ?? 0)` (mirroring `BeginDrawRowTemplate`), visit its body statements normally (each one still calls `AddCommand`+`PushInstruction` as today), then `CloseBlock()`. The **`ShouldBeEntirePrinted` flag matters**: ROWTEMPLATE uses `entirePrint=true` at the per-row block level (each row is atomic — see `OpenBlock(name, offsetY, true, newPageTopMargin)` in `PdfDocumentDrawer.BeginDrawRowTemplate`) but the *iteration* around rows behaves like chunked printing. FLOW is closer to the ROWTEMPLATE-*iteration* case: individual children (one `PARAGRAPH`, one `IMAGE`) should be atomic (`entirePrint=true`, a paragraph doesn't split mid-line onto the next page — unless later split into per-line children, out of scope for the first version) while the FLOW block itself is **not** atomic (children overflow onto new pages), which is exactly `ShouldBeEntirePrinted=false` → `DrawByChunck`. **This needs verification in code, not just reading**: `DrawByChunck`/`DrawInstructionsByChunk` currently only handles instructions and nested `IInstructionBlock`s with no notion of "the next child continues where a page's remaining space ran out" beyond what ROWTEMPLATE needs (whole rows skip to the next page; there is no case for "50pt of a paragraph fit here, the rest continues on the next page" — matches the goal's own scope note that `$CURSORY`/page-break should move a *whole* element to the next page, not split it, so this should already be sufficient without touching `DrawInstructionsByChunk`, but confirm with a two-page `FLOW` + tall `PARAGRAPH` test before trusting it).

*Statements allowed inside `FLOW … ENDFLOW`* (first version, matches the goal's example): `PARAGRAPH Text=formula;` (new statement — wraps to the flow's width via T06's `WrapText`-equivalent, advances the cursor by the measured height), `SPACE formula;` (new statement — advances the cursor by a fixed amount, draws nothing), then step 3 adds `IMAGE` (already exists, just needs `x` relative to `$CURSORY` instead of absolute) and `TABLE … ENDTABLE` (already exists — `PdfDocumentDrawer.DrawTable` already measures row/column heights before drawing, so it should slot in the same way `LINETEXT`/`RECT` do: push into the open block with its computed rect). Anything else (`RECT`, `LINE`, …) is not disallowed by necessity, just not "flow-aware" — it would need an explicit absolute `y`, defeating the point of FLOW, so the grammar could restrict `EmbbededSmtList`-equivalent inside `FlowBlock` to an allowlist, or simply document that only `PARAGRAPH`/`SPACE`/`IMAGE`/`TABLE` make sense there and let anything else behave as if it had an absolute y (no restriction needed at the grammar level — simpler, and consistent with how `ROWTEMPLATE` doesn't restrict its own body either).

*Measuring:* `PARAGRAPH`'s height comes from the exact same private `WrapText`/`MeasureText(text, font)` used by T06's `IPdfDocumentDrawer.MeasureText` and T11's shrink/ellipsis — no new measuring code needed, just call it with `w = FlowWidth` before computing the rect passed to `PushInstruction`.

*`$CURSORY`:* a new system variable read the same way T05 added `$PAGECOUNT` to `VariablesDictionary`/`PdfDrawerVisitor.SystemVariableGet`, except (unlike `$PAGECOUNT`) its value **is** known synchronously while visiting (it's just "the open flow block's accumulated `Rect.Bottom` plus its `offsetY`"), so it does not need a sentinel/publish-time substitution — it can read directly off the currently-open `InstructionBlock`, the way `$LASTTEMPLATEHEIGHT` already reads `DrawingResult` from `EndDrawRowTemplate`. Needs a way for `PdfDrawerVisitor` to ask the drawer/`DrawingContext` "what's the open block's current bottom" — `DrawingContext`/`InstructionBlock` are `internal` to `PdfSharpDslCore`, so this is an internal plumbing addition, not a public `IPdfDocumentDrawer` method (contrast with `$LASTTEMPLATEHEIGHT`, which does cross the interface via `DrawingResult`).

*Master interaction (T22):* covered for free per the "consequence" paragraph above — no FLOW-specific code needed, just confirm with a test (`FLOW` tall enough to overflow a page, on a `NEWPAGE … Master=name;` page, master's header appears on both pages).

*Suggested grammar shape* (not yet written): `FlowSmt.Rule = ToTerm("FLOW") + OptArg("Margin", FormulaExpression) + OptArg("Top", FormulaExpression) + FlowBlock;` / `FlowBlock.Rule = embbededSmtListOpt + "ENDFLOW";` (reuse the shared list, like `MasterBlock` — see T22's notes on the string-concatenation red herring before assuming a shared-list conflict; there wasn't one). `PARAGRAPH`/`SPACE` are new `PdfPrimaryInstruction` alternatives usable anywhere (not flow-exclusive), consistent with how `ROWTEMPLATE`'s children aren't restricted either.

---

### T24 – VS Code extension

**Goal**: `.ipdf` editing support: syntax highlighting, keyword and color completion, live PDF preview.

**Steps (split into sessions)**
1. TextMate grammar (`ipdf.tmLanguage.json`): keywords from `PdfGrammar.cs` (instructions, `ENDxxx`, named args), strings, `#` comments, `$variables`, color names (`PdfColors`). Put it in a new folder `vscode-ipdf/`.
2. Completion: a static list generated by a small C# tool that reflects the grammar's `KeyTerms`, so it stays in sync.
3. Preview: a command that runs `PdfSharpDslConsole` on the file and opens the PDF. Later: a language server with the real parser for diagnostics (reuse T21).

---

## Notes from sessions

<!-- One line per finding, newest last: "T01 (date): …" -->
T22 (2026-09-24): `MASTER name [MarginTop=formula] … ENDMASTER` hoisted into a new `Masters` dict on `PdfVisitor<TState>` (base class, alongside `UserDefinedFunctions`; hoisting added right next to the UDF hoisting line in `Draw()`), and `NEWPAGE … Master=name;` (grammar: `OptArg("Master", variableLiteral)` on `NewPageSmt`). Two real bugs found and fixed along the way, worth reading before touching this area again:
  1. A brand-new `PdfLine`-level statement (`MasterSmt`, sibling of `UdfSmt`) needs a `case "MasterSmt": break;` in `PdfVisitor.Visit`'s switch — forgetting it doesn't fail to parse, it fails at `CustomVisit` with `NotImplementedException` deep into `Draw()`. Easy to misdiagnose as a grammar problem because the stack trace looks unrelated.
  2. Hours were lost chasing a phantom LALR conflict (tried a dedicated non-shared `MasterSmtList`/`MasterSmtListOpt` instead of reusing `embbededSmtListOpt`, reordered rule alternatives, stripped `PreferShiftHere()`/the `OptArg`, checked `parser.Language.Errors` — stayed empty throughout, i.e. Irony never actually detected a conflict) before finding the real cause: **C# compile-time string-literal concatenation in test input**, e.g. `"...; ENDMASTER" + "NEWPAGE ...;"`, silently glues into `"...ENDMASTERNEWPAGE...`" with no space, so the lexer reads one identifier token instead of two keywords. The grammar (reusing `embbededSmtListOpt`, exactly like `UdfBlock`/`ForBlock`/etc. already do) was correct from the first attempt. Lesson: when a `Parser.Parse()` call built from `"a" + "b" + "c"` string literals fails right at the boundary between two pieces, check for a missing space before doubting the grammar.
  Design decision not fully specified by this file: an explicit `NEWPAGE` without `Master=` clears the current master (starts fresh) rather than continuing the previous page's master; only a page break a `ROWTEMPLATE` triggers (which calls `drawer.NewPage()` directly, bypassing `ExecuteNewPage`) inherits it, by construction (the field is simply left untouched). The alternative (master persists across bare `NEWPAGE` until changed) was tried first and reverted — it made the demo's later, unrelated pages keep drawing the master's header/footer, overlapping the existing `__ONNEWPAGE` footer, since there's no `Master=none` syntax to opt back out.
T01 (2026-09-24): not a code bug. TerraPDF already formats `Tm` rotation coefficients with a dedicated 6-decimal `M()` helper (position stays `F()`/2-decimal) and rotation direction/values were already correct — the test failed only because `PdfSharpDslCore`/`pdfsharpdslTests` `obj`/`bin` held stale build output (probably built once against the NuGet `TerraPDF` 2.2.0 package before `UseLocalTerraPdf` picked the sibling checkout). `rm -rf` on every project's `obj`/`bin` and a fresh `dotnet restore` fixed it; no source or test change needed. If this test (or others referencing TerraPDF) misbehaves again, clean build output before assuming a product bug.
T04 (2026-09-24): implemented per the design in this file (`PdfSharpDslCore/Parser/BuiltInFunctions.cs`, called from `PdfDrawerVisitor`'s constructor so host `RegisterFormulaFunction` calls, which happen after construction, still override). Skipped the `Evaluator` DateTime-in-`+` change per the task's own fallback: `Now()`/`Today()` are only guaranteed useful through `Format(...)`; concatenating one with `+` falls back to .NET's default, culture-dependent `DateTime.ToString()` since `BinaryEvaluation`'s string operator doesn't special-case `DateTime`. Documented in README.
T11 (2026-09-24): added `TextFitOptions` (plain class, not `record` — `PdfSharpDsl.Language` targets netstandard2.0, which has no `IsExternalInit`, so a positional `record`'s `init` accessors fail to compile there) with `ShrinkToFit`/`EllipsisOverflow` flags, threaded through `IPdfDocumentDrawer.DrawLineText` as a trailing optional param (default `null`, so no existing call site needed updating) down into `PdfDocumentDrawer.InternalDrawText`. Shrink is a linear scan from the current font size down to a 4pt floor in 0.5pt steps, re-wrapping at each size — fine for realistic font-size ranges, not binary-searched. Ellipsis reuses the sentinel-aware `measure` callback added for T05's `$PAGECOUNT`, so it composes with it for free. `PdfGrammar`'s `LineTextSmt` gained `OptArg("Fit","shrink")`/`OptArg("Overflow","ellipsis")` (presence-only flags, following the existing `Opt-*` child-index-2 convention); `PdfVisitor<TState>.ExecuteLineText` gained two `bool` params before `contentNode`. The source generator (`PdfSharpDslCore.Generator`) doesn't override `ExecuteLineText` at all — LINETEXT already wasn't source-generated before this task — so it needed no changes. Confirmed via `WinAnsiEncoding` that `…` (U+2026) already round-trips through `PdfPage.EscapeForPdfString` as the octal escape `\205` (byte 0x85); no TerraPDF change needed.
T06 (2026-09-24): added `PdfSize IPdfDocumentDrawer.MeasureText(string, double? maxWidth)`, implemented in `PdfDocumentDrawer` with the existing private `MeasureText`/`WrapText` and `ScaleFont`. Measures in physical page points (same space as `$PAGEWIDTH`/`$PAGEHEIGHT`, which are also physical, not VIEWSIZE-virtual) — if a script uses `VIEWSIZE` and needs `TextWidth` back in virtual units, it isn't handled here. `TextWidth`/`TextHeight` are registered in `PdfDrawerVisitor.Draw` (not the constructor, unlike T04's built-ins) because they close over `state`; guarded with a `CustomFunctions.ContainsKey` check so a host registration made between construction and `Draw()` still wins. `TextDocumentDrawer` (test double) throws `NotImplementedException`, consistent with its other unexercised members.
T05 (2026-09-24): implemented per the design in this file. `SystemVariableTokens.PageCountSentinel` (new file, `PdfSharpDsl.Language/Evaluation/SystemVariableTokens.cs`) is the sentinel, shared by `VariablesDictionary` (new intercepted key), `PdfDrawerVisitor.SystemVariableGet`, and `PdfDocumentDrawer`. `WrapText` now takes an optional `measure` function so wrapping/sizing can use a "999" stand-in for the sentinel without mutating the stored line text; the real page count is substituted with `_pages.Count` inside the `AddCommand` closure, i.e. only at publish time. `BinaryEvaluation` throws a clear `NotSupportedException` if `$PAGECOUNT` is used in any operator other than string `+`.
