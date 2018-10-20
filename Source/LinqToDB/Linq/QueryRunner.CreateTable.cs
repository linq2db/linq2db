using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class CreateTable<T>
		{
			public static ITable<T> Query(IDataContext dataContext,
				string tableName, string serverName, string databaseName, string schemaName,
				string statementHeader, string statementFooter,
				DefaultNullable defaultNullable)
			{
				var sqlTable    = new SqlTable<T>(dataContext.MappingSchema);
				var createTable = new SqlCreateTableStatement();

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (serverName   != null) sqlTable.Server       = serverName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				createTable.Table           = sqlTable;
				createTable.StatementHeader = statementHeader;
				createTable.StatementFooter = statementFooter;
				createTable.DefaultNullable = defaultNullable;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = createTable, } }
				};

				SetNonQueryQuery(query);

				query.GetElement(dataContext, Expression.Constant(null), null);

				ITable<T> table = new Table<T>(dataContext);

				if (sqlTable.PhysicalName != null) table = table.TableName   (sqlTable.PhysicalName);
				if (sqlTable.Server       != null) table = table.ServerName  (sqlTable.Server);
				if (sqlTable.Database     != null) table = table.DatabaseName(sqlTable.Database);
				if (sqlTable.Schema       != null) table = table.SchemaName  (sqlTable.Schema);

				return table;
			}

			public static async Task<ITable<T>> QueryAsync(IDataContext dataContext,
				string tableName, string serverName, string databaseName, string schemaName, string statementHeader,
				string statementFooter, DefaultNullable defaultNullable,
				CancellationToken token)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var createTable = new SqlCreateTableStatement();

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (serverName   != null) sqlTable.Server       = serverName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				createTable.Table           = sqlTable;
				createTable.StatementHeader = statementHeader;
				createTable.StatementFooter = statementFooter;
				createTable.DefaultNullable = defaultNullable;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = createTable, } }
				};

				SetNonQueryQuery(query);

				await query.GetElementAsync(dataContext, Expression.Constant(null), null, token);

				ITable<T> table = new Table<T>(dataContext);

				if (sqlTable.PhysicalName != null) table = table.TableName   (sqlTable.PhysicalName);
				if (sqlTable.Server       != null) table = table.ServerName  (sqlTable.Server);
				if (sqlTable.Database     != null) table = table.DatabaseName(sqlTable.Database);
				if (sqlTable.Schema       != null) table = table.SchemaName  (sqlTable.Schema);

				return table;
			}
		}
	}
}
