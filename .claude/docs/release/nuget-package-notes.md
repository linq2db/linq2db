# Per-package update rules and release-notes URLs

Accrued across releases by [`release-deps`](../../skills/release-deps/SKILL.md). One entry per package that has a non-default update rule, a known release-notes URL, or a documented gotcha.

## Schema

Each entry is a heading + bullet list:

```markdown
## <PackageId>

- **Release notes URL:** <url>  <!-- omit if unknown -->
- **Update rule:** <terse rule, e.g. "always pin to .NET 8.0.0 unless flagged vulnerable" or "bump requires regenerating T4 templates in Tests.T4">
- **Co-bump:** <other-package-ids that must move together>  <!-- omit if not applicable -->
- **Last verified:** <iso-date> on release <version>
```

When a rule is added or amended, the modifying skill prompts session-reload.

## Categories

- **Shipping runtime packages:** `Microsoft.Extensions.*`, `System.*` referenced by published linq2db assemblies. Default rule: pin to the initial .NET version (e.g. 8.0.0 / 9.0.0); only bump if flagged vulnerable.
- **Test-only references:** opposite rule — latest stable is always proposed.
- **Analyzers:** prerelease versions allowed (analyzers are dev-time only, not transitively visible to consumers).
- **Database providers:** per-provider rules vary widely (some providers have schema-init compatibility quirks tied to specific versions). Capture each as a row when encountered.
- **Self-references:** `linq2db.t4models` is bumped post-release by `/release-postpublish` step 4 to the just-released version; not by `/release-deps`.

## Entries

<!-- entries below this line are appended by `release-deps` on first encounter -->
