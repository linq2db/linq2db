using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq
{
	public interface IExpressionQuery
	{
		Expression   Expression  { get; }
		IDataContext DataContext { get; }

		QueryDebugView DebugView { get; }

		IReadOnlyList<QuerySql> GetSqlQueries(SqlGenerationOptions? options);
	}
}
