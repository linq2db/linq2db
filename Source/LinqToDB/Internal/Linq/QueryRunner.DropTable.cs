using System;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metrics;

namespace LinqToDB.Internal.Linq
{
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

				var query = PrepareQuery(dataContext, tableName, serverName, databaseName, schemaName, ifExists, tableOptions, out var suppress);

				try
				{
					query.GetElement(dataContext, EmptyQueryExpressions, null, null);
				}
				catch (Exception ex) when (suppress && IsTableNotFound(dataContext, ex))
				{
					// swallow "table not found" only — real errors (permission, syntax, etc.) still propagate
				}
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
					var query = PrepareQuery(dataContext, tableName, serverName, databaseName, schemaName, ifExists, tableOptions, out var suppress);

					try
					{
						await query.GetElementAsync(dataContext, EmptyQueryExpressions, null, null, token).ConfigureAwait(false);
					}
					catch (Exception ex) when (suppress && IsTableNotFound(dataContext, ex))
					{
						// swallow "table not found" only — real errors (permission, syntax, etc.) still propagate
					}
				}
			}

			static Query<int> PrepareQuery(
				IDataContext dataContext,
				string?      tableName,
				string?      serverName,
				string?      databaseName,
				string?      schemaName,
				bool?        ifExists,
				TableOptions tableOptions,
				out bool     suppressTableNotFound)
			{
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

				// After the options merge above, the DropIfExists flag is the single source of truth
				// for "should we suppress a missing-table error". Matches the precedence previously
				// encoded in DataExtensions.ShouldSuppressDropException.
				suppressTableNotFound = sqlTable.TableOptions.HasDropIfExists();

				var query = new Query<int>(dataContext)
				{
					Queries = { new QueryInfo { Statement = dropTable } },
				};

				SetNonQueryQuery(query);

				return query;
			}

			static bool IsTableNotFound(IDataContext dataContext, Exception exception)
			{
				var serviceProvider = ((IInfrastructure<IServiceProvider>)dataContext).Instance;
				var dmlService      = serviceProvider.GetService<IDMLService>();
				return dmlService != null && dmlService.IsTableNotFoundException(exception);
			}
		}
	}
}
