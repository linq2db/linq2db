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
		public AliasesContext? Aliases         { get; set; }
		public DataOptions?    DataOptions     { get; set; }

		// Render caches (unified PreparedScenario type). CommandCache is the main query's rendered commands (DML / the
		// single SELECT), the typed replacement for the untyped Context slot (kept for its shipped IQueryContext contract
		// but no longer carrying the cache). EagerCommandCache is the SEPARATE combined eager-loading scenario (detail +
		// main): a LoadWith query uses BOTH on the same QueryInfo — the eager executor for its data and GetCommand for
		// ToString/GetSqlText — so they must not share one slot.
		internal PreparedScenario? CommandCache      { get; set; }
		internal PreparedScenario? EagerCommandCache { get; set; }
	}
}
