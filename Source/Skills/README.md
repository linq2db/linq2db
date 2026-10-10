# Agent Skills <!-- omit in toc -->

This directory holds the [Agent Skills](https://agentskills.io/) that ship inside the linq2db NuGet
package. A skill is a folder with a `SKILL.md` entry point (YAML frontmatter with `name` and
`description`, then instructions) and supporting files. Coding agents load it to write and review
code that uses linq2db, with guidance that matches the package version.

These files are package content, not maintainer documentation and not library source code.

## Layout

| Path | Role |
| --- | --- |
| `README.md` | This file (maintainers only; not packed). |
| `linq2db/SKILL.md` | Skill entry point: general rules and the guide index. |
| `linq2db/docs/*.md` | Task guides. |
| `linq2db/docs/crud/*.md` | CRUD guides; `crud.md` routes to one file per operation. |

`Source/LinqToDB/LinqToDB.csproj` packs `linq2db/**/*.md` into the package as:

```text
skills/
  linq2db/
    SKILL.md
    docs/
      ...
```

The skill is used from one of two places:

- the restored package, `<global-packages>/linq2db/<version>/skills/linq2db/`;
- a copy in the consuming repository, `.agents/skills/linq2db/` or `.claude/skills/linq2db/`
  (made by `dotnet linq2db skill install` or by hand; the command comes with the `linq2db.cli` tool, installed or run
  without installing as `dnx linq2db.cli skill install`).

## Rules for skill content

- The skill must work from both places above. Links between skill files are relative and stay
  inside `linq2db/`. Do not link to package-root paths such as `lib/<tfm>/linq2db.xml` or to
  repository paths such as `Source/...`; `SKILL.md` explains once where the package's
  `linq2db.xml` is, and guides refer to it by name.
- `SKILL.md` frontmatter: `name` equals the folder name (`linq2db`); `description` says what the
  skill covers and when to use it, at most 1024 characters, on one line, without `: ` or ` #`
  sequences (they need YAML quoting).
- Write for the reader who opens one guide for one task. Each guide starts with a one-line pointer
  to `SKILL.md` and a "You are here if" block. State rules plainly; avoid banners, capitals and
  "must read" instructions - they make agents read every guide for every task.
- Text written into the user's code (comments, names) must be ordinary code text, never notes
  about the agent that wrote it.
- Keep guidance versioned with the code it describes: a change to a public API, provider
  capability, translator or default that a guide documents updates the guide in the same PR.
- Do not put maintainer prompts, roadmaps, test prompts or internal notes under `linq2db/`; every
  file there ships to consumers.
- Files are UTF-8 without BOM; line endings follow `.gitattributes`.

`Tests/Linq/Infrastructure/SkillPackageTests.cs` checks the frontmatter, that every relative link inside
the skill resolves, and that the packed `linq2db` package carries exactly these files with the same
content. The package is the one named by `LINQ2DB_SKILL_PACKAGE`, or the one under `.build/package`
packed from the `linq2db.dll` the test runs against; without either, that check is skipped.

## Not yet covered

Topics without a guide (agents fall back to `linq2db.xml`; `linq2db/docs/coverage.md` lists the
consumer-facing view): grouping and paging gotchas, set operations, projections, inheritance
mapping, transactions and connection lifetime in depth, generated SQL and diagnostics, DDL and
schema provider APIs, async and streaming, compiled queries, metrics, remote contexts, JSON/XML,
date/time behavior, identity and sequences, table functions and views, per-provider guides,
performance, troubleshooting.
