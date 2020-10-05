﻿using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;
	using Mapping;
	using Common.Internal.Cache;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;

	static partial class QueryRunner
	{
		public static class InsertWithIdentity<T>
		{
			static Query<object> CreateQuery(
				IDataContext dataContext, EntityDescriptor descriptor, [DisallowNull] T obj,
				InsertColumnFilter<T>? columnFilter,
				string? tableName, string? serverName, string? databaseName, string? schemaName,
				Type type)
			{
				var sqlTable = new SqlTable(dataContext.MappingSchema, type);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (serverName   != null) sqlTable.Server       = serverName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				var sqlQuery        = new SelectQuery();
				var insertStatement = new SqlInsertStatement(sqlQuery);

				insertStatement.Insert.Into = sqlTable;
				insertStatement.Insert.WithIdentity = true;

				var ei = new Query<object>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = insertStatement, } }
				};

				foreach (var field in sqlTable.Fields.Where(x => columnFilter == null || columnFilter(obj, x.ColumnDescriptor)))
				{
					if (field.IsInsertable && !field.ColumnDescriptor.ShouldSkip(obj!, descriptor, SkipModification.Insert))
					{
						var param = GetParameter(type, dataContext, field);
						ei.Queries[0].Parameters.Add(param);

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

				SetScalarQuery(ei);

				return ei;
			}

			public static object Query(
				IDataContext dataContext, T obj,
				InsertColumnFilter<T>? columnFilter,
				string? tableName, string? serverName, string? databaseName, string? schemaName)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = GetType<T>(obj!, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type);
				var ei = Common.Configuration.Linq.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert) || columnFilter != null
					? CreateQuery(dataContext, entityDescriptor, obj!, columnFilter, tableName, serverName, databaseName, schemaName, type)
					: Cache<T>.QueryCache.GetOrCreate(
						new { Operation = "II", dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schemaName, databaseName, serverName, type },
						o =>
						{
							o.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(dataContext, entityDescriptor, obj!, null, tableName, serverName, databaseName, schemaName, type);
						});

				return ei.GetElement(dataContext, Expression.Constant(obj), null, null)!;
			}

			public static async Task<object> QueryAsync(
				IDataContext dataContext, T obj,
				InsertColumnFilter<T>? columnFilter,
				string? tableName, string? serverName, string? databaseName, string? schemaName,
				CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = GetType<T>(obj!, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type);
				var ei               = Common.Configuration.Linq.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Insert) || columnFilter != null
					? CreateQuery(dataContext, entityDescriptor, obj!, columnFilter, tableName, serverName, databaseName, schemaName, type)
					: Cache<T>.QueryCache.GetOrCreate(
						new { Operation = "II", dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schemaName, databaseName, serverName, type },
						o =>
						{
							o.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(dataContext, entityDescriptor, obj!, null, tableName, serverName, databaseName, schemaName, type);
						});

				return await ((Task<object>)ei.GetElementAsync(dataContext, Expression.Constant(obj), null, null, token)!).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}
	}
}
