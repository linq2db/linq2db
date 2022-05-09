using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using LinqToDB.Expressions;
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
				bool?        ifExists,
				TableOptions tableOptions)
			{
				var sqlTable  = new SqlTable<T>(dataContext.MappingSchema);
				var dropTable = new SqlDropTableStatement(sqlTable);

				if (tableName    != null) sqlTable.TableName = sqlTable.TableName with { Name     = tableName    };
				if (serverName   != null) sqlTable.TableName = sqlTable.TableName with { Server   = serverName   };
				if (databaseName != null) sqlTable.TableName = sqlTable.TableName with { Database = databaseName };
				if (schemaName   != null) sqlTable.TableName = sqlTable.TableName with { Schema   = schemaName   };
				if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

				sqlTable.Set(ifExists, TableOptions.DropIfExists);

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = dropTable } }
				};

				SetNonQueryQuery(query);

				query.GetElement(dataContext, ExpressionInstances.UntypedNull, null, null);
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
				var sqlTable  = new SqlTable<T>(dataContext.MappingSchema);
				var dropTable = new SqlDropTableStatement(sqlTable);

				if (tableName    != null) sqlTable.TableName = sqlTable.TableName with { Name     = tableName    };
				if (serverName   != null) sqlTable.TableName = sqlTable.TableName with { Server   = serverName   };
				if (databaseName != null) sqlTable.TableName = sqlTable.TableName with { Database = databaseName };
				if (schemaName   != null) sqlTable.TableName = sqlTable.TableName with { Schema   = schemaName   };
				if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

				sqlTable.Set(ifExists, TableOptions.DropIfExists);

				var query = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = dropTable, } }
				};

				SetNonQueryQuery(query);

				await query.GetElementAsync(dataContext, ExpressionInstances.UntypedNull, null, null, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}
	}
}
