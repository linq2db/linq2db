﻿using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using LinqToDB.Expressions;
	using SqlQuery;
	using Mapping;

	static partial class QueryRunner
	{
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

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = createTable, } }
				};

				SetNonQueryQuery(query);

				query.GetElement(dataContext, ExpressionInstances.UntypedNull, null, null);

				ITable<T> table = new Table<T>(dataContext, tableDescriptor);

				if (sqlTable.TableName.Name     != null) table = table.TableName   (sqlTable.TableName.Name);
				if (sqlTable.TableName.Server   != null) table = table.ServerName  (sqlTable.TableName.Server);
				if (sqlTable.TableName.Database != null) table = table.DatabaseName(sqlTable.TableName.Database);
				if (sqlTable.TableName.Schema   != null) table = table.SchemaName  (sqlTable.TableName.Schema);
				if (sqlTable.TableOptions.IsSet()) table = table.TableOptions(sqlTable.TableOptions);

				return table;
			}

			public static async Task<ITable<T>> QueryAsync(
				IDataContext      dataContext,
				EntityDescriptor? tableDescriptor,
				string?           tableName,
				string?           serverName,
				string?           databaseName,
				string?           schemaName,
				string?           statementHeader,
				string?           statementFooter,
				DefaultNullable   defaultNullable,
				TableOptions      tableOptions,
				CancellationToken token)
			{
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

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = createTable, } }
				};

				SetNonQueryQuery(query);

				await query.GetElementAsync(dataContext, ExpressionInstances.UntypedNull, null, null, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				ITable<T> table = new Table<T>(dataContext, tableDescriptor);

				if (sqlTable.TableName.Name     != null) table = table.TableName   (sqlTable.TableName.Name);
				if (sqlTable.TableName.Server   != null) table = table.ServerName  (sqlTable.TableName.Server);
				if (sqlTable.TableName.Database != null) table = table.DatabaseName(sqlTable.TableName.Database);
				if (sqlTable.TableName.Schema   != null) table = table.SchemaName  (sqlTable.TableName.Schema);
				if (sqlTable.TableOptions.IsSet()) table = table.TableOptions(sqlTable.TableOptions);

				return table;
			}
		}
	}
}
