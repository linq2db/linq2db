using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	public interface IToSqlConverter
	{
		ISqlExpression ToSql(object value);
	}
}
