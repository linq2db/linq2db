﻿using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Common;
	using Common.Internal.Cache;
	using Mapping;
	using SqlQuery;

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
				var sqlTable = SqlTable.Create<T>(dataContext);

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

				sqlQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = updateStatement, } }
				};

				var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
				var fields = sqlTable.Fields
					.Where(f => f.IsUpdatable && !f.ColumnDescriptor.ShouldSkip(obj!, descriptor, SkipModification.Update) && (columnFilter == null || columnFilter(obj, f.ColumnDescriptor)))
					.Except(keys);

				var fieldCount = 0;
				foreach (var field in fields)
				{
					var param = GetParameter(type, dataContext, field);

					ei.Queries[0].AddParameterAccessor(param);

					updateStatement.Update.Items.Add(new SqlSetExpression(field, param.SqlParameter));

					fieldCount++;
				}

				if (fieldCount == 0)
				{
					if (dataContext.Options.LinqOptions.IgnoreEmptyUpdate)
						return null;

					throw new LinqException(
						keys.Count == sqlTable.Fields.Count ?
							$"There are no fields to update in the type '{sqlTable.NameForLogging}'. No PK is defined or all fields are keys." :
							$"There are no fields to update in the type '{sqlTable.NameForLogging}'.");
				}

				foreach (var field in keys)
				{
					var param = GetParameter(type, dataContext, field);

					ei.Queries[0].AddParameterAccessor(param);

					sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

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

				return ei == null ? 0 : (int)ei.GetElement(dataContext, Expression.Constant(obj), null, null)!;
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

				var result = ei == null ? 0 : await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, null, token).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				return (int)result!;
			}
		}
	}
}
