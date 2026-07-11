namespace LinqToDB.Internal.Cache
{
	/// <summary>Point-in-time snapshot of a cache's counters. <see cref="Count"/> is <c>-1</c> when the backing
	/// store cannot report a size (e.g. a weak-keyed cache on frameworks without enumerable
	/// <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey,TValue}"/>).</summary>
	readonly record struct CacheStats(
		string    Name,
		CacheKind Kind,
		long      Count,
		long      Hits,
		long      Misses,
		long      Evictions,
		long?     Capacity);
}
