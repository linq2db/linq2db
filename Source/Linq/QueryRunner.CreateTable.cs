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
		public static class CreateTable<T>
		{
			public static ITable<T> Query(IDataContext dataContext,
				string tableName, string databaseName, string schemaName,
				string statementHeader, string statementFooter,
				DefaulNullable defaulNullable)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery { QueryType = QueryType.CreateTable };

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Owner        = schemaName;

				sqlQuery.CreateTable.Table           = sqlTable;
				sqlQuery.CreateTable.StatementHeader = statementHeader;
				sqlQuery.CreateTable.StatementFooter = statementFooter;
				sqlQuery.CreateTable.DefaulNullable  = defaulNullable;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
				};

				SetNonQueryQuery(query);

				query.GetElement(dataContext, Expression.Constant(null), null);

				ITable<T> table = new Table<T>(dataContext);

				if (tableName    != null) table = table.TableName   (tableName);
				if (databaseName != null) table = table.DatabaseName(databaseName);
				if (schemaName   != null) table = table.SchemaName  (schemaName);

				return table;
			}

#if !NOASYNC

			public static async Task<ITable<T>> QueryAsync(IDataContext dataContext,
				string tableName, string databaseName, string schemaName, string statementHeader,
				string statementFooter, DefaulNullable defaulNullable,
				CancellationToken token)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery { QueryType = QueryType.CreateTable };

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Owner        = schemaName;

				sqlQuery.CreateTable.Table           = sqlTable;
				sqlQuery.CreateTable.StatementHeader = statementHeader;
				sqlQuery.CreateTable.StatementFooter = statementFooter;
				sqlQuery.CreateTable.DefaulNullable  = defaulNullable;

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
				};

				SetNonQueryQuery(query);

				await query.GetElementAsync(dataContext, Expression.Constant(null), null, token);

				ITable<T> table = new Table<T>(dataContext);

				if (tableName    != null) table = table.TableName   (tableName);
				if (databaseName != null) table = table.DatabaseName(databaseName);
				if (schemaName   != null) table = table.SchemaName  (schemaName);

				return table;
			}

#endif
		}
	}
}
