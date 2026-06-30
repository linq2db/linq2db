---
area: GLOBAL
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
---

# GLOBAL -- GitHub themes

## Open themes

- **YDB provider completeness** -- 9 open issues tracking gaps in the YDB provider implementation: decimal precision/scale handling (#5591), missing date/time conversions (#5593), null parameter typing (#5594), string nullability inference (#5595), CTE ORDER BY preservation (#5596), unsupported correlated subqueries (#5590), and numeric result handling (#5592). Surfaced during phase-6 completion.

- **Identity column insertion** -- 3 open issues requesting Insert() overloads that preserve identity values: Add InsertOrReplace with custom key (#3597), Insert() with keepIdentity flag (#5021), and MultiInsert not handling inheritance mapping (#2988).

- **Provider feature gaps** -- Open requests for Ingres data provider (#2909), Oracle private temporary tables for large-list filtering (#5601), and EF.Core integration support documentation (#4611).

## Resolved themes

- **Type system and mapping** -- Issues with DateTimeOffset.DateTime sorting (#5435), NodaTime Instant? parameter type inference (#5549), and type-conversion gaps resolved through parameter mapping updates.

- **Build and infrastructure** -- Intermittent test failures (Linux DB2/Informix libdb2.so loading, NuGet pack HintPath resolution) and analyzer rules deferred from 6.3.0 release (#5532) have been addressed.

## Active discussions

- [Make association properties can be subclass of types implemented IEnumberable<T>](https://github.com/linq2db/linq2db/discussions/4351) — [General] We are planning to use linq2db as the ORM framework to replace the current one which developed by ourselves. In the current code all the association properties are inherit from a super class which implements IEnumberable.

- [I created a .NET 8 template using Linq2Db (also FluentMigrator and FastEndpoints)](https://github.com/linq2db/linq2db/discussions/4425) — [Show and tell] Maybe someone is interested. New .NET template with just basics.

- [Can't get properties correctly](https://github.com/linq2db/linq2db/discussions/4529) — [Q&A] using LinqToDB.Mapping; assistance needed with property resolution.

- [.NET Maui](https://github.com/linq2db/linq2db/discussions/4679) — [Q&A] Can I use Linq2db in .NET Maui?

- [Exotic database reader case](https://github.com/linq2db/linq2db/discussions/4932) — [Q&A] Hi to maintainers. Seeking guidance on an unusual scenario.

- [Who ever used pgbouncer with Linq2db?](https://github.com/linq2db/linq2db/discussions/4956) — [General] Are they compatible?

- [Possible release date for 6 version?](https://github.com/linq2db/linq2db/discussions/5007) — [Q&A] Now, before everything else: I completely understand that this is a project developed for free in spare time.

- [`[Association(ThisKey = ..., OtherKey = ...)]` on a method instead of property? Is it allowed, should it work?](https://github.com/linq2db/linq2db/discussions/5068) — [Q&A] Let's say we have Parent and Child entity. The Child has property ParentId that refers to Parent.Id.

## Stats

- Open issues: 38
- Closed issues: 462
- Open PRs: 28
- Total PRs: 1203
- Discussions: 82
- Last fetched: 2026-06-15

<details><summary>Coverage</summary>

- Index entries scanned: 1813 (501 issues + 1203 PRs + 82 discussions + 27 open)
- Themes extracted: 3
</details>
