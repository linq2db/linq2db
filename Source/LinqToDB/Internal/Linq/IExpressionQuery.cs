using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Linq
{
	public interface IExpressionQuery
	{
		Expression   Expression  { get; }
		IDataContext DataContext { get; }

		IReadOnlyList<QuerySql> GetSqlQueries(SqlGenerationOptions? options);
	}
}
