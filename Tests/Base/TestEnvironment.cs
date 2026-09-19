using System;

namespace Tests
{
	/// <summary>
	/// The <c>L2DB_*</c> environment switches the test harness honours, in one place so the set is
	/// discoverable rather than spread across the harness as string literals. Mirrors the
	/// environment-variable table in <c>CONTRIBUTING.md</c>.
	/// </summary>
	/// <remarks>
	/// Read once at type load: these are set before the test host starts and never change during a run,
	/// and some of them are consulted per test.
	/// </remarks>
	public static class TestEnvironment
	{
		/// <summary>
		/// <c>L2DB_TEST_QUERYCACHE</c> - cap on query-cache entries, or <see langword="null"/> when unset or
		/// not a number. What to do when unset is deliberately not decided here: the default depends on the
		/// leg's bitness and target framework.
		/// </summary>
		public static readonly int? QueryCacheMax = ReadInt("L2DB_TEST_QUERYCACHE");

		/// <summary>
		/// <c>L2DB_ASSERT_STATE</c> - compare the shared test tables against their expected contents after
		/// every test, so a test that leaves data behind fails in its own teardown.
		/// </summary>
		public static readonly bool AssertState = IsEnabled("L2DB_ASSERT_STATE");

		/// <summary>
		/// <c>L2DB_PARALLEL_DIAG</c> - trace how the parallel dispatcher routes each test, and how long the
		/// globally-exclusive lane holds the write lock.
		/// </summary>
		public static readonly bool ParallelDiagnostics = IsEnabled("L2DB_PARALLEL_DIAG");

		static bool IsEnabled(string name) => Environment.GetEnvironmentVariable(name) == "1";

		static int? ReadInt(string name)
			=> Environment.GetEnvironmentVariable(name) is { } value && int.TryParse(value, out var parsed) ? parsed : null;
	}
}
