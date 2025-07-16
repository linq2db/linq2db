using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

namespace LinqToDB.Internal.Linq
{
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
				var sqlTable = new SqlTable(descriptor);

				if (tableName != null || schemaName != null || databaseName != null || serverName != null)
				{
					sqlTable.TableName = new(
						          tableName    ?? sqlTable.TableName.Name,
						Server  : serverName   ?? sqlTable.TableName.Server,
						Database: databaseName ?? sqlTable.TableName.Database,
						Schema  : schemaName   ?? sqlTable.TableName.Schema);
				}

				if (tableOptions.IsSet()) sqlTable.TableOptions = tableOptions;

				var insertStatement = new SqlInsertStatement { Insert = { Into = sqlTable } };

				var ei = new Query<int>(dataContext)
				{
					Queries = { new QueryInfo { Statement = insertStatement } }
				};

				var accessorIdGenerator = new UniqueIdGenerator<ParameterAccessor>();

				foreach (var field in sqlTable.Fields.Where(x => columnFilter == null || columnFilter(obj, x.ColumnDescriptor)))
				{
					if (field.IsInsertable && !field.ColumnDescriptor.ShouldSkip(obj!, descriptor, SkipModification.Insert))
					{
						var param = GetParameter(accessorIdGenerator, type, dataContext, field);
						ei.AddParameterAccessor(param);

						insertStatement.Insert.Items.Add(new SqlSetExpression(field, param.SqlParameter));
					}
					else if (field.IsIdentity)
					{
						var sqlb = dataContext.CreateSqlBuilder();
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

				using var a = ActivityService.Start(ActivityID.InsertObject);

				var type             = GetType<T>(obj!, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type, dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

				var ei = dataContext.Options.LinqOptions.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert) || columnFilter != null
					? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schemaName, tableOptions, type)
					: Cache<T,int>.QueryCache.GetOrCreate(
						(
							operation: 'I',
							dataContext.ConfigurationID,
							tableName,
							serverName,
							databaseName,
							schemaName,
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

				return (int)ei.GetElement(dataContext, new RuntimeExpressionsContainer(Expression.Constant(obj)), null, null)!;
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

				await using (ActivityService.StartAndConfigureAwait(ActivityID.InsertObjectAsync))
				{
					var type             = GetType<T>(obj!, dataContext);
					var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type, dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated);

					var ei = dataContext.Options.LinqOptions.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert) || columnFilter != null
						? CreateQuery(dataContext, entityDescriptor, obj, columnFilter, tableName, serverName, databaseName, schemaName, tableOptions, type)
						: Cache<T,int>.QueryCache.GetOrCreate(
							(
								operation: 'I',
								dataContext.ConfigurationID,
								tableName,
								serverName,
								databaseName,
								schemaName,
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

					var result = await ei.GetElementAsync(dataContext, new RuntimeExpressionsContainer(Expression.Constant(obj)), null, null, token).ConfigureAwait(false);

					return (int)result!;
				}
			}
		}
	}
}
