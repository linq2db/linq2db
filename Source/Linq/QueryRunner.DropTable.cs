using System;
using System.Linq.Expressions;

#if !NOASYNC
using System.Threading;
using System.Threading.Tasks;
#endif

namespace LinqToDB.Linq
{
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class DropTable<T>
		{
			public static void Query(IDataContext dataContext, string tableName, string databaseName, string ownerName)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery { QueryType = QueryType.CreateTable };

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (ownerName    != null) sqlTable.Owner        = ownerName;

				sqlQuery.CreateTable.Table  = sqlTable;
				sqlQuery.CreateTable.IsDrop = true;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
				};

				SetNonQueryQuery(query);

				query.GetElement((IDataContextEx)dataContext, Expression.Constant(null), null);
			}

#if !NOASYNC

			public static async Task QueryAsync(IDataContext dataContext, string tableName, string databaseName, string ownerName, CancellationToken token)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery { QueryType = QueryType.CreateTable };

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (ownerName    != null) sqlTable.Owner        = ownerName;

				sqlQuery.CreateTable.Table  = sqlTable;
				sqlQuery.CreateTable.IsDrop = true;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
				};

				SetNonQueryQuery(query);

				await query.GetElementAsync((IDataContextEx)dataContext, Expression.Constant(null), null, token);
			}

#endif
		}
	}
}
