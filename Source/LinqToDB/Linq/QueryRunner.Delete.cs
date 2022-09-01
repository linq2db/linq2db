using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	using SqlQuery;
	using Common.Internal.Cache;

	static partial class QueryRunner
	{
		public static class Delete<T>
		{
			static Query<int> CreateQuery(
				IDataContext dataContext,
				string?      tableName,
				string?      serverName,
				string?      databaseName,
				string?      schemaName,
				TableOptions tableOptions,
				Type         type)
			{
				var sqlTable = new SqlTable(dataContext.MappingSchema, type);

				if (tableName != null || schemaName != null || databaseName != null || databaseName != null)
				{
					sqlTable.TableName = new(
						          tableName    ?? sqlTable.TableName.Name,
						Server  : serverName   ?? sqlTable.TableName.Server,
						Database: databaseName ?? sqlTable.TableName.Database,
						Schema  : schemaName   ?? sqlTable.TableName.Schema);
				}

				if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

				var deleteStatement = new SqlDeleteStatement();

				deleteStatement.SelectQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = deleteStatement, } }
				};

				var keys = sqlTable.GetKeys(true).Cast<SqlField>().ToList();

				if (keys.Count == 0)
					ThrowHelper.ThrowLinqException($"Table '{sqlTable.NameForLogging}' does not have primary key.");

				foreach (var field in keys)
				{
					var param = GetParameter(type, dataContext, field);

					ei.Queries[0].AddParameterAccessor(param);

					deleteStatement.SelectQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

					if (field.CanBeNull)
						deleteStatement.IsParameterDependent = true;
				}

				SetNonQueryQuery(ei);

				return ei;
			}

			public static int Query(
				IDataContext dataContext,
				T            obj,
				string?      tableName,
				string?      serverName,
				string?      databaseName,
				string?      schemaName,
				TableOptions tableOptions)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = GetType<T>(obj!, dataContext);
				var ei   = Common.Configuration.Linq.DisableQueryCache
					? CreateQuery(dataContext, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache<T>.QueryCache.GetOrCreate(
						(operation: 'D', dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schemaName, databaseName, serverName, tableOptions, type, dataContext.GetQueryFlags()),
						dataContext,
						static (entry, key, context) =>
						{
							entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(context, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
						});

				return (int)ei.GetElement(dataContext, Expression.Constant(obj), null, null)!;
			}

			public static async Task<int> QueryAsync(
				IDataContext      dataContext,
				T                 obj,
				string?           tableName,
				string?           serverName,
				string?           databaseName,
				string?           schemaName,
				TableOptions      tableOptions,
				CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = GetType<T>(obj!, dataContext);
				var ei   = Common.Configuration.Linq.DisableQueryCache
					? CreateQuery(dataContext, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache<T>.QueryCache.GetOrCreate(
						(operation: 'D', dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schemaName, databaseName, serverName, tableOptions, type, dataContext.GetQueryFlags()),
						dataContext,
						static (entry, key, context) =>
						{
							entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(context, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
						});

				var result = await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, null, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return (int)result!;
			}
		}
	}
}
