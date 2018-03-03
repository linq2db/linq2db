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
				string tableName, string databaseName, string schemaName,
				string statementHeader, string statementFooter,
				DefaultNullable defaultNullable)
			{
				var sqlTable    = new SqlTable<T>(dataContext.MappingSchema);
				var createTable = new SqlCreateTableStatement();

				if (tableName    != null) sqlTable.PhysicalName = tableName;
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

				if (tableName    != null) table = table.TableName   (tableName);
				if (databaseName != null) table = table.DatabaseName(databaseName);
				if (schemaName   != null) table = table.SchemaName  (schemaName);

				return table;
			}

			public static async Task<ITable<T>> QueryAsync(IDataContext dataContext,
				string tableName, string databaseName, string schemaName, string statementHeader,
				string statementFooter, DefaultNullable defaultNullable,
				CancellationToken token)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var createTable = new SqlCreateTableStatement();

				if (tableName    != null) sqlTable.PhysicalName = tableName;
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

				if (tableName    != null) table = table.TableName   (tableName);
				if (databaseName != null) table = table.DatabaseName(databaseName);
				if (schemaName   != null) table = table.SchemaName  (schemaName);

				return table;
			}
		}
	}
}
