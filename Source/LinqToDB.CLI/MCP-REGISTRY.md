# Publishing to the MCP Registry

The registry identity is `io.github.linq2db/linq2db.cli`. Its metadata lives in
[server.json](server.json); the ownership marker lives in [readme.md](readme.md),
which `LinqToDB.CLI.csproj` includes in the NuGet package.

The registry checks the README of the published NuGet package, not the GitHub
working tree. Version 6.4.0 does not contain the marker and cannot be registered
under this identity; 6.5.0 is the first registrable version.

## Publication

[publish-mcp.yml](../../.github/workflows/publish-mcp.yml) publishes on GitHub release
publication. It derives both version fields from the release tag, waits for nuget.org to serve
the package README, and authenticates with `mcp-publisher login github-oidc`, which grants the
`io.github.linq2db/*` namespace from the workflow's `repository_owner` claim - no secret and no
organization Owner required.

Both version fields are committed as `0.0.0` and populated by the workflow from the release tag,
so there is nothing to keep in step with a release. The placeholder is deliberate rather than
cosmetic: publishing the committed file unedited fails at the registry's package-existence check,
whereas a real-looking version would silently re-register a stale one. Read the live version from
the registry API linked below, not from this file.

A `release`-triggered workflow is read from the tree its tag points at, so the workflow must be
merged before the release branch is cut, or that release publishes manually (below).

## Publishing manually

Fallback for a release whose tag predates the workflow, or to re-publish out of band. Install the
official [mcp-publisher](https://github.com/modelcontextprotocol/registry/releases) for your
operating system, set both version fields in `server.json` to the released CLI version, then from
this directory:

```text
mcp-publisher validate server.json
mcp-publisher login github
mcp-publisher publish server.json
```

Interactive `login github` grants an organization namespace only to an **Owner** of the
`linq2db` organization; ordinary membership is not sufficient. See the registry's
[authentication requirements](https://github.com/modelcontextprotocol/registry/blob/main/docs/modelcontextprotocol-io/authentication.mdx).

Verify the returned name and version through the
[registry API](https://registry.modelcontextprotocol.io/v0.1/servers?search=io.github.linq2db/linq2db.cli).

## Client configuration

Clients use the NuGet `dnx` runtime (.NET 10 SDK or later), the fixed `mcp`
subcommand, and a user-supplied absolute `--config` path. Equivalent invocation:

```text
dnx linq2db.cli@6.5.0 --yes -- mcp --config /absolute/path/to/query.json
```

Create the local configuration using `config-init`, as described in the CLI
README. It can contain multiple profiles, which agents discover through
`linq2db_info`. The optional `--profile` selects the default profile.
The optional `--enable-execute-tool` defaults to `false`; setting it to `true`
registers the write-capable tool, but execution also requires `enableExecute: true`
in the selected configuration profile.
Do not put connection strings or credentials in `server.json`.
