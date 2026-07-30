#if DEBUG
using System.Threading;
#endif

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	public sealed class QueryInfo : IQueryContext
	{
		#if DEBUG

		// For debugging purposes only in multithreading environment
		static          long _uniqueIdCounter;
		public readonly long UniqueId;

		public QueryInfo()
		{
			UniqueId = Interlocked.Increment(ref _uniqueIdCounter);
		}

		#endif

		public SqlStatement    Statement       { get; set; } = null!;
		public object?         Context         { get; set; }
		public bool            IsContinuousRun { get; set; }
		public DataOptions?    DataOptions     { get; set; }

		// Render caches. Prepared is the main query's rendered commands (DML / the single SELECT) as a statement-free
		// PreparedQuery (BakedQuery). EagerCommandCache is the SEPARATE combined eager-loading scenario (detail + main),
		// still a PreparedScenario until the eager path migrates: a LoadWith query uses BOTH on the same QueryInfo — the
		// eager executor for its data and GetCommand for ToString/GetSqlText — so they must not share one slot.
		internal PreparedQuery?    Prepared          { get; set; }
		internal PreparedScenario? EagerCommandCache { get; set; }
	}
}
