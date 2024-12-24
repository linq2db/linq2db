using LinqToDB.Internals.SqlQuery;

namespace LinqToDB.Internals.Linq.Builder
{
	public interface IToSqlConverter
	{
		ISqlExpression ToSql(object value);
	}
}
