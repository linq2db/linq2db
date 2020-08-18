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
			public static void Query(
				IDataContext dataContext,
				string?      tableName,
				string?      serverName,
				string?      databaseName,
				string?      schemaName,
				bool         ifExists,
				bool?        isTemporary)
			{
				var sqlTable  = new SqlTable<T>(dataContext.MappingSchema);
				var dropTable = new SqlDropTableStatement(ifExists);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (serverName   != null) sqlTable.Server       = serverName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;
				if (isTemporary  != null) sqlTable.IsTemporary  = isTemporary.Value;

				dropTable.Table  = sqlTable;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = dropTable } }
				};

				SetNonQueryQuery(query);

				query.GetElement(dataContext, Expression.Constant(null), null, null);
			}

			public static async Task QueryAsync(
				IDataContext dataContext,
				string?      tableName,
				string?      serverName,
				string?      databaseName,
				string?      schemaName,
				bool         ifExists,
				bool?        isTemporary,
				CancellationToken token)
			{
				var sqlTable  = new SqlTable<T>(dataContext.MappingSchema);
				var dropTable = new SqlDropTableStatement(ifExists);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (serverName   != null) sqlTable.Server       = serverName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;
				if (isTemporary  != null) sqlTable.IsTemporary  = isTemporary.Value;

				dropTable.Table  = sqlTable;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = dropTable, } }
				};

				SetNonQueryQuery(query);

				await query.GetElementAsync(dataContext, Expression.Constant(null), null, null, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}
	}
}
