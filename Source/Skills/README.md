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
| `../LinqToDB.CLI/SKILL.md` | Entry point of the `linq2db-cli` skill (the `linq2db.cli` tool). Not in this directory: it is one file, also embedded in the tool, and packed from there. |

`Source/LinqToDB/LinqToDB.csproj` packs `linq2db/**/*.md` and `../LinqToDB.CLI/SKILL.md` into the package as:

```text
skills/
  linq2db/
    SKILL.md
    docs/
      ...
  linq2db-cli/
    SKILL.md
```

Only the `linq2db` package carries skills. Provider metapackages and integration packages do not: they depend on
`linq2db`, and a second copy under the same skill name would collide.

The skill is used from one of two places:

- the restored package, `<global-packages>/linq2db/<version>/skills/linq2db/`;
- a copy in the consuming repository, `.agents/skills/linq2db/` or `.claude/skills/linq2db/`
  (made by `dotnet linq2db skill install` or by hand; the command comes with the `linq2db.cli` tool, installed or run
  without installing as `dnx linq2db.cli skill install`, or by Microsoft's beta `dotnet-package-skills` tool, see below).

Two installers can fill a repository, and a destination (`.agents/skills`, `.claude/skills`) should have one of them:

- `dotnet linq2db skill install` writes `linq2db` and `linq2db-cli` into both destinations, takes `linq2db-cli` from the
  tool, and records its files in `.linq2db-skill.json` inside each skill folder. It reads, and never writes, the
  `.dotnet-package-skills.json` of a destination: a folder listed there is left unchanged (even with `--force`), and
  `--check` fails when the recorded linq2db version differs from the project's. When it owns a folder in a destination
  that has such a file, it prints how to hand the folder over (`dotnet linq2db skill uninstall`, then `dotnet-package-skills install`).
- `dotnet linq2db skill uninstall` removes what the first installer wrote, in both destinations: only folders with a
  `.linq2db-skill.json`, and inside them only files that still match the record; edited or added files are kept
  (non-zero exit; `--force` also removes edited recorded files, never files it did not install). Folders owned by
  `dotnet-package-skills`, folders without the marker, and everything in a destination whose
  `.dotnet-package-skills.json` cannot be read are left alone.
- `dotnet-package-skills install` (beta) copies the skills of the direct `PackageReference` packages into one
  destination per run (`.agents/skills`; run again with `-d .claude/skills`), records them in
  `.dotnet-package-skills.json`, and skips a non-empty folder it did not install. Because it reads direct references
  only, a project that gets `linq2db` transitively (provider or integration packages) uses
  `dotnet-package-skills install -p linq2db@<version>`.

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

`Tests/LinqToDB.CLI/SkillInstallCommandTests.cs` checks, on the files `dotnet linq2db skill install` writes, the
frontmatter, that every relative link inside the skill resolves, the copy-safe paths and the absence of a BOM.

## Not yet covered

Topics without a guide (agents fall back to `linq2db.xml`; `linq2db/docs/coverage.md` lists the
consumer-facing view): grouping and paging gotchas, set operations, projections, inheritance
mapping, transactions and connection lifetime in depth, generated SQL and diagnostics, DDL and
schema provider APIs, async and streaming, compiled queries, metrics, remote contexts, JSON/XML,
date/time behavior, identity and sequences, table functions and views, per-provider guides,
performance, troubleshooting.
