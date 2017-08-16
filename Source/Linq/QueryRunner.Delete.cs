﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

#if !NOASYNC
using System.Threading;
using System.Threading.Tasks;
#endif

namespace LinqToDB.Linq
{
	using Common;
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class Delete<T>
		{
			static readonly ConcurrentDictionary<object,Query<int>> _queryChache = new ConcurrentDictionary<object,Query<int>>();

			static Query<int> CreateQuery(IDataContext dataContext)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery { QueryType = QueryType.Delete };

				sqlQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
				};

				var keys = sqlTable.GetKeys(true).Cast<SqlField>().ToList();

				if (keys.Count == 0)
					throw new LinqException("Table '{0}' does not have primary key.".Args(sqlTable.Name));

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
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				return ei == null ? 0 : (int)ei.GetElement((IDataContextEx)dataContext, Expression.Constant(obj), null);
			}

#if !NOASYNC

			public static async Task<int> QueryAsync(IDataContext dataContext, T obj, CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				var result = ei == null ? 0 : await ei.GetElementAsync((IDataContextEx)dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}

#endif
		}
	}
}
