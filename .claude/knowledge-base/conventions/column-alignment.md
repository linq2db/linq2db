---
area: GLOBAL
kind: convention
sources: [code]
confidence: high
last_verified: 2026-05-08
last_verified_sha: d52a4ed0e83317cc11b2ef3d41ddf4ba70acf415
---

# Column-Aligned Formatting

## Rule

Large blocks of the codebase use **intentional column alignment** -- property declarations line their `{ get; set; }` up at the same column, constructor parameters align their defaults, and multi-variable declarations align their `=` signs. This is deliberate house style. Preserve it when editing; match the alignment of surrounding code rather than reformatting to a narrower width.

Authoritatively documented in [code-design.md](../../docs/code-design.md) section Column-aligned formatting is intentional, with the operational guardrail in [agent-rules.md](../../docs/agent-rules.md) section Agent Guardrails.

## Why

The alignment makes large tables of flags and settings (e.g. `SqlProviderFlags`) scannable as a grid -- a reader can visually scan the name column or the type column without tracking whitespace noise. Reformatting these blocks into ragged style destroys the scannable structure.

## When NOT to flag

Flag alignment issues only on **lines the PR itself adds or modifies**. Never flag alignment on untouched context lines -- that would be reformatting unrelated code. Legitimate new-line flags: trailing whitespace, 3+ consecutive blank lines, mixed tabs/spaces causing visible misalignment, indentation not matching enclosing scope.

## Examples

**Aligned property declarations -- `SqlProviderFlags`:**
```csharp
// Source/LinqToDB/Internal/SqlProvider/SqlProviderFlags.cs:32-59
public bool        IsParameterOrderDependent      { get; set; }
public bool        AcceptsTakeAsParameter         { get; set; }
public bool        AcceptsTakeAsParameterIfSkip   { get; set; }
public bool        IsSkipSupported                { get; set; }
public bool        IsSkipSupportedIfTake          { get; set; }
public TakeHints?  TakeHintsSupported              { get; set; }
```
Names align at column 16; `{ get; set; }` aligns at column 47.

**Aligned variable declarations -- `BasicSqlBuilder` MERGE construction:**
```csharp
// Source/LinqToDB/Internal/SqlProvider/BasicSqlBuilder.cs:1233-1235
var targetAlias = ConvertInline(insertOrUpdate.SelectQuery.From.Tables[0].Alias!, ConvertType.NameToQueryTableAlias);
var sourceAlias = ConvertInline(GetTempAliases(1, "s")[0],                        ConvertType.NameToQueryTableAlias);
var keys        = insertOrUpdate.Update.Keys;
```
The `=` signs align at the same column; trailing args pad to the same column.

**Aligned sealed subclass declarations -- provider variants:**
```csharp
// Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseDataProvider.cs:26-28
sealed class ClickHouseOctonicaDataProvider : ClickHouseDataProvider { public ClickHouseOctonicaDataProvider() : base(ProviderName.ClickHouseOctonica, ClickHouseProvider.Octonica        ) { } }
sealed class ClickHouseDriverDataProvider   : ClickHouseDataProvider { public ClickHouseDriverDataProvider() : base(ProviderName.ClickHouseDriver, ClickHouseProvider.ClickHouseDriver) { } }
sealed class ClickHouseMySqlDataProvider    : ClickHouseDataProvider { public ClickHouseMySqlDataProvider   () : base(ProviderName.ClickHouseMySql   , ClickHouseProvider.MySqlConnector  ) { } }
```
Constructor call arguments padded to align across all three rows.

**Aligned `SetConvertExpression` calls -- `SerializationMappingSchema`:**
```csharp
// Source/LinqToDB/Internal/Remote/SerializationMappingSchema.cs:20-25
SetConvertExpression<bool          , string>(value => value ? "1" : "0");
SetConvertExpression<int           , string>(value => value.ToString(CultureInfo.InvariantCulture));
SetConvertExpression<byte          , string>(value => value.ToString(CultureInfo.InvariantCulture));
SetConvertExpression<sbyte         , string>(value => value.ToString(CultureInfo.InvariantCulture));
SetConvertExpression<long          , string>(value => value.ToString(CultureInfo.InvariantCulture));
```
Type-argument type names padded so the `, string>` column aligns.

## See also

- [code-design.md](../../docs/code-design.md) -- canonical statement of the rule
- [agent-rules.md](../../docs/agent-rules.md) section Agent Guardrails -- operational version: what reviewers enforce on new lines
