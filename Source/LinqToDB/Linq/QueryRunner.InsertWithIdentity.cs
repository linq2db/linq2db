using System;
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
			static readonly ConcurrentDictionary<object,Query<object>> _queryChache = new ConcurrentDictionary<object,Query<object>>();

			static Query<object> CreateQuery(IDataContext dataContext)
			{
				var sqlTable        = new SqlTable<T>(dataContext.MappingSchema);
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
						var param = GetParameter(typeof(T), dataContext, field.Value);

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

			public static object Query(IDataContext dataContext, T obj)
			{
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID};
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				return ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<object> QueryAsync(IDataContext dataContext, T obj, CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				return await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);
			}
		}
	}
}
