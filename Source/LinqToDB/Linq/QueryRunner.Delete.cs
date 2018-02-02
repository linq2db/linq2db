﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class Delete<T>
		{
			static readonly ConcurrentDictionary<object,Query<int>> _queryChache = new ConcurrentDictionary<object,Query<int>>();

			static Query<int> CreateQuery(IDataContext dataContext)
			{
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var deleteStatement = new SqlDeleteStatement();

				deleteStatement.SelectQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { Statement = deleteStatement, } }
				};

				var keys = sqlTable.GetKeys(true).Cast<SqlField>().ToList();

				if (keys.Count == 0)
					throw new LinqException($"Table '{sqlTable.Name}' does not have primary key.");

				foreach (var field in keys)
				{
					var param = GetParameter(typeof(T), dataContext, field);

					ei.Queries[0].Parameters.Add(param);

					deleteStatement.SelectQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

					if (field.CanBeNull)
						deleteStatement.IsParameterDependent = true;
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

				return ei == null ? 0 : (int)ei.GetElement(dataContext, Expression.Constant(obj), null);
			}

			public static async Task<int> QueryAsync(IDataContext dataContext, T obj, CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				var result = ei == null ? 0 : await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}
		}
	}
}
