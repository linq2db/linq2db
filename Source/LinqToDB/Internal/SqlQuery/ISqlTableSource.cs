using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	// TODO: [sdanyliv] ISqlTableSource why it extends ISqlExpression?
	public interface ISqlTableSource : ISqlExpression
	{
		SqlField               All          { get; }
		int                    SourceID     { get; }
		SqlTableType           SqlTableType { get; }
		IList<ISqlExpression>? GetKeys(bool allIfEmpty);
	}
}
