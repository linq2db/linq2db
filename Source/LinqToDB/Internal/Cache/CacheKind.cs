namespace LinqToDB.Internal.Cache
{
	/// <summary>Classification of a linq2db cache, used to target clears and to describe diagnostics.</summary>
	public enum CacheKind
	{
		/// <summary>The LINQ query-plan cache (<see cref="LinqToDB.Internal.Linq.QueryCache"/>).</summary>
		Query,

		/// <summary>A bounded, size-capped work cache (compiled delegates, descriptors, detected versions, ...).</summary>
		BoundedWork,

		/// <summary>A weak-keyed cache whose entries are reclaimed by the GC when the key becomes collectible.</summary>
		WeakKeyed,
	}
}
