using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Infrastructure;
using LinqToDB.Internal.Cache;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Tools;

namespace LinqToDB.Internal.Linq
{
	static partial class QueryRunner
	{
		public static class Update<T>
		{
			public static class Cache
			{
				static Cache()
				{
					Linq.Query.CacheCleaners.Enqueue(ClearCache);
				}

				public static void ClearCache()
				{
					QueryCache.Clear();
				}

				internal static MemoryCache<IStructuralEquatable,Query<int>?> QueryCache { get; } = new(new());
			}

			static Query<int>? CreateQuery(
				IDataContext           dataContext,
				EntityDescriptor       descriptor,
				T                      obj,
				UpdateColumnFilter<T>? columnFilter,
				string?                tableName,
				string?                serverName,
				string?                databaseName,
				string?                schemaName,
				TableOptions           tableOptions,
				Type                   type)
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

				var sqlQuery = new SelectQuery();
				var updateStatement = new SqlUpdateStatement(sqlQuery);
				updateStatement.Update.Table = sqlTable;

				sqlQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext)
				{
					Queries = { new QueryInfo { Statement = updateStatement, } }
				};

				var keys = (sqlTable.GetKeys(true) ?? Enumerable.Empty<ISqlExpression>()).Cast<SqlField>().ToList();
				var fields = sqlTable.Fields
					.Where(f => f.IsUpdatable && !f.ColumnDescriptor.ShouldSkip(obj!, descriptor, SkipModification.Update) && (columnFilter == null || columnFilter(obj, f.ColumnDescriptor)))
					.Except(keys);

				var accessorIdGenerator = new UniqueIdGenerator<ParameterAccessor>();

				var fieldCount = 0;
				foreach (var field in fields)
				{
					var param = GetParameter(accessorIdGenerator, type, dataContext, field);

					ei.AddParameterAccessor(param);

					updateStatement.Update.Items.Add(new SqlSetExpression(field, param.SqlParameter));

					fieldCount++;
				}

				if (fieldCount == 0)
				{
					if (dataContext.Options.LinqOptions.IgnoreEmptyUpdate)
						return null;

					throw new LinqToDBException(
						keys.Count == sqlTable.Fields.Count ?
							$"There are no fields to update in the type '{sqlTable.NameForLogging}'. No PK is defined or all fields are keys." :
							$"There are no fields to update in the type '{sqlTable.NameForLogging}'.");
				}

				foreach (var field in keys)
				{
					var param = GetParameter(accessorIdGenerator, type, dataContext, field);

					ei.AddParameterAccessor(param);

					sqlQuery.Where.SearchCondition.AddEqual(field, param.SqlParameter, CompareNulls.LikeSql);

					if (field.CanBeNull)
						sqlQuery.IsParameterDependent = true;
				}

				SetNonQueryQuery(ei);

				return ei;
			}

			public static int Query(
				IDataContext           dataContext,
				T                      obj,
				UpdateColumnFilter<T>? columnFilter,
				string?                tableName,
				string?                serverName,
				string?                databaseName,
				string?                schemaName,
				TableOptions           tableOptions)
			{
				if (Equals(default(T), obj))
					return 0;

				using var a = ActivityService.Start(ActivityID.UpdateObject);

				var type             = GetType<T>(obj!, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type, dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

				var ei = dataContext.Options.LinqOptions.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update) || columnFilter != null
					? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache.QueryCache.GetOrCreate(
						(
							dataContext.ConfigurationID,
							columnFilter,
							tableName,
							schemaName,
							databaseName,
							serverName,
							tableOptions,
							queryFlags: dataContext.GetQueryFlags(),
							type
						),
						(dataContext, entityDescriptor, obj),
						static (entry, key, context) =>
						{
							entry.SlidingExpiration = context.dataContext.Options.LinqOptions.CacheSlidingExpirationOrDefault;
							return CreateQuery(context.dataContext, context.entityDescriptor, context.obj, null, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
						});

				return ei == null ? 0 : (int)ei.GetElement(dataContext, new RuntimeExpressionsContainer(Expression.Constant(obj)), null, null)!;
			}

			public static async Task<int> QueryAsync(
				IDataContext           dataContext,
				T                      obj,
				UpdateColumnFilter<T>? columnFilter,
				string?                tableName,
				string?                serverName,
				string?                databaseName,
				string?                schemaName,
				TableOptions           tableOptions,
				CancellationToken      token)
			{
				if (Equals(default(T), obj))
					return 0;

				await using (ActivityService.StartAndConfigureAwait(ActivityID.UpdateObjectAsync))
				{
					var type             = GetType<T>(obj!, dataContext);
					var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type, dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

					var ei = dataContext.Options.LinqOptions.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update) || columnFilter != null
						? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schemaName, tableOptions, type)
						: Cache.QueryCache.GetOrCreate(
							(
								dataContext.ConfigurationID,
								columnFilter,
								tableName,
								schemaName,
								databaseName,
								serverName,
								tableOptions,
								type,
								queryFlags: dataContext.GetQueryFlags()
							),
							(dataContext, entityDescriptor, obj),
							static (entry, key, context) =>
							{
								entry.SlidingExpiration = context.dataContext.Options.LinqOptions.CacheSlidingExpirationOrDefault;
								return CreateQuery(context.dataContext, context.entityDescriptor, context.obj, null, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
							});

					var result = ei == null ? 0 : await ei.GetElementAsync(dataContext, new RuntimeExpressionsContainer(Expression.Constant(obj)), null, null, token).ConfigureAwait(false);

					return (int)result!;
				}
			}
		}
	}
}
