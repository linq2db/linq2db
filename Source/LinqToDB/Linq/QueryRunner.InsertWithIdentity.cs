﻿using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class InsertWithIdentity<T>
		{
			static readonly ConcurrentDictionary<object,Query<object>> _queryCache = new ConcurrentDictionary<object,Query<object>>();

			static Query<object> CreateQuery(IDataContext dataContext, string tableName, string databaseName, string schemaName, Type type)
			{
				var sqlTable = new SqlTable(dataContext.MappingSchema, type);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				var sqlQuery        = new SelectQuery();
				var insertStatement = new SqlInsertStatement(sqlQuery);

				insertStatement.Insert.Into         = sqlTable;
				insertStatement.Insert.WithIdentity = true;

				var ei = new Query<object>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = insertStatement, } }
				};

				foreach (var field in sqlTable.Fields)
				{
					if (field.Value.IsInsertable)
					{
						var param = GetParameter(type, dataContext, field.Value);

						ei.Queries[0].Parameters.Add(param);

						insertStatement.Insert.Items.Add(new SqlSetExpression(field.Value, param.SqlParameter));
					}
					else if (field.Value.IsIdentity)
					{
						var sqlb = dataContext.CreateSqlProvider();
						var expr = sqlb.GetIdentityExpression(sqlTable);

						if (expr != null)
							insertStatement.Insert.Items.Add(new SqlSetExpression(field.Value, expr));
					}
				}

				SetScalarQuery(ei);

				return ei;
			}

			public static object Query(IDataContext dataContext, T obj, string tableName, string databaseName = null, string schemaName = null)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = obj.GetType();
				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schemaName, databaseName, type };
				var ei = _queryCache.GetOrAdd(key, o => CreateQuery(dataContext, tableName, databaseName, schemaName, type));

				return ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<object> QueryAsync(IDataContext dataContext, T obj, string tableName = null,
				string databaseName = null, string schemaName = null, CancellationToken token = default)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = obj.GetType();
				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, schemaName, databaseName, type };
				var ei  = _queryCache.GetOrAdd(key, o => CreateQuery(dataContext, tableName, databaseName, schemaName, type));

				return await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);
			}
		}
	}
}
