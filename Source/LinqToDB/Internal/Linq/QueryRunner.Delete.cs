using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Infrastructure;
using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Tools;

namespace LinqToDB.Internal.Linq
{
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
				var sqlTable = new SqlTable(dataContext.MappingSchema.GetEntityDescriptor(type, dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated));

				if (tableName != null || schemaName != null || databaseName != null || serverName != null)
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

				var ei = new Query<int>(dataContext)
				{
					Queries = { new QueryInfo { Statement = deleteStatement, } }
				};

				var keys = sqlTable.GetKeys(true)!.Cast<SqlField>().ToList();

				if (keys.Count == 0)
					throw new LinqToDBException($"Table '{sqlTable.NameForLogging}' does not have primary key.");

				var accessorIdGenerator = new UniqueIdGenerator<ParameterAccessor>();

				foreach (var field in keys)
				{
					var param = GetParameter(accessorIdGenerator, type, dataContext, field);

					ei.AddParameterAccessor(param);

					deleteStatement.SelectQuery.Where.SearchCondition.AddEqual(field, param.SqlParameter, CompareNulls.LikeSql);

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

				using var a = ActivityService.Start(ActivityID.DeleteObject);

				var type        = GetType<T>(obj!, dataContext);
				var dataOptions = dataContext.Options;

				var ei = dataOptions.LinqOptions.DisableQueryCache
					? CreateQuery(dataContext, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache<T,int>.QueryCache.GetOrCreate(
						(
							operation: 'D',
							dataContext.ConfigurationID,
							tableName,
							schemaName,
							databaseName,
							serverName,
							tableOptions,
							type,
							queryFlags: dataContext.GetQueryFlags()
						),
						dataContext,
						static (entry, key, context) =>
						{
							entry.SlidingExpiration = context.Options.LinqOptions.CacheSlidingExpirationOrDefault;
							return CreateQuery(context, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
						});

				return (int)ei.GetElement(dataContext, new RuntimeExpressionsContainer(Expression.Constant(obj)), null, null)!;
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

				await using (ActivityService.StartAndConfigureAwait(ActivityID.DeleteObjectAsync))
				{
					var type = GetType<T>(obj!, dataContext);
					var ei = dataContext.Options.LinqOptions.DisableQueryCache
						? CreateQuery(dataContext, tableName, serverName, databaseName, schemaName, tableOptions, type)
						: Cache<T,int>.QueryCache.GetOrCreate(
							(
								operation: 'D',
								dataContext.ConfigurationID,
								tableName,
								schemaName,
								databaseName,
								serverName,
								tableOptions,
								type,
								queryFlags: dataContext.GetQueryFlags()
							),
							dataContext,
							static (entry, key, context) =>
							{
								entry.SlidingExpiration = context.Options.LinqOptions.CacheSlidingExpirationOrDefault;
								return CreateQuery(context, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
							});

					var result = await ei.GetElementAsync(dataContext, new RuntimeExpressionsContainer(Expression.Constant(obj)), null, null, token).ConfigureAwait(false);

					return (int)result!;
				}
			}
		}
	}
}
