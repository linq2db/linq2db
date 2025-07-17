using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Metrics;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	static partial class QueryRunner
	{
		static IQueryExpressions EmptyQueryExpressions  = new RuntimeExpressionsContainer(ExpressionInstances.UntypedNull);

		public static class CreateTable<T>
			where T : notnull
		{
			public static ITable<T> Query(
				IDataContext      dataContext,
				EntityDescriptor? tableDescriptor,
				string?           tableName,
				string?           serverName,
				string?           databaseName,
				string?           schemaName,
				string?           statementHeader,
				string?           statementFooter,
				DefaultNullable   defaultNullable,
				TableOptions      tableOptions)
			{
				using var m = ActivityService.Start(ActivityID.CreateTable);

				var sqlTable    = tableDescriptor != null ? new SqlTable(tableDescriptor) : SqlTable.Create<T>(dataContext);
				var createTable = new SqlCreateTableStatement(sqlTable);

				if (tableName != null || schemaName != null || databaseName != null || serverName != null)
				{
					sqlTable.TableName = new(
						          tableName    ?? sqlTable.TableName.Name,
						Server  : serverName   ?? sqlTable.TableName.Server,
						Database: databaseName ?? sqlTable.TableName.Database,
						Schema  : schemaName   ?? sqlTable.TableName.Schema);
				}

				if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

				createTable.StatementHeader = statementHeader;
				createTable.StatementFooter = statementFooter;
				createTable.DefaultNullable = defaultNullable;

				var query = new Query<int>(dataContext)
				{
					Queries = { new QueryInfo { Statement = createTable, } }
				};

				SetNonQueryQuery(query);

				query.GetElement(dataContext, EmptyQueryExpressions, null, null);

				ITable<T> table = new Table<T>(dataContext, tableDescriptor);

				if (sqlTable.TableName.Name     != null) table = table.TableName   (sqlTable.TableName.Name);
				if (sqlTable.TableName.Server   != null) table = table.ServerName  (sqlTable.TableName.Server);
				if (sqlTable.TableName.Database != null) table = table.DatabaseName(sqlTable.TableName.Database);
				if (sqlTable.TableName.Schema   != null) table = table.SchemaName  (sqlTable.TableName.Schema);
				if (sqlTable.TableOptions.IsSet()) table = table.TableOptions(sqlTable.TableOptions);

				return table;
			}

			public static async Task<ITable<T>> QueryAsync(
				IDataContext         dataContext,
				TempTableDescriptor? tableDescriptor,
				string?              tableName,
				string?              serverName,
				string?              databaseName,
				string?              schemaName,
				string?              statementHeader,
				string?              statementFooter,
				DefaultNullable      defaultNullable,
				TableOptions         tableOptions,
				CancellationToken    token)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.CreateTableAsync))
				{
					var sqlTable    = tableDescriptor != null ? new SqlTable(tableDescriptor.EntityDescriptor) : SqlTable.Create<T>(dataContext);
					var createTable = new SqlCreateTableStatement(sqlTable);

					if (tableName != null || schemaName != null || databaseName != null || serverName != null)
					{
						sqlTable.TableName = new(
							tableName              ?? sqlTable.TableName.Name,
							Server  : serverName   ?? sqlTable.TableName.Server,
							Database: databaseName ?? sqlTable.TableName.Database,
							Schema  : schemaName   ?? sqlTable.TableName.Schema);
					}

					if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

					createTable.StatementHeader = statementHeader;
					createTable.StatementFooter = statementFooter;
					createTable.DefaultNullable = defaultNullable;

					var query = new Query<int>(dataContext)
					{
						Queries = { new QueryInfo { Statement = createTable, } }
					};

					SetNonQueryQuery(query);

					await query.GetElementAsync(dataContext, EmptyQueryExpressions, null, null, token).ConfigureAwait(false);

					ITable<T> table = new Table<T>(dataContext, tableDescriptor?.EntityDescriptor);

					if (sqlTable.TableName.Name     != null) table = table.TableName   (sqlTable.TableName.Name);
					if (sqlTable.TableName.Server   != null) table = table.ServerName  (sqlTable.TableName.Server);
					if (sqlTable.TableName.Database != null) table = table.DatabaseName(sqlTable.TableName.Database);
					if (sqlTable.TableName.Schema   != null) table = table.SchemaName  (sqlTable.TableName.Schema);
					if (sqlTable.TableOptions.IsSet()) table       = table.TableOptions(sqlTable.TableOptions);

					return table;
				}
			}
		}
	}
}
