# Changelog Draft

This file is a temporary staging area for release notes that should be preserved while PRs are in progress.

Official release notes are maintained in the project wiki:
https://github.com/linq2db/linq2db/wiki/Releases-and-Roadmap

Before publishing a release:

1. Move relevant entries from this file to the official wiki release notes.
2. Clear moved entries from this file so it is ready for the next release cycle.

### Release 6.4.0

#### LinqToDB

##### Fixed

  * [ClickHouse] fixed join hint modeling to match ClickHouse strictness/distribution syntax. Deprecated `All*` join hint aliases now emit corrected standalone strictness hints when consumers recompile (`AllOuter` -> `OUTER`, `AllSemi` -> `SEMI`, etc.). Existing already-compiled callers keep their inlined legacy string values; the SQL builder keeps compatibility handling for explicit/legacy `ALL <strictness>` hint strings. (#5555)
