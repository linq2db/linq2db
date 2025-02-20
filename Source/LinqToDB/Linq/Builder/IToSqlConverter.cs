using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	public interface IToSqlConverter
	{
		ISqlExpression ToSql(object value);
	}
}
