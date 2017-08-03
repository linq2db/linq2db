using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

#if !NOASYNC
using System.Threading;
using System.Threading.Tasks;
#endif

namespace LinqToDB.Linq
{
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class InsertWithIdentity<T>
		{
			static readonly ConcurrentDictionary<object,Query<object>> _queryChache = new ConcurrentDictionary<object,Query<object>>();

			static Query<object> CreateQuery(IDataContext dataContext)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery { QueryType = QueryType.Insert };

				sqlQuery.Insert.Into         = sqlTable;
				sqlQuery.Insert.WithIdentity = true;

				var ei = new Query<object>(dataContext, null)
				{
					Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
				};

				foreach (var field in sqlTable.Fields)
				{
					if (field.Value.IsInsertable)
					{
						var param = GetParameter(typeof(T), dataContext, field.Value);

						ei.Queries[0].Parameters.Add(param);

						sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, param.SqlParameter));
					}
					else if (field.Value.IsIdentity)
					{
						var sqlb = dataContext.CreateSqlProvider();
						var expr = sqlb.GetIdentityExpression(sqlTable);

						if (expr != null)
							sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, expr));
					}
				}

				SetScalarQuery(ei);

				return ei;
			}

			public static object Query(IDataContext dataContext, T obj)
			{
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID};
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				return ei.GetElement(null, (IDataContextEx)dataContext, Expression.Constant(obj), null);
			}

#if !NOASYNC

			public static Task<object> QueryAsync(IDataContext dataContext, T obj, CancellationToken token, TaskCreationOptions options)
			{
				if (Equals(default(T), obj))
					return Task.FromResult((object)0);

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				return ei.GetElementAsync(null, (IDataContextEx)dataContext, Expression.Constant(obj), null, token, options);
			}

#endif
		}
	}
}
