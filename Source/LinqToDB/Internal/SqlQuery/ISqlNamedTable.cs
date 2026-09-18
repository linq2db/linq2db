using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	public interface ISqlNamedTable : ISqlTableSource
	{
		SqlObjectName TableName { get; }
	}
}
