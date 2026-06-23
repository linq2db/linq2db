# Adding a new provider version (dialect anchor)

How to add a new version anchor to a versioned provider (PostgreSQL, SQL Server, Firebird, MySQL, Oracle, DB2, …) without leaving silently-broken paths. Written from the PG19 work on #5644, where the core wiring was complete but four **version-keyed lists** were missed and only surfaced at CI.

## The trap

Adding a version is two layers of work:

1. **Core anchor wiring** — necessary, and easy to remember because it won't compile without it. For PostgreSQL that was: `PostgreSQLVersion.v19`, `ProviderName.PostgreSQL19`, `PostgreSQLDataProvider19`, `PostgreSQLMappingSchema.PostgreSQL19MappingSchema`, the version detector (`>= 19`, config-name, factory `"19"`), the `ProviderDetector` / `GetConfigurationName` / mapping-schema switches, and `PublicAPI.Unshipped.txt`.

2. **Additive `ProviderName`-keyed lists** — the trap. These are scattered allowlists that enumerate the specific versions a behaviour applies to. They are **not cumulative chains**: the new anchor's `ConfigurationList` does not contain older anchors, so an un-extended list *silently excludes* the new version. There is no compile error — the fast `Testing` / single-`net10.0` build is green, and the gap only shows up at runtime / full CI.

## The lists to extend (PostgreSQL worked example)

When adding `PostgreSQL.<nn>`, grep-and-extend every site below. The general category is named first so the equivalent applies to other providers.

| Category | PG19 site | Symptom if missed |
|---|---|---|
| SQL-builder / hint version whitelist (`ConfigurationList.Contains(ProviderName.X)`) | `Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLHints.cs` → `SubQueryTableHintExtensionBuilder` `SKIP LOCKED` whitelist | `SubQueryTableHint` drops `SKIP LOCKED` (`TableHintTest2` red) |
| EF Core provider mapping switch | `Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFToolsImplDefault.cs` → `CreateLinqToDBDataProvider` `ProviderName.X => CreatePostgreSqlProvider(v..)` | new provider name falls through to the default arm |
| LINQPad dialect dropdown | `Source/LinqToDB.LINQPad/DatabaseProviders/PostgreSQLProvider.cs` → `_providers` list | version absent from the LINQPad dialect picker |
| Per-version test column / feature mapping | `Tests/Linq/DataProvider/PostgreSQLTests.cs` → `AllTypes.macaddr8DataType` `[Column(DbType="macaddr8", Configuration=…)]` stack | column unmapped → `BulkCopyTest` / `BulkCopyTestAsync` round-trip mismatch |

There may be more for other providers (SQL builder feature flags gated on specific version names, mapping-schema type registrations, etc.). Treat the table as a starting point, not an exhaustive list.

## Detection technique

Grep the **previous anchor's** `ProviderName` constant everywhere, and treat each hit as a candidate site for the new one:

```
Grep  ProviderName.PostgreSQL18   across Source/ , the EF/LINQPad satellites, and Tests/
```

Filter out: cumulative test-chain definitions like `TestProvName.AllPostgreSQL<nn>Plus` (those propagate the new version automatically — `AllPostgreSQL18Plus = $"{PostgreSQL18},{AllPostgreSQL19Plus}"`), and the core-wiring switches you already added. What remains are the additive whitelists.

Why grep instead of trusting the build: these lists are data, not dispatch — omitting an entry changes behaviour, not types. Only a Release / multi-TFM CI run (or a live test against the new version's DB) catches them.

## Verifying a brand-new version locally

Neither the primary clone nor the sibling clone's `UserDataProviders.json` will have the new version's connection yet (it lives only in the PR's `UserDataProviders.json.template`). To run tests against it:

1. Start the container (`docker start pgsql<nn>`; the PR's `Data/Setup Scripts/pgsql<nn>.cmd` creates it).
2. In the **worktree** running the change, hand-write `UserDataProviders.json` from the `.template` connection entry (PG19 = `Server=localhost;Port=5419;…`), enabling only the new provider on the fast TFM bucket (NET100). It's gitignored — scratch, not committed.
3. Run via `/test … worktree <abs-path>` (see [`worktree.md`](worktree.md) → *Running tests from a worktree*), prefixing `FullyQualifiedName~CreateData.CreateDatabase|` to initialise the empty DB before the target tests.

## After the merge / GA

- Update [`test-databases.md`](test-databases.md)'s provider table with the new version row.
- For a beta image (PG19 shipped as `postgres:19beta1`), bump the tag to GA across `pgsql<nn>.cmd`, `Build/Azure/scripts/pgsql<nn>.sh`, and `mac.pgsql<nn>.sh` once the stable image ships.
