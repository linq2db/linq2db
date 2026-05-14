# Per-provider container + DB init for release testing

Release testing (LINQPad 5/7+, T4, NuGet-T4, CLI) reads connection strings from settings directly — providers do **not** need to be enabled in `UserDataProviders.json`. The only env prep is: container start + database init via the matching script under `Data\Setup Scripts\` (in the linq2db repo, not this curation workspace).

Per-provider entries below capture the exact invocation so `release-test-matrix` can run them without re-asking the user.

## Schema

```markdown
## <provider-name>

- **Container:** `<docker-container-name>` (`docker start <name>`)
- **Setup script(s):** `Data\Setup Scripts\<file>.<ext>` <!-- relative to linq2db repo root -->
- **Invocation:** `<exact command line, including pwsh / bash / sqlcmd / docker exec etc>`
- **Env vars:** <comma-separated VAR=VALUE pairs, or "none">
- **Notes:** <quirks: requires admin password, requires container-internal exec, takes ~N seconds, ...>
- **Last verified:** <iso-date> on release <version>
```

## Allowed actions

The release skill may **run** the recorded invocation directly when it's documented here. For undocumented providers, the skill asks the user for the invocation, records it here, and prompts session-reload. The skill does **not** modify or read `Data\Setup Scripts\<file>` itself — those scripts are owned by the linq2db repo's CI/build setup, not by this curation workspace.

## Entries

<!-- entries below this line are appended on first encounter -->
