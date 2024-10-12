namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Internal.SqlQuery;

	public interface IToSqlConverter
	{
		ISqlExpression ToSql(object value);
	}
}
