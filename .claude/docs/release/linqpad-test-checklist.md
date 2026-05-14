# LINQPad smoke + targeted-change checklist

What to actually click in LINQPad 5 / 7+ when testing a release. Filled as the skill learns the structure of each release's test surface.

## T4 build prerequisite

Before any T4 / NuGet-T4 / CLI track in `/release-test-matrix`, the solution must be built in Debug targeting net462 to populate the T4 binaries under `.build`. Exact command captured on first run:

> _(empty — first-run user will fill this in)_

## LINQPad 5 (.lpx) smoke

Default checklist (extended on first run):

- [ ] LINQPad starts with no error dialog.
- [ ] linq2db connection wizard appears under Add Connection.
- [ ] Connect to one provider (default: SQL Server) — schema browsable, sample query runs.
- [ ] Run a simple LINQ query → expected results.
- [ ] Run a more complex query that touches the changed surface (release-specific).

## LINQPad 7+ (nugets) smoke

Same as above plus:

- [ ] Nuget installs from the local test feed (recorded path in [`external-repos.md`](./external-repos.md)).
- [ ] Schema browser does not throw for any enabled provider.

## Targeted-change rows

Filled in per release when changes touch the LINQPad driver, scaffold library, or provider surface.

### Release <version>

<!-- entries appended by /release-test-matrix 4.8 on a per-release basis -->
