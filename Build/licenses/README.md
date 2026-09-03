# Third-party license notices

Most linq2db packages ship only linq2db's own assemblies and *declare* their dependencies, so NuGet hands
each dependency to the consumer under that dependency's own license and there is nothing for us to
disclose. Three artifact families are different, because the foreign binary is physically **inside** the
artifact we publish:

| Artifact | How the binaries get in |
|---|---|
| `linq2db.cli` (pointer + 7 RID sub-packages) | `PackAsTool` publishes the entire dependency closure into `tools/<tfm>/<rid>/` — around 90 NuGet packages and 158 assemblies per RID, against 22 `PackageReference` lines |
| the 14 T4 packages | each `NuGet/*/linq2db.*.csproj` copies named provider assemblies into `tools/`, including SQL CE and the SAP HANA client out of `Redist/`, and IBM's 223-file `clidriver` tree |
| `linq2db.LINQPad.lpx` | `Source/LinqToDB.LINQPad/Nuget/Pack.cmd` archives the whole `net472` output directory |

All of them declare `PackageLicenseExpression=MIT`. That is correct for linq2db's own code and says
nothing about the rest. This directory is what closes that gap. See
[#5731](https://github.com/linq2db/linq2db/issues/5731).

## Layout

```
components.json    the inventory - one entry per redistributed third-party component
texts/             license and notice bodies, referenced by components.json
generated/         THIRD-PARTY-NOTICES.<artifact>.txt - GENERATED, never hand-edited
```

`generated/` is tracked in git on purpose, for the same reason `CompatibilitySuppressions.xml` and
`PublicAPI.*.txt` are: the exact text that ships is then reviewable in the pull request that changes it.
CI regenerates and byte-compares, so a hand-edit fails the build.

`.gitattributes` pins `generated/**` and `texts/**` to `eol=lf`. Without that, `* text=auto` would check
them out CRLF on Windows and LF elsewhere, and the byte comparison would be red on one platform and green
on the other.

## The script

`Build/Azure/scripts/third-party-notices.ps1` has four actions:

| Action | What it does | Who runs it |
|---|---|---|
| `generate` | renders `generated/` from `components.json` + `texts/` | you, after editing the manifest |
| `check` | regenerates to a temp dir and byte-compares against `generated/` | CI, on every PR |
| `verify` | opens the produced `.nupkg` / `.lpx` / publish output and asserts every shipped binary maps to a component | CI, on every PR |
| `harvest` | reads restore graphs, looks packages up in the NuGet cache, and prints what would change | `/release-deps`, after a dependency bump |

`verify` reads the **produced artifact**, never the csproj. That is the whole point: a csproj cannot tell
you what a transitive dependency dragged in, and a transitive dependency is exactly what a version bump
changes without touching a single line of ours.

## Adding or changing a component

1. Run `-Action harvest` after the dependency change to see what moved. It proposes; it never writes.
2. Edit `components.json` by hand.
3. Run `-Action generate`.
4. Commit `components.json`, any new `texts/` file, **and** the regenerated `generated/` files together.

### `components.json` entry

```jsonc
{
  "id":             "npgsql",                      // stable, lowercase; the sort key
  "displayName":    "Npgsql",
  "packageId":      "Npgsql",                      // empty for components with no NuGet package
  "versions":       { "net8.0": "10.0.3", "net10.0": "10.0.3" },
  "license":        "PostgreSQL",
  "licenseTexts":   [ "npgsql-postgresql.txt" ],   // one or more files under texts/
  "copyright":      "Copyright 2025 © The Npgsql Development Team",
  "projectUrl":     "https://github.com/npgsql/npgsql",
  "redistribution": "permitted",                   // permitted | unresolved
  "files":          [ "Npgsql.dll" ],              // names, or globs, as they appear in the artifact
  "artifacts":      [ "linq2db.cli", "linq2db.PostgreSQL" ]
}
```

Notes on the fields that are easy to get wrong:

- **`versions` is a map, not a string.** The CLI resolves several packages to a different version per
  target framework — `Microsoft.Extensions.Hosting` is 8.0.0 / 9.0.0 / 10.0.0 — because
  `Directory.Packages.props` conditions those entries on `$(TargetFramework)`. Each of the 21 tool payloads
  carries its own `deps.json`, and `verify` checks each one against the entry for *that* framework.
- **`files` matches by exact name first, then by glob**, and a file claimed by two components is an error
  rather than a silent pick. Satellite resources are attributed to their parent:
  `<culture>/Foo.resources.dll` resolves through `Foo.dll`.
- **`redistribution: "unresolved"`** is a deliberate, shipping state for components whose vendor terms have
  not yet been adjudicated. It changes nothing about what is packed; it marks the entry for the audit.
- **A component with no `licenseTexts` is not automatically a defect.** Public-domain software has no text
  to reproduce. It *is* a defect when the terms exist and we simply do not carry them — currently SQL CE
  and the SAP HANA client, both `Redist/` binaries with no license file in this repository.
- **`revisit` is an optional marker for a decision taken on a condition that will change** — an upstream
  package we are waiting on, a question raised with a vendor. `-Action harvest` prints every one of them,
  so they resurface on each release-prep run instead of resting in a file nobody re-reads. Clear the field
  when the condition is met. `Microsoft.SqlServer.Types` carries one today: its 170.1000.7 package ships
  the SQL Server vNext CTP pre-release terms — expired 09/30/2022, with no distribution grant — which is a
  packaging error on a stable release, so the notices reproduce the ordinary SQL Server 2022 terms from
  160.1000.6 of the same package. Reported to Microsoft; the substitution is stated in the component's
  `notes`, which renders into every affected notices file.

## Which packages are asserted to bundle nothing

`verify` also checks the other direction. `linq2db`, `linq2db.Tools`, `linq2db.Scaffold`, `linq2db.Compat`,
`linq2db.Extensions`, `linq2db.FSharp`, the six `linq2db.Remote.*`, the four `linq2db.EntityFrameworkCore`
packages, `linq2db.Analyzers` and the `linq2db.LINQPad` **nupkg** must contain no third-party binary at
all. If one acquires a bundled DLL, that is a change in what we redistribute, and it fails the build rather
than passing unnoticed.

The `linq2db.cli` pointer package has its own expectation: it must carry `tools/any/any/DotnetToolSettings.xml`
and no assemblies whatsoever. Without a per-class rule an artifact the script failed to read would satisfy
every other assertion vacuously.

## First-party assemblies

The allow-list in `components.json` is exact assembly names, deliberately not a `linq2db*` prefix:
`linq2db4iSeries` is a **third-party** package whose id starts with `linq2db` and whose namespace starts
with `LinqToDB.`, and it ships inside the LINQPad 5 plugin. A prefix rule would classify it as ours and
drop it from the notices.
