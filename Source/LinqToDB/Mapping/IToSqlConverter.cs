using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Mapping
{
	public interface IToSqlConverter
	{
		ISqlExpression ToSql(object value);
	}
}
