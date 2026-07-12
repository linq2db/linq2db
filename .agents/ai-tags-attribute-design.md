# AI-Tags: XML-doc Tag vs Typed Attribute — Design Decision

Context: PR #5376. MaceWindu raised (2026-05-30) whether inline `<ai-tags />` in XML-doc is the
right mechanism at all, versus a typed attribute or a sidecar spec. This note captures the decision
reached while working through the tradeoffs, so it isn't lost between sessions.

Status: **migration complete, including cleanup.** All 777 `<ai-tags>`/`<ai-tags-defaults>`
instances converted to `AiTagsAttribute`/`AiTagsDefaultsAttribute`; 0 remaining in XML-doc. The
legacy XML-doc extraction path in `GenerateApiDocs.ps1` is removed (replaced with a hard error if
the XML-doc form is ever used again); `docs/api.md` is regenerated and current. Still open: whether
to post this as a follow-up in the PR thread.

## Options considered

1. **Keep XML-doc `<ai-tags />`, close the CI gap.** `GenerateAiDocs` (the target that regenerates
   `docs/api.md` and validates the `<ai-tags>` vocabulary) is opt-in
   (`Condition="'$(GenerateAiDocsOnBuild)'=='true' ..."` in `Source/LinqToDB/LinqToDB.csproj`) — not
   part of the default build or CI. The validator itself (`Build/GenerateApiDocs.ps1`) is strict
   (hard `throw` on unknown attribute/value), but nothing forces it to run. Cheapest fix: make it a
   required CI step. Preserves co-location (tag sits in the same diff hunk as the member) and adds
   zero public API surface.
2. **Sidecar file** (member-ID-keyed JSON/YAML, OpenAPI-style). Rejected: loses co-location (the
   empirically-observed reason the current mechanism hasn't drifted despite no CI gate), and
   reproduces the same unmitigated drift risk already present in `Source/Knowledge/linq2db-expert/`
   (hand-maintained mapping, no content-hash staleness detection, no CI gate). `docs/api.md` is
   already effectively the "external file for tooling" as a generated *output* — a hand-authored
   sidecar as the *source* adds a new drift surface without a compensating benefit.
3. **Typed attribute, `internal`, enum-backed.** Selected. Rationale below.

## Why typed attribute

Compiler-validated vocabulary was the entire point of considering attribute over XML-doc-tag — an
`enum`-backed attribute makes an invalid value a compile error, versus today's free-text-in-comment
which is only checked by the (opt-in, unenforced) generator script. A `string`-based attribute would
not buy this and isn't worth the migration cost on its own.

`internal` visibility removes the main objection (no `PublicAPI.Unshipped.txt` entries, no per-TFM
baseline, no `CompatibilitySuppressions.xml` cost) while keeping the one useful new capability over
XML-doc: reflectable via `CustomAttributeData`/`GetCustomAttributesData()` without the shipped `.xml`
doc file needing to be present — this works for `internal` attribute types too, since metadata-only
reflection doesn't enforce accessibility. Co-location is preserved (attribute sits directly on the
member, same as the tag does today).

The documented consumer (an offline agent grepping `docs/api.md`'s `Search anchors:` lines, per
`SKILL.md`) doesn't need runtime reflectability — so this isn't the deciding factor either way, but
it's a free side benefit at `internal` visibility, not a reason on its own to migrate.

## Shape

Two attribute types, matching today's `<ai-tags />` / `<ai-tags-defaults />` split — **not merged**.
Checked real usage (`git grep -B1 "<ai-tags-defaults"`) before assuming they could collapse into one:

```
CommandInfo.cs:            <ai-tags group="RawSQL" provider="ProviderDefined" />
                            <ai-tags-defaults group="RawSQL" pipeline="SqlText" provider="ProviderDefined" />

DataContextExtensions.cs:  <ai-tags groups="RawSQL,DML" provider="ProviderDefined" />
                            <ai-tags-defaults provider="ProviderDefined" />

DataExtensions.cs:         (no class-level <ai-tags> at all)
                            <ai-tags-defaults pipeline="ExpressionTree,SqlAST,SqlText" provider="ProviderDefined" />

DataOptionsExtensions.cs:  (no class-level <ai-tags> at all)
                            <ai-tags-defaults group="Configuration" execution="Immediate" composability="Composable"
                                               affects="Configuration" pipeline="..." provider="ProviderDefined" />

LinqExtensions.cs:         <ai-tags groups="..." pipeline="..." provider="ProviderDefined" />
                            <ai-tags-defaults pipeline="..." provider="ProviderDefined" />   (only case where they coincide)
```

Only `LinqExtensions.cs` has identical values between the two — the other four differ in key set
and/or value, and two have no class-level `<ai-tags>` at all. The two answer different questions
("what is this class itself" vs "what do members inherit if unset") and must stay separate types
with an identical full property set (defaults are not restricted to `Pipeline`/`Provider` in
practice, despite `ai-tags.md`'s illustrative example only showing those two).

```csharp
namespace LinqToDB.Internal.Metadata  // placement TBD — confirm no better-fitting existing namespace
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property |
                     AttributeTargets.Class | AttributeTargets.Interface,
                     AllowMultiple = false, Inherited = false)]
    internal sealed class AiTagsAttribute : Attribute
    {
        public AiGroup          Groups        { get; init; }
        public AiExecution?     Execution     { get; init; }
        public AiComposability? Composability { get; init; }
        public AiAffects        Affects       { get; init; }
        public AiPipeline       Pipeline      { get; init; }
        public AiProvider?      Provider      { get; init; }
        public AiHintType?      HintType      { get; init; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface,
                     AllowMultiple = false, Inherited = false)]
    internal sealed class AiTagsDefaultsAttribute : Attribute
    {
        public AiGroup          Groups        { get; init; }
        public AiExecution?     Execution     { get; init; }
        public AiComposability? Composability { get; init; }
        public AiAffects        Affects       { get; init; }
        public AiPipeline       Pipeline      { get; init; }
        public AiProvider?      Provider      { get; init; }
        public AiHintType?      HintType      { get; init; }
    }
}
```

`[Flags]` enums for the fields `ai-tags.md` documents as comma-separated (`Group`/`Groups`,
`Affects`, `Pipeline`) — bitwise-OR at call sites, e.g.
`Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText`. Matches the
existing `[Flags]` idiom already used in this codebase for combinable option sets
(`Source/LinqToDB/TableOptions.cs:54`), rather than introducing a different array-literal pattern.
Plain (non-flags) enums for strictly single-value fields (`Execution`, `Composability`, `Provider`,
`HintType`). The `group`/`groups` XML-attribute-name split collapses into one `Groups` property —
no reason to keep two names once it's a `[Flags]` type.

```csharp
[Flags] internal enum AiGroup   { None = 0, QueryDirectives = 1<<0, NavigationLoading = 1<<1, Hints = 1<<2, DML = 1<<3, Merge = 1<<4, Helpers = 1<<5, Configuration = 1<<6, Connection = 1<<7, RawSQL = 1<<8, Schema = 1<<9 }
internal enum AiExecution       { Deferred, Immediate }
internal enum AiComposability   { Composable, Terminal }
internal enum AiProvider        { ProviderDefined, ProviderAgnostic }
internal enum AiHintType        { Table, TablesInScope, Index, Join, SubQuery, Query, Merge, TableName }
[Flags] internal enum AiAffects   { None = 0, DmlStatement = 1<<0, DdlStatement = 1<<1, QueryRoot = 1<<2, QueryStructure = 1<<3, QueryCompilation = 1<<4, JoinGraph = 1<<5, SqlSemantics = 1<<6, CommandBuilder = 1<<7, Data = 1<<8, QueryResult = 1<<9, ExecutionContext = 1<<10, ConnectionConfiguration = 1<<11, Configuration = 1<<12, SchemaResult = 1<<13, GeneratedSql = 1<<14 }
[Flags] internal enum AiPipeline  { None = 0, ExpressionTree = 1<<0, SqlAST = 1<<1, SqlText = 1<<2, Connection = 1<<3, Execution = 1<<4, BulkInsert = 1<<5 }
```

## Example usage

```csharp
/// <summary>...</summary>
/// <remarks>...</remarks>
[AiTags(Groups = AiGroup.DML, Execution = AiExecution.Immediate, Composability = AiComposability.Terminal,
        Affects = AiAffects.DmlStatement,
        Pipeline = AiPipeline.ExpressionTree | AiPipeline.SqlAST | AiPipeline.SqlText,
        Provider = AiProvider.ProviderDefined)]
public static object InsertWithIdentity<T>(...)
```

## Generator merge semantics

Defaults-merge rule (from `ai-tags.md`, unchanged): start from `AiTagsDefaultsAttribute` on the
containing class/interface, apply member-level `AiTagsAttribute` on top, member value wins per key,
absent member key inherits the default.

To detect "key absent" correctly, the generator must read attributes via
`CustomAttributeData`/`GetCustomAttributesData()` (metadata-only reflection), not
`GetCustomAttribute<T>()`. Only explicitly-set named arguments appear in
`CustomAttributeData.NamedArguments` — an unset property doesn't appear in the list at all, so
presence/absence *is* the "was this explicitly specified" signal. A strongly-typed
`GetCustomAttribute<T>()` read loses this distinction: an unset property silently reads back as the
CLR default (`0`/`null`), indistinguishable from "explicitly set to that value."

## Progress

1. **Audit — done.** Parsed all 747 `<ai-tags>`/`<ai-tags-defaults>` instances in the tree
   (`grep -rhoE "<ai-tags(-defaults)?[^/]*/>"`) and diffed every distinct value per attribute key
   against the enums above. Zero gaps - every value in real usage maps cleanly onto the drafted
   enums.
2. **Flags decided** - bitwise-OR, consistent with `TableOptions` (`Source/LinqToDB/TableOptions.cs:54`).
   No array-typed property needed.
3. **Types implemented** - `Source/LinqToDB/Internal/Metadata/*.cs`, one type per file (`AiTagsAttribute.cs`,
   `AiTagsDefaultsAttribute.cs`, `AiGroup.cs`, `AiExecution.cs`, `AiComposability.cs`, `AiProvider.cs`,
   `AiHintType.cs`, `AiAffects.cs`, `AiPipeline.cs`) - MA0048 (Meziantou.Analyzer, Release-only) requires
   one type per file; a combined file built fine under `-c Testing` but failed `-c Release`. All files
   need UTF-8 BOM (`.editorconfig` `charset = utf-8-bom` for `*.cs`) - `Write`/`Edit` don't add it,
   had to backfill via `UTF8Encoding($true)`.
4. **C# constraint found**: `Nullable<TEnum>` is not a valid attribute parameter type (CS0655).
   Properties are plain (non-nullable) enums; "was this explicitly set" is read from
   `CustomAttributeData.NamedArguments` presence, not from property nullability (see generator notes
   below) - nullability was never actually needed for this.
5. **7 real members converted** as a proof of concept (not the full ~700): `LinqExtensions.Insert.cs`
   `InsertWithIdentity<T>`, `PostgreSQLHints.cs` `SubQueryTableHint` (both overloads),
   `Data/CommandInfo.cs`, `Data/DataContextExtensions.cs`, `DataExtensions.cs`,
   `DataOptionsExtensions.cs`, `LinqExtensions/LinqExtensions.cs` (the last five are class-level
   `[AiTags]`/`[AiTagsDefaults]`). Covers every real pattern found in the audit: plain member, hint
   member with `HintType`, class-tag+defaults that differ, class-tag+defaults that coincide,
   defaults-only-no-class-tag.
6. **`Build/GenerateApiDocs.ps1` rewritten** to read `AiTagsAttribute`/`AiTagsDefaultsAttribute` via
   `CustomAttributeData` off the compiled assembly, with a **hybrid fallback**: members not yet
   migrated keep working through the legacy XML-doc `<ai-tags>` path unchanged. New mandatory
   `-AssemblyPath` parameter; `Source/LinqToDB/LinqToDB.csproj`'s `GenerateAiDocs` target updated to
   pass `$(TargetDir)$(AssemblyName).dll`.
   - Implemented `GetMemberDocId`/`GetParamTypeDocName` - a from-scratch C# XML-doc-comment-ID
     encoder (`T:`/`M:`/`P:`/`F:`/`E:` + generic arity + parameter list, handling generic parameter
     back-references, arrays, by-ref, generic instantiations). Verified byte-for-byte against the
     real compiler-emitted ID for `InsertWithIdentity``1(...)` - matches exactly, including the
     ` `` ` (double-backtick, method-level generic parameter) encoding inside a nested
     `Expression{Func{``0}}`.
   - **Two runtime bugs found and fixed during verification** (both worth remembering):
     - `Assembly.GetTypes()` throws `ReflectionTypeLoadException` when any type in the assembly
       fails to fully resolve - must catch it and use `.Exception.Types | Where-Object { $_ -ne $null }`.
     - Invoking the script via plain `powershell` (Windows PowerShell 5.1 / .NET Framework CLR)
       cannot load a net10.0-targeted assembly at all (`FileNotFoundException` on `System.Runtime,
       Version=10.0.0.0`) - must be `pwsh`. This empirically confirms the Copilot review comment on
       this exact target (flagged `powershell` vs `pwsh` as a cross-platform concern) was a real bug,
       not just style - fixed in the csproj `<Exec>` too.
7. **Verified correct** by three-way comparison on the same freshly-built XML doc + assembly
   (`.build/bin/LinqToDB/Testing/net10.0/`):
   - Old generator (git `HEAD` version) vs new generator on identical input: 67-line diff, every line
     accounted for - header text change, `Group=`→`Groups=` rename (intended), canonical flag
     ordering (intended, see below), and the 7 converted members' new `AiTags`-sourced output.
   - The **un-migrated sibling overload** of `InsertWithIdentity` (the `IValueInsertable<T>` receiver
     one, not converted) correctly still shows old `Group=DML` output via the XML fallback path in
     the *same* generator run - proves the hybrid mode doesn't cross-contaminate between migrated and
     un-migrated members even within one overload family.
   - Re-ran after the one-type-per-file split + BOM fix: output byte-identical to the pre-split run.
   - **Caveat on the first, larger diff attempt**: comparing new-generator-output against the
     *checked-in* `docs/api.md` produced a 9500+ line diff dominated by member-count drift (5130 vs
     4133 XML members scanned) unrelated to this change - the checked-in file is stale relative to
     current source. This is a live instance of the exact "no CI gate on `GenerateAiDocs`" drift risk
     flagged earlier in this document and in the original audit; not something to fix as part of this
     migration, but worth surfacing.
   - Full build green on `Testing`/net10.0, `Release`/net10.0, `Release`/netstandard2.0, `Release`/net462
     (portable-TFM and analyzer gates both pass; `init`-accessor polyfill already available on the
     downlevel TFMs).

## Migration complete

`.agents/scripts/Convert-AiTagsToAttributes.ps1` mechanically converted the remaining instances
across 42 files (35 `.cs` + 7 `.tt` templates; the corresponding 7 `.generated.cs` T4 outputs were
converted directly with the same transform rather than by re-running T4, so source and generated
output stay textually consistent without a T4 tooling dependency). Combined with the 7-member
hand-converted pilot: **777 `[AiTags]`/`[AiTagsDefaults]` attribute declarations, 0 remaining
XML-doc `<ai-tags>` anywhere in `Source/LinqToDB`.**

Confirmed the "convert `.generated.cs` directly instead of re-running T4" shortcut is actually
sound for these 7 templates specifically, not just assumed: installed `dotnet-t4`
(`dotnet tool install -g dotnet-t4`, not a repo dependency) and regenerated all 7 hint
`.generated.cs` files for real from the now-converted `.tt` templates via the plain `t4` CLI with no
extra flags. Byte-for-byte identical to the manually-edited versions in every case, aside from the
UTF-8 BOM the raw `t4` CLI doesn't add (kept intentionally on the manual edits per `.editorconfig`
`charset = utf-8-bom` for `*.cs`; the repo's checked-in `.generated.cs` already carried it before
this change too, so `t4`'s own default was never actually what shipped).

**Scope of this verification is narrow - all 7 hint templates only use
`<#@ assembly name="System.Core" #>` and `<#@ import #>` of stock BCL namespaces, no
`<#@ include #>` and no custom/project assembly references.** That's why the bare `t4` CLI could
resolve them with zero extra `-r`/`-P`/`-u` flags. This does not generalize to a claim that
`dotnet-t4` can regenerate arbitrary templates elsewhere in the repo (e.g. `LinqToDB.Scaffold` or
`Tests.T4`, which likely use `<#@ include #>` and reference project assemblies) - those would need
MSBuild-integrated T4 tooling or explicit reference/include-path flags, untested here.

Two real bugs found and fixed while building the script (both worth remembering for the next
regex-over-C#-source script):
- .NET regex `$` in `(?m)` multiline mode matches just before `\n`, not before `\r\n` - `[ \t]*$`
  silently failed to match every real (CRLF) file even though it matched hand-typed LF test
  strings. Fixed with a captured `(\r?)` group re-emitted in the replacement.
- `<ai-tags />` is not always the last XML-doc tag in a member's comment block (e.g.
  `<remarks>` → `<ai-tags />` → `<returns>`, or a positional record's `<ai-tags />` followed by
  several `<param>` tags). Converting in place there splits the `///` block around a C# attribute,
  which is invalid (CS1587). Fixed with a second pass (`FloatAttributesPastTrailingDocs`) that
  moves a converted attribute line down past any `///` lines immediately following it, landing it
  right before the real declaration.

**Verification**, isolating the conversion's effect from the pre-existing `docs/api.md` staleness
(see below) via `git stash`: ran the generator on the identical current (non-stale) XML doc/assembly
twice - once with only the 7-member pilot present (740 members still on the XML path) and once with
the full 777-attribute migration applied. Same member universe both times (`XML members scanned:
4133`, `API families: 2281` - identical). "Included members with AI metadata" went from 270 to 339,
and every line in the diff is either a `Group=`→`Groups=` rename or a **strict superset** of the
previous content (no row lost data, several rows gained fields). Tracked the surplus down: the old
XML-based `ExtractAiTagsFromXml` never implemented the `<ai-tags-defaults>` merge that
`docs/ai-tags.md` documents (`CommandInfo.Execute` etc. showed only `Execution=...;
Composability=...; Affects=...;` before - missing `Groups`/`Pipeline`/`Provider` inherited from the
class's defaults). The new attribute-based `FormatMergedAiTags` implements the merge correctly, so
this is a real, incidental bug fix, not conversion noise. Full build green across `Testing`/net10.0,
`Release`/{net10.0, net8.0, net9.0, netstandard2.0, net462}, plus `Tests/Linq/Tests.csproj`.

## Cleanup done

- `docs/api.md` regenerated: picked up both the current member set (was stale independent of this
  migration - 5130 -> 4133 XML members scanned, unrelated codebase drift) and the defaults-merge
  bug fix as real content changes.
- Legacy XML `<ai-tags>` path removed from `GenerateApiDocs.ps1`
  (`ValidateAiTagElement`/`FormatAiTagElement`/`ExtractAiTagsFromXml`/the regex fallback are gone).
  Replaced with `AssertNoLegacyAiTagXml`, which throws if the XML-doc form is used again, rather
  than silently accepting or silently ignoring it.
- Found and filed separately (not part of this migration, pure pre-existing XML-doc-generation
  quirk): `LinqToDB.Common.Configuration.Linq.PreferClientCalculation` compiles and exists in the
  assembly but is absent from the generated `.xml` doc file entirely - task_7ef5137b.

## Remaining work

1. Decide whether/when to post this as the promised follow-up to MaceWindu on PR #5376.
