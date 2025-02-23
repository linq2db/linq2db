using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	public interface IToSqlConverter
	{
		ISqlExpression ToSql(object value);
	}
}
