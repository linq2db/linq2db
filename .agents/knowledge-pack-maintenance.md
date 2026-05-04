# LinqToDB Expert Knowledge Pack Maintenance

This file is the authoritative procedure for preparing and updating the external
linq2db Expert knowledge pack.

The knowledge pack is a packaging format for the same documentation shipped with LinqToDB.
It must not become a second source of truth.

## Task

When asked to update the linq2db Expert knowledge pack, rebuild the complete pack from current
package-local source material.

The job is mechanical conversion, not independent documentation design.

## Source Of Truth

Use only package-shipped documentation and generated package API artifacts as source material:

- `Source/LinqToDB/AGENT_GUIDE.md`
- `Source/LinqToDB/SKILL.md`
- `Source/LinqToDB/readme.md`
- `Source/LinqToDB/docs/**/*.md`
- `Source/LinqToDB/docs/api.md`
- `Source/LinqToDB/docs/hints-api-map.md`
- current generated package XML-doc, normally:
  `P:\linq2db\.build\bin\LinqToDB\net10.0\linq2db.xml`

Do not use old generated pack files, caches, indexes, previous extracts, or old uploaded
Knowledge files as source material.

Do not add rules that exist only in the knowledge pack. If a rule seems necessary for retrieval or
agent behavior, add it to the package documentation first, then regenerate or recopy the knowledge
pack.

## Refresh Steps

1. Build `P:\linq2db\Source\LinqToDB\LinqToDB.csproj` for the current package TFM.
   This regenerates `Source/LinqToDB/docs/api.md` and the XML-doc artifact.
2. Rebuild the entire pack from source. Do not partially update one knowledge file.
3. Preserve source meaning and source order. Do not add independent explanations, examples, APIs,
   provider capabilities, or fallback policies.
4. Remove old numbered upload files that are no longer part of the layout.
5. Do not create or depend on `xml-doc-index.json` or other caches/indexes.

## Conversion Rules

1. Preserve the semantic order from the source documentation. Do not move generic fallback guidance
   before typed, provider-specific, or exact API lookup guidance.
2. Prefer copying files as-is. Allowed transformations are:
   - file renaming for upload ordering;
   - concatenating package docs into a bundle when the target system needs fewer files;
   - rewriting package-local links so they point to the converted file names;
   - rewriting links to `linq2db.xml` or generated API pages to the converted API/XML files;
   - adding a short generated header with source file names and generation time;
   - collapsing accidental consecutive horizontal-rule-only separators into a single separator;
   - normalizing generated markdown to CRLF line endings;
   - omitting repository-local maintenance files from the upload set.
3. Do not rewrite text inside inline code spans or fenced code blocks.
4. Do not invent examples, API names, overloads, provider capabilities, or fallback policies.
5. Do not summarize away `Stop` blocks, `You are here if you need to` blocks, `AI-Tags`,
   XML member ids, or generated API extract rows.
   Preserve `Search anchors:` lines in generated API extract files. They are the primary retrieval
   surface for agents; XML member ids are exact lookup keys after a candidate is found.
6. Keep more specific source files before broader files when concatenation is unavoidable, but treat
   that order as preserving the package documentation model, not as a separate knowledge-pack rule.
7. Preserve negative lookup rules. If source docs say that an API absence claim requires exact map
   lookup plus `docs/api.md` / XML-doc lookup, the converted pack must keep that rule visible near
   the corresponding workflow.
8. Read source markdown and XML as UTF-8 and write generated markdown as UTF-8.
9. Keep CRLF line endings in generated markdown files.

## Upload Set

Upload only these numbered markdown files to Custom GPT Knowledge:

1. `01-agent-guide.md`
2. `02-skill.md`
3. `03-overview-readme.md`
4. `04-api-discovery-and-extract.md`
5. `05-architecture.md`
6. `06-agent-antipatterns-and-ai-tags.md`
7. `07-provider-configuration.md`
8. `08-mapping.md`
9. `09-crud-and-merge.md`
10. `10-query-composition.md`
11. `11-hints.md`
12. `12-hints-api-map.md`
13. `13-custom-sql.md`
14. `14-translatable-methods.md`
15. `15-interceptors.md`
16. `16-xml-doc.md`

Do not upload supporting files. `custom-gpt-instructions.md` goes into the GPT Instructions field.

## Source-To-Bundle Mapping

| Output file | Source material |
| --- | --- |
| `01-agent-guide.md` | `AGENT_GUIDE.md` |
| `02-skill.md` | `SKILL.md` |
| `03-overview-readme.md` | `readme.md` |
| `04-api-discovery-and-extract.md` | `docs/api.md` |
| `05-architecture.md` | `docs/architecture.md` |
| `06-agent-antipatterns-and-ai-tags.md` | `docs/agent-antipatterns.md`, `docs/ai-tags.md` |
| `07-provider-configuration.md` | `docs/provider-capabilities.md`, `docs/provider-setup.md`, `docs/configuration.md` |
| `08-mapping.md` | `docs/mapping.md` |
| `09-crud-and-merge.md` | `docs/crud/*.md` in source order |
| `10-query-composition.md` | `docs/query-cte.md`, `docs/query-temp-tables.md` |
| `11-hints.md` | `docs/hints.md` |
| `12-hints-api-map.md` | `docs/hints-api-map.md` |
| `13-custom-sql.md` | `docs/custom-sql.md` |
| `14-translatable-methods.md` | `docs/translatable-methods.md` |
| `15-interceptors.md` | `docs/interceptors.md` |
| `16-xml-doc.md` | current `linq2db.xml` parsed directly |

The hint route must remain visible as separate files:

`11-hints.md` -> `12-hints-api-map.md` -> `04-api-discovery-and-extract.md` -> `16-xml-doc.md`.

This corresponds to the package documentation route:

`docs/hints.md` -> `docs/hints-api-map.md` -> `docs/api.md` -> `linq2db.xml`.

Keep hint scope semantics visible during conversion. In particular, `TablesInScope` helpers must be
documented as applying to the query/subquery they are called on, not to tables added later by outer
composition. The converted pack must preserve the distinction between:

- table receiver overloads: one table source;
- queryable receiver overloads with `HintType=TablesInScope`: all table references already present
  in the composed query scope.

Do not rewrite examples so a scope helper is attached to the first table before joins are composed.

## Link Rewrite Rules

Rewrite markdown links only outside inline code and fenced code.

Examples:

- `[AGENT_GUIDE.md](AGENT_GUIDE.md)` -> `[AGENT_GUIDE.md](01-agent-guide.md)`
- `[docs/hints-api-map.md](docs/hints-api-map.md)` -> `[docs/hints-api-map.md](12-hints-api-map.md)`
- `[linq2db.xml](lib/net10.0/linq2db.xml)` -> `[linq2db.xml](16-xml-doc.md)`
- API-page links such as `https://linq2db.github.io/api/...` -> `16-xml-doc.md` when they
  refer to generated package API docs.

Do not transform code spans such as `` `lib/<TFM>/linq2db.xml` ``.

## Required Validation

After preparing the knowledge pack:

1. Verify that every output file can be traced to package-local source files.
2. Verify that links do not point to repository-only paths unless the target pack intentionally
   includes those files.
3. Verify that no link rewrite touched inline code or fenced code blocks.
4. Verify that `docs/api.md` entries still contain XML member ids.
5. Verify that `docs/api.md` entries still contain `Search anchors:` lines and that the converted
   workflow tells agents to search anchors before XML member ids.
6. Verify that provider-specific hint questions can route through:
   `docs/hints.md` -> `docs/hints-api-map.md` -> `docs/api.md` -> `linq2db.xml`.
7. Verify that `TablesInScope` examples and API entries preserve query-scope placement: the helper
   is applied to the composed query/subquery containing the target tables.
8. Verify that generic fallback APIs do not appear earlier than typed/provider-specific lookup
   guidance in the same converted bundle.
9. Verify that negative hint lookup rules still require exact provider + SQL term lookup in the map
   and `docs/api.md` / XML-doc lookup before claiming a typed helper is absent.
10. Verify that generated files do not contain mojibake or XML parser artifacts.
11. Verify that generated files do not contain long runs of horizontal-rule-only lines.
12. Verify that generated markdown files use CRLF line endings.

## Validation Commands

Run these checks after each refresh.

```powershell
# exactly 16 uploadable Knowledge files
(Get-ChildItem -LiteralPath P:\linq2db.Expert -File | Where-Object { $_.Name -match '^\d\d-.*\.md$' } | Measure-Object).Count

# source markdown inputs are represented in bundle-manifest.json
$expected = @('AGENT_GUIDE.md','SKILL.md','readme.md') + (Get-ChildItem -LiteralPath P:\linq2db\Source\LinqToDB\docs -Recurse -File -Filter *.md | ForEach-Object { $_.FullName.Substring('P:\linq2db\Source\LinqToDB\'.Length) -replace '\\','/' }) | Sort-Object
$manifest = Get-Content -LiteralPath P:\linq2db.Expert\bundle-manifest.json -Raw -Encoding UTF8 | ConvertFrom-Json
Compare-Object -ReferenceObject $expected -DifferenceObject (@($manifest.included_docs) | Sort-Object)

# no stale package-local markdown links in upload files
rg -n -g "[0-9][0-9]-*.md" "\]\((\.\./|\.\\|docs/|crud/|AGENT_GUIDE\.md|SKILL\.md|[^)]*linq2db\.xml|https://github\.com/linq2db/linq2db/blob/master/docs/|https://linq2db\.github\.io/api/)" P:\linq2db.Expert

# no mojibake or XML parser artifacts
rg -n -g "[0-9][0-9]-*.md" "System\.Xml\.XmlElement|вЂ|вљ|Р |РЎ|пёЏ|РІ" P:\linq2db.Expert

# no accidental runs of horizontal-rule-only separators
Get-ChildItem -LiteralPath P:\linq2db.Expert -File -Filter *.md | ForEach-Object {
    $text = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($_.FullName))
    if ([regex]::IsMatch($text, "(?m)^(---\r?\n\s*){3,}")) { $_.Name }
}

# API extract discovery surface is present and ordered before XML member lookup
Select-String -LiteralPath P:\linq2db.Expert\04-api-discovery-and-extract.md -Pattern 'Search anchors.*primary discovery|XML member.*after a candidate'
Select-String -LiteralPath P:\linq2db.Expert\04-api-discovery-and-extract.md -Pattern '^Search anchors: .*Group=Hints'

# provider-specific hint route canaries
Select-String -LiteralPath P:\linq2db.Expert\12-hints-api-map.md -Pattern '^\| `FINAL`|^\| `NOLOCK`|^\| `FOR UPDATE`'
Select-String -LiteralPath P:\linq2db.Expert\04-api-discovery-and-extract.md -Pattern 'Group=Hints|HintType=Table|HintType=TablesInScope'
Select-String -LiteralPath P:\linq2db.Expert\16-xml-doc.md -Pattern 'Group=Hints|HintType=Table|HintType=TablesInScope'

# negative lookup guardrails remain visible
Select-String -LiteralPath P:\linq2db.Expert\11-hints.md,P:\linq2db.Expert\12-hints-api-map.md,P:\linq2db.Expert\04-api-discovery-and-extract.md -Pattern 'exact provider.*exact SQL|typed helper is absent|negative lookup'

# scope route and placement guidance
Select-String -LiteralPath P:\linq2db.Expert\11-hints.md -Pattern 'composed query scope|already contains|first table before joins'
Select-String -LiteralPath P:\linq2db.Expert\12-hints-api-map.md -Pattern 'TablesInScope|Table receiver affects only that table source'
Select-String -LiteralPath P:\linq2db.Expert\04-api-discovery-and-extract.md -Pattern 'HintType=TablesInScope|all tables already present in the query scope'
Select-String -LiteralPath P:\linq2db.Expert\16-xml-doc.md -Pattern 'HintType=TablesInScope|all tables already present in the query scope'

# generated markdown has no LF-only line endings
Get-ChildItem -LiteralPath P:\linq2db.Expert -File | Where-Object { $_.Name -match '^\d\d-.*\.md$' } | ForEach-Object {
    $text = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($_.FullName))
    [pscustomobject]@{ File = $_.Name; HasLfWithoutCr = [regex]::IsMatch($text, '(?<!\r)\n') }
} | Where-Object { $_.HasLfWithoutCr }
```

Expected result:

- uploadable count is `16`;
- manifest diff is empty;
- stale-link and mojibake checks return no matches;
- horizontal-rule run check returns no rows;
- generated API extract preserves `Search anchors` and the workflow tells agents to search them
  before XML member ids;
- provider-specific hint canaries are present in the map, generated API extract, and XML-doc extract;
- negative lookup guardrails require exact map and API/XML-doc lookup before absence claims;
- scope guidance is present in hints, hint map, generated API extract, and XML-doc extract;
- CRLF check returns no rows.

## Upload Procedure

1. In the Custom GPT editor, replace Knowledge with only the 16 numbered `.md` files.
2. Paste `custom-gpt-instructions.md` into the Instructions field and save.
3. Start a new chat before validation; existing chats can keep stale retrieved context.
4. Validate with several provider-specific hint questions, for example ClickHouse `FINAL`,
   SQL Server `NOLOCK`, and PostgreSQL `FOR UPDATE`. Do not add special rules for the canary
   questions; fix package documentation if a route is unclear.
