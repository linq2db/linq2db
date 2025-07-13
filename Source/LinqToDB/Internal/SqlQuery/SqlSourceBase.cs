using System.Collections.Generic;
using System.Threading;

namespace LinqToDB.Internal.SqlQuery
{
	public abstract class SqlSourceBase : SqlExpressionBase, ISqlTableSource
	{
		protected SqlSourceBase()
		{
			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		}

		protected SqlSourceBase(int sourceId)
		{
			SourceID = sourceId;
		}

		public int SourceID { get; }

		public abstract SqlTableType          SqlTableType { get; }
		public abstract ISqlTableSource       Source       { get; }
		public abstract SqlField              All          { get; }
		public abstract IList<ISqlExpression> GetKeys(bool allIfEmpty);
	}
}
