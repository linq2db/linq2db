using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using LinqToDB.Expressions;
	using SqlQuery;
	using Tools;

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
				bool?        ifExists,
				TableOptions tableOptions)
			{
				using var m = ActivityService.Start(ActivityID.DropTable);

				var sqlTable  = SqlTable.Create<T>(dataContext);
				var dropTable = new SqlDropTableStatement(sqlTable);

				if (tableName != null || schemaName != null || databaseName != null || serverName != null)
				{
					sqlTable.TableName = new(
						          tableName    ?? sqlTable.TableName.Name,
						Server  : serverName   ?? sqlTable.TableName.Server,
						Database: databaseName ?? sqlTable.TableName.Database,
						Schema  : schemaName   ?? sqlTable.TableName.Schema);
				}

				if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

				sqlTable.Set(ifExists, TableOptions.DropIfExists);

				var query = new Query<int>(dataContext)
				{
					Queries = { new QueryInfo { Statement = dropTable } }
				};

				SetNonQueryQuery(query);

				query.GetElement(dataContext, EmptyQueryExpressions, null, null);
			}

			public static async Task QueryAsync(
				IDataContext      dataContext,
				string?           tableName,
				string?           serverName,
				string?           databaseName,
				string?           schemaName,
				bool?             ifExists,
				TableOptions      tableOptions,
				CancellationToken token)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.DropTableAsync))
				{
					var sqlTable  = SqlTable.Create<T>(dataContext);
					var dropTable = new SqlDropTableStatement(sqlTable);

					if (tableName != null || schemaName != null || databaseName != null || serverName != null)
					{
						sqlTable.TableName = new(
							tableName              ?? sqlTable.TableName.Name,
							Server  : serverName   ?? sqlTable.TableName.Server,
							Database: databaseName ?? sqlTable.TableName.Database,
							Schema  : schemaName   ?? sqlTable.TableName.Schema);
					}

					if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

					sqlTable.Set(ifExists, TableOptions.DropIfExists);

					var query = new Query<int>(dataContext)
					{
						Queries = { new QueryInfo { Statement = dropTable, } }
					};

					SetNonQueryQuery(query);

					await query.GetElementAsync(dataContext, EmptyQueryExpressions, null, null, token).ConfigureAwait(false);
				}
			}
		}
	}
}
