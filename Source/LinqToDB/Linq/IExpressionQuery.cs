using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	public interface IExpressionQuery
	{
		Expression   Expression  { get; }
		IDataContext DataContext { get; }

		IReadOnlyList<QuerySql> GetSqlQuery();
	}
}
