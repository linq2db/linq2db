using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.SqlQuery;

	public interface IToSqlConverter
	{
		ISqlExpression ToSql(Expression expression);
	}
}
