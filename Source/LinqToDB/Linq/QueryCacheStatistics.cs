using System.Runtime.InteropServices;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Point-in-time snapshot of the LINQ query-plan cache counters, returned by
	/// <see cref="Tools.GetQueryCacheStatistics"/>.
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	public readonly struct QueryCacheStatistics
	{
		internal QueryCacheStatistics(long hits, long misses, long count)
		{
			Hits   = hits;
			Misses = misses;
			Count  = count;
		}

		/// <summary>
		/// Total number of query-plan cache hits since the last clear. Counted only while
		/// <see cref="LinqToDB.Internal.Linq.QueryCache.CollectHitStatistics"/> is enabled; otherwise <c>0</c>.
		/// </summary>
		public long Hits { get; }

		/// <summary>Total number of query-plan cache misses since the last clear. Always counted.</summary>
		public long Misses { get; }

		/// <summary>Number of query plans currently cached.</summary>
		public long Count { get; }

		/// <summary>
		/// Hit rate over the counted accesses (<see cref="Hits"/> / (<see cref="Hits"/> + <see cref="Misses"/>)),
		/// or <c>0</c> when there have been no counted accesses. Meaningful only when hit counting is enabled
		/// (see <see cref="Hits"/>).
		/// </summary>
		public double HitRate
		{
			get
			{
				var total = Hits + Misses;
				return total == 0 ? 0d : (double)Hits / total;
			}
		}
	}
}
