using System;

using NUnit.Framework;

namespace Tests
{
	/// <summary>
	/// Marks a test that depends on process-global query-cache state - typically asserting exact
	/// <c>GetCacheMissCount()</c> deltas, query-object identity across two executions, or clearing the
	/// cache itself. Such a test cannot run concurrently with any other: <c>Query&lt;T&gt;</c> reaches
	/// <c>QueryCache.Default</c> statically, so a concurrent test's compilation perturbs the counters,
	/// and its cache pressure can evict the entry this test expects to hit - the netfx / 32-bit runs cap
	/// the cache at 100 entries (see TestsInitialization), which is what turns that eviction into a
	/// failure on a merged multi-provider leg.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Derives from <see cref="ParallelizableAttribute"/> with <see cref="ParallelScope.None"/> so the
	/// scope is published through the same <c>ParallelScope</c> property NUnit's own
	/// <c>[NonParallelizable]</c> uses - which is what the test run's dispatcher reads to route work to
	/// the globally-exclusive lane. It carries no behaviour of its own: it exists so the *reason* for
	/// serializing is stated at the call site instead of repeated in a comment on every test.
	/// </para>
	/// <para>
	/// It deliberately does not clear the cache before the test. Many of these tests are parameterised
	/// by iteration (<c>[Values(1, 2)] int iteration</c>): the first case warms the cache and the second
	/// asserts it was hit, so clearing between cases makes them fail by construction. Exclusivity is
	/// sufficient - with nothing else running, nothing else adds entries, so the test's own entries
	/// cannot be evicted out from under it.
	/// </para>
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class QueryCacheTestAttribute : ParallelizableAttribute
	{
		public QueryCacheTestAttribute() : base(ParallelScope.None)
		{
		}
	}
}
