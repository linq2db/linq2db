using Tests;

// Live progress heartbeat for long test runs. Opt-in via the LINQ2DB_TEST_PROGRESS environment variable.
// See .claude/docs/testing.md → "Monitoring a long run".
[assembly: TestProgressReporter]
