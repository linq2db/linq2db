---
name: compatibility
description: Use when changing public APIs, option records, provider options, or configuration APIs in this repository. Captures compatibility rules for public API changes.
metadata:
  short-description: linq2db compatibility rules
---

# Compatibility Rules

- When adding a positional parameter to an options record or public configuration record, preserve binary compatibility by adding hidden `[EditorBrowsable(EditorBrowsableState.Never)]` overloads for the previous positional constructor and `Deconstruct` signatures.
- Add `// TODO: remove in v7` to those binary-compatibility members.
