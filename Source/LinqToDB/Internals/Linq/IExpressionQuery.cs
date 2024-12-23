using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Linq;

namespace LinqToDB.Internals.Linq
{
	public interface IExpressionQuery
	{
		Expression   Expression  { get; }
		IDataContext DataContext { get; }

		IReadOnlyList<QuerySql> GetSqlQueries(SqlGenerationOptions? options);
	}
}
