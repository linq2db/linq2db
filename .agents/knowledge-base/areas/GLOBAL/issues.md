---
area: GLOBAL
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-07-06
last_verified_sha: 
---

# GLOBAL -- GitHub themes

## Open themes

- **Mapping and inheritance** -- Complex mapping scenarios including inheritance filter review (#4305), composite property auto-mapping (#2873), associations with composite objects (#4139), multi-level inheritance edge cases (#3184, #4364), and fluent API configuration scope limitations (#3041, #3136, #4073, #4893). InheritanceMapping values not preserved in MultiInsert (#2988).

- **SQL generation and optimization** -- SQL Server-specific patterns: NULL handling / ISNULL optimization (#3314), TimeSpan mapping to `time` datatype (#4306, #4308), parameterization improvements with interpolated strings (#2266), temp table collation errors (#4598), case-sensitive schema testing (#4658). Cross-provider: function call support vs stored procedures (#1857), LoadWith optimization (#4588), SqlRow list handling (#3631), and SQL divergence between direct and remote contexts (#5169). Generic SQL generation backlog (#4745).

- **Query execution performance** -- Bulk operation tuning: parameter accessor compilation overhead for many tables (#4724), Collection.Contains optimization (#4600), EXISTS/NOT EXISTS join optimization (#2815), remoting performance and serialization (#3800). Regressions: optimization disabled by #5074 (#5081). API gaps: no SumAsync for BigInteger (#4040), new LINQ methods for .NET 9.0/10 (#4412).

- **Association and relationship handling** -- Duplicate query generation on association loads (#3806), lack of many-to-many support (#2888), duplicate entity objects with same PrimaryKey but incomplete data (#4059). Scope includes intersection of association properties and composite-object mapping.

- **Remote query execution and LinqService** -- LinqService extensibility requests (#1949), batched query execution without multiple round trips (#1908), SQL divergence between direct and remote contexts (#5169). No GetSchemaProvider() implementation for YDB (#5169 adjacent).

- **Identity column insertion** -- Insert() overloads that preserve identity values (#5021), support for identity insertion into temp tables via IntoTempTable() with IQueryable source (#3795), MultiInsert not handling InheritanceMapping (#2988).

- **Transaction and concurrency handling** -- Transaction savepoints support (#1935), retry mechanisms similar to EF execution strategy (#3219), concurrency detection and thread-safety validation (#4405, #4420 draft).

- **Type system and conversion** -- DateTimeOffset.DateTime sorting (#5435), NodaTime Instant parameter type inference (#5549), type-conversion gaps, char null valueConverter (#5654), F# option types (#195). Constant/parameter column type resolution in UNION queries (#3360).

- **Expression translation and compilation** -- CompiledQuery with async operations (#3266), expression translation improvements, compiled query caching and performance.

## Resolved themes

- **Type system and mapping** -- DateTimeOffset.DateTime sorting (#5435), NodaTime Instant? parameter type inference (#5549), type-conversion gaps, char null valueConverter (#5654), F# option types (#195).

- **Build and infrastructure** -- Intermittent test failures (Linux DB2/Informix libdb2.so loading #5538), NuGet pack HintPath resolution, analyzer rules deferred from 6.3.0 release (#5532).

- **Aggregate functions and unions** -- Aggregate functions inside UNION queries (#5616), trim with character parameters (#3296), analytic functions with GROUP BY (#3127).

- **Feature implementations** -- Trim with character parameters (#3296), InsertIfNotExists operation (#2528), UUIDv7 support (#5646), window-function template cleanup (#5674).

## Active discussions

- [Make association properties can be subclass of types implemented IEnumberable<T>](https://github.com/linq2db/linq2db/discussions/4351) — [General] Association property inheritance from IEnumerable-implementing base class.

- [I created a .NET 8 template using Linq2Db (also FluentMigrator and FastEndpoints)](https://github.com/linq2db/linq2db/discussions/4425) — [Show and tell] New .NET template featuring linq2db.

- [Can't get properties correctly](https://github.com/linq2db/linq2db/discussions/4529) — [Q&A] Property resolution assistance.

- [.NET Maui](https://github.com/linq2db/linq2db/discussions/4679) — [Q&A] MAUI compatibility.

- [Exotic database reader case](https://github.com/linq2db/linq2db/discussions/4932) — [Q&A] Unusual custom reader scenario.

- [Who ever used pgbouncer with Linq2db?](https://github.com/linq2db/linq2db/discussions/4956) — [General] PgBouncer compatibility.

- [Possible release date for 6 version?](https://github.com/linq2db/linq2db/discussions/5007) — [Q&A] v6 release timeline.

- [`[Association(ThisKey = ..., OtherKey = ...)]` on a method instead of property?](https://github.com/linq2db/linq2db/discussions/5068) — [Q&A] Association attribute on methods.

## Stats

- Open issues: 108
- Closed issues: 494
- Open PRs: 14
- Total PRs: 1198
- Discussions: (counted in issues above)
- Last fetched: 2026-07-06

<details><summary>Coverage</summary>

- Index entries scanned: 122 (108 issues + 494 closed issues)
- Themes extracted: 9 major (mapping/inheritance, SQL optimization, performance, associations, remote execution, identity, transactions, type-system, expressions) + 4 resolved patterns
</details>
