using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;
	using Mapping;
	using Common.Internal.Cache;
	using System.Linq;

	static partial class QueryRunner
	{
		public static class Insert<T>
		{
			static Query<int> CreateQuery(
				IDataContext           dataContext,
				EntityDescriptor       descriptor,
				T                      obj,
				InsertColumnFilter<T>? columnFilter,
				string?                tableName,
				string?                serverName,
				string?                databaseName,
				string?                schemaName,
				TableOptions           tableOptions,
				Type                   type)
			{
				var sqlTable = new SqlTable(dataContext.MappingSchema, type);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (serverName   != null) sqlTable.Server       = serverName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;
				if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

				var insertStatement = new SqlInsertStatement { Insert = { Into = sqlTable } };

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = insertStatement } }
				};

				foreach (var field in sqlTable.Fields.Where(x => columnFilter == null || columnFilter(obj, x.ColumnDescriptor)))
				{
					if (field.IsInsertable && !field.ColumnDescriptor.ShouldSkip(obj!, descriptor, SkipModification.Insert))
					{
						var param = GetParameter(type, dataContext, field);
						ei.Queries[0].AddParameterAccessor(param);

						insertStatement.Insert.Items.Add(new SqlSetExpression(field, param.SqlParameter));
					}
					else if (field.IsIdentity)
					{
						var sqlb = dataContext.CreateSqlProvider();
						var expr = sqlb.GetIdentityExpression(sqlTable);

						if (expr != null)
							insertStatement.Insert.Items.Add(new SqlSetExpression(field, expr));
					}
				}

				SetNonQueryQuery(ei);

				return ei;
			}

			public static int Query(
				IDataContext           dataContext,
				T                      obj,
				InsertColumnFilter<T>? columnFilter,
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
				var ei               = Common.Configuration.Linq.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert) || columnFilter != null
					? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache<T>.QueryCache.GetOrCreate(
						(operation: 'I', dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, serverName, databaseName, schemaName, tableOptions, type),
						new { dataContext, entityDescriptor, obj },
						static (entry, key, context) =>
						{
							entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(context.dataContext, context.entityDescriptor, context.obj, null, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
						});

				return (int)ei.GetElement(dataContext, Expression.Constant(obj), null, null)!;
			}

			public static async Task<int> QueryAsync(
				IDataContext           dataContext,
				T                      obj,
				InsertColumnFilter<T>? columnFilter,
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
				var ei               = Common.Configuration.Linq.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert) || columnFilter != null
					? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache<T>.QueryCache.GetOrCreate(
						(operation: 'I', dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, serverName, databaseName, schemaName, tableOptions, type),
						new { dataContext, entityDescriptor, obj },
						static (entry, key, context) =>
						{
							entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(context.dataContext, context.entityDescriptor, context.obj, null, key.tableName, key.serverName, key.databaseName, key.schemaName, key.tableOptions, key.type);
						});

				var result = await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, null, token).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return (int)result!;
			}
		}
	}
}
