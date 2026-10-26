using Tests;

// Live progress heartbeat for long test runs. Opt-in via the --test-progress command-line option.
// Compiled into all EF Core test assemblies (EF3/EF10/EF11) via the shared source directory.
// See .claude/docs/testing.md → "Monitoring a long run".
[assembly: TestProgressReporter]
