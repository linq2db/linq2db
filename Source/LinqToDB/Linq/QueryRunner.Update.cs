using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Common;
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class Update<T>
		{
			static readonly ConcurrentDictionary<object,Query<int>> _queryChache = new ConcurrentDictionary<object,Query<int>>();

			static Query<int> CreateQuery(IDataContext dataContext)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery();
				sqlQuery.ChangeQueryType(QueryType.Update);

				sqlQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = sqlQuery, } }
				};

				var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
				var fields = sqlTable.Fields.Values.Where(f => f.IsUpdatable).Except(keys).ToList();

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
					var param = GetParameter(typeof(T), dataContext, field);

					ei.Queries[0].Parameters.Add(param);

					sqlQuery.Update.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
				}

				foreach (var field in keys)
				{
					var param = GetParameter(typeof(T), dataContext, field);

					ei.Queries[0].Parameters.Add(param);

					sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

					if (field.CanBeNull)
						sqlQuery.IsParameterDependent = true;
				}

				SetNonQueryQuery(ei);

				return ei;
			}

			public static int Query(IDataContext dataContext, T obj)
			{
				if (Equals(default, obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				return ei == null ? 0 : (int)ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<int> QueryAsync(IDataContext dataContext, T obj, CancellationToken token)
			{
				if (Equals(default, obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				var result = ei == null ? 0 : await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}
		}
	}
}
