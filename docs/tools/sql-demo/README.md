# Landing Page SQL Demo — Maintenance Guide

The landing page at `docs/source/index.md` features a tabbed demo showing C# LINQ code alongside the SQL linq2db generates for it. The SQL shown is based on the output of `SqlDemoGenerator.cs` and hand-formatted for the page; nothing checks it automatically, so regenerate it whenever a tab changes. This document explains how to maintain and extend it.

## Architecture

- **C# side**: Hand-written syntax-highlighted HTML using `<span>` classes (`code-keyword`, `code-type`, `code-string`, `code-number`, `code-comment`)
- **SQL side**: Output of `SqlDemoGenerator.cs` (`ToSqlQuery()`, or the executed statements for Temp Table), re-indented and wrapped in the same span classes. Each SQL pane label names its dialect.
- **Tabs**: Pure CSS — visually hidden but focusable `<input type="radio">` elements + `:checked` / `:focus-visible` sibling selectors in `main.css` (no JavaScript). The container is a `role="radiogroup"`, so keyboard users switch tabs with the arrow keys.

## How to generate SQL for a new or updated tab

`SqlDemoGenerator.cs` holds one test per tab, with the same C# as the tab. It is a reference copy: it is not compiled where it lives.

- SQLite in-memory (`CreateSQLiteConnection()`) for every tab except Merge — no server needed. Temp Table really runs `CreateTempTable` and records the executed SQL (CREATE, the bulk copy INSERT, the DROP from `Dispose`).
- Merge uses the SQL Server dialect (`CreateSqlServerConnection()`) for generation only; `ToSqlQuery` never opens the connection.

### 1. Copy the generator into the playground

From the repository root:

```bash
cp docs/tools/sql-demo/SqlDemoGenerator.cs Tests/Tests.Playground/SqlDemoGenerator.cs
```

**Never commit the copy** — `Tests/Tests.Playground` takes no new source files in a PR.

### 2. Edit the tests to add or modify queries

Keep each test's C# identical to its tab's C# pane, except Merge (stops before the executing `.Merge()`) and Temp Table (a `using` block, so the `DROP` from `Dispose` is traced).

### 3. Run the tests

```bash
dotnet test --project Tests/Tests.Playground/Tests.Playground.csproj -c Testing -f net11.0 \
  --filter "FullyQualifiedName~SqlDemoGenerator" --output detailed --test-progress
```

Each test prints `=== <tab> ===` followed by the SQL.

### 4. Copy the test file back and delete the playground copy

```bash
cp Tests/Tests.Playground/SqlDemoGenerator.cs docs/tools/sql-demo/SqlDemoGenerator.cs
rm Tests/Tests.Playground/SqlDemoGenerator.cs
```

### 5. Update the landing page

Edit `docs/source/index.md`. Each tab consists of:

```html
<!-- Tab N -->
<div class="demo-panel demo-panel-N">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# LINQ</div>
<pre class="demo-code">...syntax-highlighted C# here...</pre>
</div>
<div class="demo-pane">
<div class="demo-pane-label">Generated SQL · SQLite</div>
<pre class="demo-code demo-sql">...syntax-highlighted SQL here...</pre>
</div>
</div>
</div>
```

Only whitespace, the span wrappers and whole-line `-- <n>. <step>` comments may differ from the generator output. The one abbreviation is Temp Table's 500-row `VALUES` list.

### Adding a new tab

1. Add a new `<input type="radio">` with id `demo-tab-N`
2. Add a `<label>` in the `.demo-tabs` div
3. Add a `.demo-panel-N` div with the content
4. Add CSS selectors in `docs/source/templates/custom/public/main.css`:

```css
#demo-tab-N:checked ~ .demo-tabs label[for="demo-tab-N"],
/* ...add to the existing comma-separated list... */

#demo-tab-N:focus-visible ~ .demo-tabs label[for="demo-tab-N"],
/* ...add to the existing comma-separated list... */

#demo-tab-N:checked ~ .demo-panels .demo-panel-N,
/* ...add to the existing comma-separated list... */
```

5. Add a test for it to `SqlDemoGenerator.cs`.

## Syntax highlighting classes

| Class | Color | Used for |
|-------|-------|----------|
| `code-keyword` | Purple (#c792ea) | C# keywords, SQL keywords (SELECT, FROM, WHERE, etc.) |
| `code-type` | Blue (#82aaff) | Type names (Product, DataConnection), SQL functions (COUNT, SUM) |
| `code-string` | Green (#c3e88d) | String literals |
| `code-number` | Orange (#f78c6c) | Numeric literals |
| `code-comment` | Gray (#8b93bd) | Comments (// and --) |

## Important: blank lines in `<pre>` blocks

DocFX's Markdown processor converts blank lines inside `<pre>` into `<p>` tags. Use `&#10;` (newline entity) instead of actual blank lines to create visual spacing inside `<pre>` blocks.

## Rebuild and preview

See [`docs/README.md`](../../README.md) for building the site locally.
