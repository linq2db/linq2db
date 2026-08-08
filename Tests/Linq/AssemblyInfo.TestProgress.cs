using Tests;

// Live progress heartbeat for long test runs. Opt-in via the --test-progress command-line option.
// See .claude/docs/testing.md → "Monitoring a long run".
[assembly: TestProgressReporter]
