﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Common;
	using SqlQuery;
	using Mapping;
	using Common.Internal.Cache;

	static partial class QueryRunner
	{
		public static class Update<T>
		{
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
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (serverName   != null) sqlTable.Server       = serverName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;
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
					if (Configuration.Linq.IgnoreEmptyUpdate)
						return null;

					throw new LinqException(
						keys.Count == sqlTable.Fields.Count ?
							$"There are no fields to update in the type '{sqlTable.Name}'. No PK is defined or all fields are keys." :
							$"There are no fields to update in the type '{sqlTable.Name}'.");
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

				var type = GetType<T>(obj!, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type);
				var ei               = Configuration.Linq.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update) || columnFilter != null
					? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache<T>.QueryCache.GetOrCreate(
						(operation: 'U', dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, columnFilter, tableName, schemaName, databaseName, serverName, tableOptions, type),
						new { dataContext, entityDescriptor, obj},
						static (entry, key, context) =>
						{
							entry.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;
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

				var type = GetType<T>(obj!, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type);
				var ei               = Configuration.Linq.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update) || columnFilter != null
					? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache<T>.QueryCache.GetOrCreate(
						(operation: 'U', dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, columnFilter, tableName, schemaName, databaseName, serverName, tableOptions, type),
						new { dataContext, entityDescriptor, obj },
						static (entry, key, context) =>
						{
							entry.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(context.dataContext, context.entityDescriptor, context.obj, null, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
						});

				var result = ei == null ? 0 : await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, null, token).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				return (int)result!;
			}
		}
	}
}
