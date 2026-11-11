# Documentation site

Sources of <https://linq2db.github.io>: articles, table of contents and the docfx configuration under
`source/`, API reference generated from the projects under `Source/`. The site's home page and the
Contributing article include the repository's `README.md` and `CONTRIBUTING.md`.

## Building locally

```powershell
./docs/build.ps1                    # output: .build/docs/_site
./docs/build.ps1 -WarningsAsErrors  # what CI runs
```

Needs only the .NET SDK from `global.json`; tested on Windows only. The script cleans the previous output, downloads the pinned
docfx on first use (cached under `.build/docfx/<sha256>/`), builds `LinqToDB.FSharp` (its API is read from
the built assembly) and runs docfx. Open `.build/docs/_site/index.html`, or serve it with
`dotnet .build/docfx/<sha256>/docfx.dll serve .build/docs/_site`.

## Publishing

`.github/workflows/docs.yml` builds the site on every pull request that touches `docs/`, `README.md`,
`CONTRIBUTING.md`, the build settings it depends on (`global.json`, `Directory.Build.props`,
`Directory.Packages.props`, `Source/**/Directory.Build.props`, `Source/Analyzers.Common.props`) or the
workflow itself, and publishes it to `linq2db/linq2db.github.io` when a (non-prerelease) GitHub release
is published. To re-publish a release:

```
gh workflow run docs.yml --repo linq2db/linq2db --ref v<version> -f deploy=true
```

Deployment uses the `DOCS_DEPLOY_KEY` secret of the `docs` environment, which only accepts `v*` tags.

## Why a custom docfx build

linq2db documents many assemblies in one docfx project and many of them declare the same namespaces,
most of all `LinqToDB`. A docfx UID is derived from the fully qualified name of an API alone, so those
APIs collide: pages overwrite each other, `DuplicateUids` is reported and links resolve to whichever
assembly happened to win. See <https://github.com/dotnet/docfx/issues/8966>.

`source/docfx.json` therefore uses two options that are not in released docfx yet
(<https://github.com/dotnet/docfx/pull/11090>):

- **`assemblyUids`** — the assemblies whose APIs carry the assembly they are declared in as a component of
  their UID, as in `linq2db.Tools::LinqToDB.Tools.Activity.ActivityStatistics`. It sits at the top level of
  `docfx.json` next to `metadata`, because every metadata entry has to agree: an entry mints UIDs for the
  APIs it references as well as the ones it documents.
- **`assemblyUidOverride`** — the component to use for the assemblies one entry documents. The four
  Entity Framework Core entries all build an assembly named `linq2db.EntityFrameworkCore` (see
  `LinqToDB.EntityFrameworkCore.props`), so they cannot be told apart by assembly name and this is the only
  thing that separates them.

The assembly is a component of the identity, not a namespace segment: `LinqToDB` still reads as `LinqToDB`
in titles, breadcrumbs and links, and only the UID and the file name carry the assembly (`::` becomes `--`
in file names). Nothing displayed names the assembly, because `assemblyLabel` is left at its default of
`none`.

## How the docfx build is produced

`docfx-version.json` pins a `docfx.zip` released from <https://github.com/MaceWindu/docfx>, branch
`custom/linq2db-assembly-uids`: the commits of dotnet/docfx#11090, merged with upstream `main`, plus the
`Microsoft.CodeAnalysis*` pins linq2db needs and the `linq2db-build.yml` workflow. Pushing a
`linq2db-<yyyymmdd>-<sha7>` tag there runs that workflow, which builds the templates and
`dotnet publish src/docfx -c Release -f net10.0`, and attaches `docfx.zip` and `docfx.zip.sha256` to a
release of that tag. Copy the tag, asset URL and the hash from `docfx.zip.sha256` into
`docfx-version.json`.

docfx's Roslyn must be at least the `Microsoft.CodeAnalysis.CSharp` version `Source/CodeGenerators` is
built against (the netstandard2.0 pin in `Directory.Packages.props`). With an older one the CodeGenerators
source generator does not load, the build fails with `CS8795` on `ExpressionBuilder.FindBuilderImpl`, and
docfx reports `No .NET API detected`. The release preparation (`/release-deps`) re-syncs the fork with
upstream and checks this.

## When can this go away

Once `assemblyUids` and `assemblyUidOverride` ship in a released docfx, replace the download in `build.ps1`
with the `docfx` tool and drop `docfx-version.json`. `source/docfx.json` does not need to change.
