# AI Skill Packs

This directory contains AI-agent documentation packs shipped with linq2db.

These files are not general user documentation and are not part of the library source code. They
are versioned deliverables intended to help coding agents generate correct linq2db code for the
package version they are shipped with.

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

- Keep skill-pack content versioned with the library code it describes.
- Keep `linq2db/SKILL.md` as the canonical entry point for the linq2db skill.
- Regenerate generated files, including `linq2db/docs/api.md`, when XML documentation changes.
- Do not create a second full agent entry point under `Source/LinqToDB`; use a small redirect only
  when compatibility requires one.
- Preserve CRLF line endings and UTF-8 without BOM.
