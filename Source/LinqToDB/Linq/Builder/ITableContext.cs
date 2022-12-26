using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	interface ITableContext : IBuildContext
	{
		public Type     ObjectType { get; }
		public SqlTable SqlTable { get; }
	}
}
