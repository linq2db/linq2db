using System.Runtime.InteropServices;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Point-in-time entry count for one registered linq2db cache, returned by
	/// <see cref="Tools.GetCacheEntryCounts"/>. Intended for diagnostics — e.g. confirming caches stay bounded.
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	public readonly struct CacheEntryCount
	{
		internal CacheEntryCount(string name, long count)
		{
			Name  = name;
			Count = count;
		}

		/// <summary>Stable identifier of the cache, e.g. <c>"MappingSchema.EntityDescriptors"</c>.</summary>
		public string Name { get; }

		/// <summary>Current number of entries held by the cache.</summary>
		public long Count { get; }
	}
}
