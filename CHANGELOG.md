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

  * [Oracle] fixed long string parameter binding for `NClob`/XML scenarios by moving `DbParameter` creation into the provider layer and allowing Oracle to infer `NClob` for undefined string parameters that exceed the configured maximum string parameter length. This fixes Oracle `XmlTable` and raw SQL long string parameter regressions. (#5600)
