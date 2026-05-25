# AI Skill Packs <!-- omit in toc -->

This directory contains AI-agent documentation packs shipped with linq2db.

These files are not general user documentation and are not part of the library source code. They
are versioned deliverables intended to help coding agents generate correct linq2db code for the
package version they are shipped with.

## Directory Map

| Path | Role |
| --- | --- |
| `README.md` | Maintainer-facing overview for shipped skill packs. |
| `linq2db/SKILL.md` | Canonical agent entry point for linq2db package usage. |
| `linq2db/docs/*.md` | Task-focused source-of-truth guides for package users and coding agents. |
| `linq2db/docs/api.md` | Generated API discovery index from `linq2db.xml`; search it, do not read it sequentially. |
| `../Knowledge/` | Versioned generated/assembled knowledge packs for hosted assistants; not shipped in NuGet. |
## Available Skills

- `linq2db/` - AI-friendly documentation for using linq2db.
  Agent entry point: `linq2db/SKILL.md`.

## Package Layout

When packed into NuGet, this directory is included as:

```text
skills/
  linq2db/
    SKILL.md
    docs/
      ...
```

## Maintenance

Knowledge-pack maintenance lives in `../Knowledge/README.md`.

- Keep skill-pack content versioned with the library code it describes.
- Keep `linq2db/SKILL.md` as the canonical entry point for the linq2db skill.
- Regenerate generated files, including `linq2db/docs/api.md`, when XML documentation changes.
- Do not create a second agent entry point under `Source/LinqToDB`; `linq2db/SKILL.md` is canonical.
- Preserve CRLF line endings and UTF-8 without BOM.
