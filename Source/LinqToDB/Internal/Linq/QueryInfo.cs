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

		// Render caches, both built from the same PreparedCommand templates. Prepared is the main query's rendered commands
		// (DML / the single SELECT) as a statement-free PreparedQuery (BakedQuery). EagerCommandCache is the SEPARATE
		// combined eager-loading scenario (detail + main) as a PreparedScenario, which additionally carries that scenario's
		// step facts and group list, so a warm eager execution rebuilds nothing structural. Two slots and not one because a
		// LoadWith query uses BOTH on the same QueryInfo — the eager executor for its data, GetCommand for
		// ToString/GetSqlText.
		internal PreparedQuery?    Prepared          { get; set; }
		internal PreparedScenario? EagerCommandCache { get; set; }
	}
}
