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
			static Query<int> CreateQuery(IDataContext dataContext, EntityDescriptor descriptor, T obj, string tableName, string databaseName, string schemaName, Type type)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				var sqlQuery = new SelectQuery();
				var updateStatement = new SqlUpdateStatement(sqlQuery);

				sqlQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = updateStatement, } }
				};

				var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
				var fields = sqlTable.Fields.Values.Where(f => f.IsUpdatable && !f.ColumnDescriptor.ShouldSkip(obj, descriptor, SkipModification.Update)).Except(keys).ToList();

				if (fields.Count == 0)
				{
					if (Configuration.Linq.IgnoreEmptyUpdate)
						return null;

					throw new LinqException(
						keys.Count == sqlTable.Fields.Count ?
							$"There are no fields to update in the type '{sqlTable.Name}'. No PK is defined or all fields are keys." :
							$"There are no fields to update in the type '{sqlTable.Name}'.");
				}

				foreach (var field in fields)
				{
					var param = GetParameter(type, dataContext, field);

					ei.Queries[0].Parameters.Add(param);

					updateStatement.Update.Items.Add(new SqlSetExpression(field, param.SqlParameter));
				}

				foreach (var field in keys)
				{
					var param = GetParameter(type, dataContext, field);

					ei.Queries[0].Parameters.Add(param);

					sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

					if (field.CanBeNull)
						sqlQuery.IsParameterDependent = true;
				}

				SetNonQueryQuery(ei);

				return ei;
			}

			public static int Query(IDataContext dataContext, T obj, string tableName, string databaseName = null, string schemaName = null)
			{
				if (Equals(default(T), obj))
					return 0;

				var type             = GetType<T>(obj, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type);
				var ei               = Configuration.Linq.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update)
					? CreateQuery(dataContext, entityDescriptor, obj, tableName, databaseName, schemaName, type)
					: Cache<T>.QueryCache.GetOrCreate(
						new { Operation = 'U', dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schemaName, databaseName, type },
						o =>
						{
							o.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(dataContext, entityDescriptor, obj, tableName, databaseName, schemaName, type);
						});

				return ei == null ? 0 : (int)ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<int> QueryAsync(IDataContext dataContext, T obj, string tableName = null,
				string databaseName = null, string schemaName = null, CancellationToken token = default)
			{
				if (Equals(default, obj))
					return 0;

				var type             = GetType<T>(obj, dataContext);
				var entityDescriptor = dataContext.MappingSchema.GetEntityDescriptor(type);
				var ei               = Configuration.Linq.DisableQueryCache || entityDescriptor.SkipModificationFlags.HasFlag(SkipModification.Update)
					? CreateQuery(dataContext, entityDescriptor, obj, tableName, databaseName, schemaName, type)
					: Cache<T>.QueryCache.GetOrCreate(
						new { Operation = 'U', dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schemaName, databaseName, type },
						o =>
						{
							o.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
							return CreateQuery(dataContext, entityDescriptor, obj, tableName, databaseName, schemaName, type);
						});

				var result = ei == null ? 0 : await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}
		}
	}
}
