using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class DropTable<T>
		{
			public static void Query(IDataContext dataContext, string tableName, string databaseName, string schemaName)
			{
				var sqlTable  = new SqlTable<T>(dataContext.MappingSchema);
				var dropTable = new SqlDropTableStatement();

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				dropTable.Table  = sqlTable;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = dropTable, } }
				};

				SetNonQueryQuery(query);

				query.GetElement(dataContext, Expression.Constant(null), null);
			}

			public static async Task QueryAsync(IDataContext dataContext, string tableName, string databaseName, string schemaName, CancellationToken token)
			{
				var sqlTable  = new SqlTable<T>(dataContext.MappingSchema);
				var dropTable = new SqlDropTableStatement();

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				dropTable.Table  = sqlTable;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = dropTable, } }
				};

				SetNonQueryQuery(query);

				await query.GetElementAsync(dataContext, Expression.Constant(null), null, token);
			}
		}
	}
}
