using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class Insert<T>
		{
			static readonly ConcurrentDictionary<object,Query<int>> _queryCache = new ConcurrentDictionary<object,Query<int>>();

			static Insert()
			{
				LinqToDB.Linq.Query.CacheCleaners.Add(() => _queryCache.Clear());
			}

			static Query<int> CreateQuery(IDataContext dataContext, T obj, string tableName, string databaseName, string schemaName, Type type)
			{
				var sqlTable = new SqlTable(dataContext.MappingSchema, type);

				if (tableName    != null) sqlTable.PhysicalName = tableName;
				if (databaseName != null) sqlTable.Database     = databaseName;
				if (schemaName   != null) sqlTable.Schema       = schemaName;

				var insertStatement = new SqlInsertStatement { Insert = { Into = sqlTable } };

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = insertStatement } }
				};

				foreach (var field in sqlTable.Fields)
				{
					if (field.Value.IsInsertable)
					{
						var param = GetParameter(type, dataContext, field.Value);
						if (field.Value.SkipValuesOnInsert != null && field.Value.SkipValuesOnInsert.Any() && param.Expression is MemberExpression mExpr)
						{
							if ((mExpr.Member is PropertyInfo info && info.CanRead) || mExpr.Member is FieldInfo)
							{
								var propOrFieldExpr = Expression.PropertyOrField(Expression.Constant(obj), mExpr.Member.Name);
								var func = Expression.Lambda<Func<object>>(Expression.Convert(propOrFieldExpr, typeof(object))).Compile();
								var value = func.Invoke();
								if (field.Value.SkipValuesOnInsert.Contains(value))
								{
									continue;
								}
							}
						}
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

				SetNonQueryQuery(ei);

				return ei;
			}

			public static int Query(IDataContext dataContext, T obj, string tableName, string databaseName, string schemaName)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = GetType<T>(obj, dataContext);
				var key  = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, databaseName, schemaName, type };
				var ei   = Common.Configuration.Linq.DisableQueryCache
					? CreateQuery(dataContext, obj, tableName, databaseName, schemaName, type)
					: _queryCache.GetOrAdd(key,
						o => CreateQuery(dataContext, obj, tableName, databaseName, schemaName, type));

				return (int)ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<int> QueryAsync(
				IDataContext dataContext, T obj, string tableName, string databaseName, string schemaName, CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var type = GetType<T>(obj, dataContext);
				var key  = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID, tableName, databaseName, schemaName, type };
				var ei   = Common.Configuration.Linq.DisableQueryCache
					? CreateQuery(dataContext, obj, tableName, databaseName, schemaName, type)
					: _queryCache.GetOrAdd(key,
						o => CreateQuery(dataContext, obj, tableName, databaseName, schemaName, type));

				var result = await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}
		}
	}
}
