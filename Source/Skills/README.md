# Agent Skills <!-- omit in toc -->

This directory holds the [Agent Skills](https://agentskills.io/) that ship inside the linq2db NuGet
package. A skill is a folder with a `SKILL.md` entry point (YAML frontmatter with `name` and
`description`, then instructions) and supporting files. Coding agents load it to write and review
code that uses linq2db, with guidance that matches the package version.

These files are package content, not maintainer documentation and not library source code.

## Layout

The layout follows the [Agent Skills specification](https://agentskills.io/specification) and the package layout
that NuGet's `dotnet-package-skills` tool reads: one folder per skill under `skills/` in the package, each with a
`SKILL.md`.

| Path | Role |
| --- | --- |
| `README.md` | This file (maintainers only; not packed). |
| `linq2db/SKILL.md` | Skill entry point: what linq2db is, its architecture, the traps that most often produce wrong code, a pointer to recent changes, and the table of references. |
| `linq2db/references/<area>.md` | Optional references, one area each, opened only when a task needs them. One level: no subfolders. |

`Source/LinqToDB/LinqToDB.csproj` packs `linq2db/**/*.md` into the package as:

```text
skills/
  linq2db/
    SKILL.md
    references/
      ...
```

`references/hints.md` is named in the message of analyzer L2DB2001 (`StringHintAnalyzer.GuidePath`); keep that path.

The skill folder is the lowercased package id (`linq2db`). NuGet recommends prefixing every skill folder with it
(`skills/linq2db-<skill-name>/` for any further skill), since skills of all packages share one folder in a
repository. Only the `linq2db` package carries the skill. Provider and integration packages do not:
they depend on `linq2db`, and the same skill name from two packages is a collision for `dotnet-package-skills`.

Frontmatter: `name` (equals the folder name), `description` and `license`, all from the specification. There is no
version field: the specification has none at the top level, the package version is already part of the path the skill
is read from, and `dotnet-package-skills` records the version of every skill it copies.

The skill is used from one of two places:

- the restored package, `<global-packages>/linq2db/<version>/skills/linq2db/`;
- a copy in the consuming repository, made by `dotnet-package-skills install` (`.agents/skills/linq2db/` by default,
  `--destination .claude/skills` for Claude Code). The tool reads the project's direct package references; when
  `linq2db` comes through another package, `dotnet-package-skills install --package linq2db@<version>`.

## Rules for skill content

- The skill must work from both places above. Links between skill files are relative and stay
  inside `linq2db/`. Do not link to package-root paths such as `lib/<tfm>/linq2db.xml` or to
  repository paths such as `Source/...`; `SKILL.md` explains once where the package's
  `linq2db.xml` is, and references refer to it by name.
- `SKILL.md` frontmatter: `name` equals the folder name (`linq2db`); `description` says what the
  skill covers and when to use it, on one line, without `: ` or ` #` sequences (they need YAML quoting).
- Write for an agent that has the task in front of it and most of linq2db in its training data. Text
  that a model would write correctly without the skill costs context and adds nothing. The exception is
  the frame of `SKILL.md` (what linq2db is and its architecture), which stays.
  Outside the frame, add a line for an error that agents make without it (an API they do not know, a
  habit from another ORM that is wrong here, a semantic they get wrong), and drop lines that no longer
  prevent one. APIs that postdate model training data go to `references/recent-changes.md`.
- State rules plainly. Do not mark files as required or tell the reader to read everything first:
  agents then load every reference for every task.
- Verify every API name, signature and version against the code before writing it.
- Text written into the user's code (comments, names) must be ordinary code text, never notes
  about the agent that wrote it.
- Keep guidance versioned with the code it describes: a change to a public API or behaviour that
  the skill documents updates the skill in the same PR.
- Do not put maintainer prompts, roadmaps, test prompts or internal notes under `linq2db/`; every
  file there ships to consumers.
- Files are UTF-8 without BOM; line endings follow `.gitattributes`.

## Where new content goes

`SKILL.md` stays small: the frame (what linq2db is, architecture) and at most a handful of traps. Everything
else goes to one reference file per area:

| Area | File |
| --- | --- |
| Mapping: attributes, fluent mapping, `MappingSchema`, DDL-sensitive columns | `references/mapping.md` |
| Queries: joins, grouping, CTEs, window functions, translatable methods, null semantics | `references/queries.md` |
| Writes: insert, update, delete, `Merge`, `Upsert`, `BulkCopy`, optimistic concurrency | `references/writes.md` |
| Eager loading: associations, `LoadWith`, strategies | `references/eager-loading.md` |
| Configuration and lifetime: `DataOptions`, contexts, transactions, temp tables, interceptors | `references/configuration.md` |
| Providers: setup, driver packages, capabilities | `references/providers.md` |
| Raw SQL and extensions: `FromSql`, `[Sql.Expression]`, `[ExpressionMethod]`, member translators | `references/sql-extensions.md` |
| Hints | `references/hints.md` (exists) |
| APIs that postdate model training data | `references/recent-changes.md` (exists) |

A file exists only when it has content. A finding (from blind checks of agents with and without the skill,
or from a review) goes into the file of its area; the first finding for an area creates the file and adds
its row to the References table of `SKILL.md`. A trap moves into `SKILL.md` only when it is among the
most damaging; an entry leaves `recent-changes.md` for its area file once agents know the API without it.

## Size budget

The skill is loaded into the agent's context: the `description` always, `SKILL.md` when the skill is
activated, a reference when the agent opens it. Budgets, in estimated tokens (UTF-8 bytes with LF line
endings / 3.5, rounded up; an estimate, not the count of a particular tokenizer):

| Part | Budget |
| --- | --- |
| `description` | 500 characters |
| `SKILL.md` | 2,000 tokens and 500 lines |
| each file in `references/` | 1,500 tokens |
| the whole skill | 6,000 tokens |

The budgets leave room to grow: `SKILL.md` is about 1,100 tokens, so the frame plus a few more traps and
References rows fit; a reference of 1,500 tokens holds 30 to 40 short entries; 6,000 in all leaves room for
about five more area files next to the current 2,300 tokens.

A budget is raised in the PR that needs it, with the reason (for example, a measured agent error that the
extra text prevents).

`Tests/Tests.Analyzers/SkillContentTests.cs` checks the budgets, the frontmatter, the single level of `references/`,
that `SKILL.md` links every reference, that relative links resolve inside the skill, the absence of a BOM, and that
only the `linq2db` package packs `skills/`.
