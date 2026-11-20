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
| `linq2db/SKILL.md` | Skill entry point: what linq2db is, its architecture, Entity Framework habits that do not apply, one paragraph per area, recent API changes. |
| `linq2db/docs/*.md` | Optional references, one topic each, opened only when a task needs them. One level: no subfolders. |

`Source/LinqToDB/LinqToDB.csproj` packs `linq2db/**/*.md` into the package as:

```text
skills/
  linq2db/
    SKILL.md
    docs/
      ...
```

`docs/hints.md` is named in the message of analyzer L2DB2001 (`StringHintAnalyzer.GuidePath`); keep that path.

Only the `linq2db` package carries skills. Provider metapackages and integration packages do not: they depend on
`linq2db`, and a second copy under the same skill name would collide.

The skill is used from one of two places:

- the restored package, `<global-packages>/linq2db/<version>/skills/linq2db/`;
- a copy in the consuming repository, `.agents/skills/linq2db/` or `.claude/skills/linq2db/`
  (made by `dotnet linq2db skill install` or by hand; the command comes with the `linq2db.cli` tool, installed or run
  without installing as `dnx linq2db.cli skill install`, or by Microsoft's beta `dotnet-package-skills` tool, see below).

Two installers can fill a repository, and a destination (`.agents/skills`, `.claude/skills`) should have one of them:

- `dotnet linq2db skill install` writes `linq2db` into both destinations and records its files in
  `.linq2db-skill.json` inside the skill folder. It reads, and never writes, the
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
  `linq2db.xml` is, and references refer to it by name.
- `SKILL.md` frontmatter: `name` equals the folder name (`linq2db`); `description` says what the
  skill covers and when to use it, on one line, without `: ` or ` #` sequences (they need YAML quoting).
- Write for an agent that has the task in front of it and most of linq2db in its training data. Text
  that a model would write correctly without the skill costs context and adds nothing. The exception is
  the core of `SKILL.md` (what linq2db is and its architecture), which stays as the frame for the rest.
  Outside the core, add a line for an error that agents make without it (an API they do not know, a
  habit from another ORM that is wrong here, a semantic they get wrong), and drop lines that no longer
  prevent one. APIs that postdate model training data go to "Recent changes".
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

## Size budget

The skill is loaded into the agent's context: the `description` always, `SKILL.md` when the skill is
activated, a reference when the agent opens it. Budgets, in estimated tokens (UTF-8 bytes with LF line
endings / 3.5, rounded up; an estimate, not the count of a particular tokenizer):

| Part | Budget |
| --- | --- |
| `description` | 600 characters |
| `SKILL.md` | 3,500 tokens and 500 lines |
| each file in `docs/` | 2,500 tokens |
| the whole skill | 18,000 tokens |

A budget is raised in the PR that needs it, with the reason (for example, a measured agent error that the
extra text prevents).

`Tests/LinqToDB.CLI/SkillBudgetTests.cs` checks the budgets, the frontmatter, the single level of `docs/`,
that `SKILL.md` links every reference, and the absence of a BOM. `Tests/LinqToDB.CLI/SkillInstallCommandTests.cs`
checks, on the files `dotnet linq2db skill install` writes, the frontmatter, that every relative link
inside the skill resolves, and the copy-safe paths.
