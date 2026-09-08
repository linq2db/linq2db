# Publishing to the MCP Registry

The registry identity is `io.github.linq2db/linq2db.cli`. Its metadata lives in
[server.json](server.json); the ownership marker lives in [readme.md](readme.md),
which `LinqToDB.CLI.csproj` includes in the NuGet package.

## Release prerequisites

1. Set both version fields in `server.json` to the CLI package version being released.
2. Publish that version of `linq2db.cli` through the normal NuGet release process.
3. Confirm that the published package README contains
   `<!-- mcp-name: io.github.linq2db/linq2db.cli -->`.

The registry checks the README of the published NuGet package, not the GitHub
working tree. Version 6.4.0 does not contain the marker and cannot be registered
under this identity. The initial manifest targets the upcoming 6.5.0 release.

## Register the released package

Install the official [mcp-publisher](https://github.com/modelcontextprotocol/registry/releases)
for your operating system. From this directory, run:

```text
mcp-publisher login github
mcp-publisher publish server.json
```

Complete the GitHub device authorization using an account that is an **Owner**
of the `linq2db` organization. See the registry's
[authentication requirements](https://github.com/modelcontextprotocol/registry/blob/main/docs/modelcontextprotocol-io/authentication.mdx).

Verify the returned name and version through the
[registry API](https://registry.modelcontextprotocol.io/v0.1/servers?search=io.github.linq2db/linq2db.cli).
Repeat publication with updated version fields for subsequent releases.

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
