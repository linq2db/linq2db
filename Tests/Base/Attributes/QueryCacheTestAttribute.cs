using System;

using LinqToDB.Internal.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tests
{
	/// <summary>
	/// Marks a test that depends on process-global query-cache state - typically asserting exact
	/// <c>GetCacheMissCount()</c> deltas, query-object identity across two executions, or clearing the
	/// cache itself. Such a test needs two things that ordinary tests do not:
	/// <list type="number">
	/// <item>no other test may run concurrently, because <c>Query&lt;T&gt;</c> reads the process-wide
	/// <see cref="QueryCache.Default"/> and a concurrent test's compilation perturbs the counters;</item>
	/// <item>nothing may be evicted from the cache while it runs, because netfx / 32-bit runs cap the
	/// cache at 100 entries (see TestsInitialization) and a suite that has already filled it evicts the
	/// entry the test expects to hit - which reads as one extra compilation.</item>
	/// </list>
	/// </summary>
	/// <remarks>
	/// <para>
	/// Derives from <see cref="ParallelizableAttribute"/> with <see cref="ParallelScope.None"/> so the
	/// scope is published through the same <c>ParallelScope</c> property NUnit's own
	/// <c>[NonParallelizable]</c> uses - which is what the test run's dispatcher reads to route work to
	/// the globally-exclusive lane.
	/// </para>
	/// <para>
	/// The second guarantee is met by lifting the cap for the duration of the test (restoring the
	/// configured value afterwards), not by clearing the cache. Clearing would break the many tests
	/// parameterised by iteration (<c>[Values(1, 2)] int iteration</c>), where the first case warms the
	/// cache and the second asserts it was hit: NUnit fires <see cref="ITestAction"/> per test *case*, so
	/// a clear lands between them. Lifting the cap keeps those entries and still rules out eviction.
	/// Memory stays bounded - only this one exclusive test adds entries while the cap is lifted, and the
	/// first <c>Add</c> after it is restored trims back to the configured size.
	/// </para>
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class QueryCacheTestAttribute : ParallelizableAttribute, ITestAction
	{
		// Captured once, before the first test lifts it, so a mispaired Before/After can never leave the
		// run uncapped - restoring always targets the value TestsInitialization configured.
		static int? _configuredCap;
		static bool _capCaptured;

		public QueryCacheTestAttribute() : base(ParallelScope.None)
		{
		}

		public ActionTargets Targets => ActionTargets.Test;

		public void BeforeTest(ITest test)
		{
			if (!_capCaptured)
			{
				_configuredCap = QueryCache.Default.MaxEntriesOverride;
				_capCaptured   = true;
			}

			QueryCache.Default.MaxEntriesOverride = null;
		}

		public void AfterTest(ITest test)
		{
			QueryCache.Default.MaxEntriesOverride = _configuredCap;
		}
	}
}
