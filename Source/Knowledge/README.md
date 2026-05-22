# Knowledge Packs <!-- omit in toc -->

This directory contains versioned knowledge packs for Custom GPTs and similar hosted assistants.
These files are not shipped in the linq2db NuGet package and are not the source of truth for linq2db API behavior.

## Scope

`Source/Knowledge/linq2db-expert` is a generated or assembled upload format for the linq2db Expert Custom GPT.
It is versioned in the repository so maintainers can review exactly what was uploaded, but it remains
a derived artifact.

## Directory Map

| Path | Role |
| --- | --- |
| `Source/Skills/README.md` | Describes NuGet-shipped skill packs. |
| `Source/Skills/linq2db/SKILL.md` | Canonical entry point for coding agents using the package. |
| `Source/Skills/linq2db/docs/*.md` | Source-of-truth task guides. Some guides are intentionally partial; check `docs/coverage.md`. |
| `Source/Knowledge/README.md` | Maintainer-facing overview for generated/assembled knowledge packs. |
| `Source/Knowledge/linq2db-expert/01-*.md` through `16-*.md` | Versioned Custom GPT upload files generated from the skill pack and XML-doc. |
| `Source/Knowledge/linq2db-expert/*.json` | Generated manifests that describe included source files and upload files. |
| `.agents/Build-LinqToDBExpert.ps1` | Generator for the versioned Expert knowledge pack. |
| `.agents/knowledge-pack-maintenance.md` | Detailed maintainer runbook and validation commands. |
## Source Of Truth

For the linq2db skill, authoritative source material is:

- `Source/Skills/linq2db/SKILL.md`
- `Source/Skills/linq2db/docs/**/*.md`
- generated `Source/Skills/linq2db/docs/api.md`
- generated package XML documentation, normally produced by building `Source/LinqToDB/LinqToDB.csproj`
- `Source/LinqToDB/README.md` for package/NuGet-facing discovery text

Do not add package usage rules only to external Expert/GPT artifacts. If a rule is needed for users
or agents consuming the package, add it to the skill pack or XML documentation first, then regenerate
derived artifacts.

## Layout Rules

- Keep `Source/Skills/linq2db/SKILL.md` as the only canonical agent entry point.
- Keep `Source/LinqToDB/README.md` in place for NuGet/package discovery.
- Keep `Source/LinqToDB/AGENT_GUIDE.md` redirect-only if compatibility requires it.
- Keep task-specific docs under `Source/Skills/linq2db/docs/`.
- Keep knowledge-pack artifacts under `Source/Knowledge/linq2db-expert/`.
- Keep NuGet-shipped skill content under `Source/Skills/linq2db/`.
- Preserve CRLF line endings and UTF-8 without BOM.

## Generated Files

`Source/Skills/linq2db/docs/api.md` is generated from `linq2db.xml` by `Build/GenerateApiDocs.ps1`.
Do not edit it manually except while fixing the generator itself.

When XML documentation changes, rebuild the package project so the generated API extract stays in
sync with the current public API surface.

## Expert Pack

`Source/Knowledge/linq2db-expert` is the repository-versioned copy of the Custom GPT / Expert knowledge pack.
A local external output directory may be used for experiments, but it is not a source of truth.

Regenerate it from repository sources using:

```powershell
.\.agents\Build-LinqToDBExpert.ps1 -NoBuild -OutputRoot Source\Knowledge\linq2db-expert
```

Use `.agents/custom-gpt-instructions.md` as the source for the Custom GPT Instructions field. That
file should define retrieval, source-priority, audit, and response rules only; it must not define
linq2db API behavior independently from the package docs.

## Validation Checklist

After changing skill-pack docs or XML documentation:

1. Build `Source\LinqToDB\LinqToDB.csproj` for the current package TFM.
2. Confirm `Source/Skills/linq2db/docs/api.md` was regenerated when XML docs changed.
3. Regenerate `Source/Knowledge/linq2db-expert` when Custom GPT / Expert knowledge is used for testing or release.
4. Run `git diff --check`.
5. Verify touched text files are UTF-8 without BOM and use CRLF line endings.
6. Search for stale paths such as `Source/LinqToDB/docs`, `Source/LinqToDB/SKILL.md`, `01-agent-guide`, and `02-skill`.
7. Verify package-local docs still route agents through `skills/linq2db/SKILL.md`.
8. Verify generated/derived artifacts do not introduce independent API rules or examples.

## Solution Items

Keep files under `Source/Skills` and `Source/Knowledge` listed in `linq2db.slnx` so maintainers can find and review the skill and knowledge pipeline from IDE solution explorers.
