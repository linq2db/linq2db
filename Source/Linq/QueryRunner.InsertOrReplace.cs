using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
		public static class InsertOrReplace<T>
		{
			static readonly ConcurrentDictionary<object,Query<int>> _queryChache = new ConcurrentDictionary<object,Query<int>>();

			static Query<int> CreateQuery(IDataContext dataContext)
			{
				var fieldDic = new Dictionary<SqlField,ParameterAccessor>();
				var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
				var sqlQuery = new SelectQuery { QueryType = QueryType.InsertOrUpdate };

				ParameterAccessor param;

				sqlQuery.Insert.Into  = sqlTable;
				sqlQuery.Update.Table = sqlTable;

				sqlQuery.From.Table(sqlTable);

				var ei = new Query<int>(dataContext, null)
				{
					Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
				};

				var supported = ei.SqlProviderFlags.IsInsertOrUpdateSupported && ei.SqlProviderFlags.CanCombineParameters;

				// Insert.
				//
				foreach (var field in sqlTable.Fields.Select(f => f.Value))
				{
					if (field.IsInsertable)
					{
						if (!supported || !fieldDic.TryGetValue(field, out param))
						{
							param = GetParameter(typeof(T), dataContext, field);
							ei.Queries[0].Parameters.Add(param);

							if (supported)
								fieldDic.Add(field, param);
						}

						sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
					}
					else if (field.IsIdentity)
					{
						throw new LinqException("InsertOrReplace method does not support identity field '{0}.{1}'.", sqlTable.Name, field.Name);
					}
				}

				// Update.
				//
				var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
				var fields = sqlTable.Fields.Values.Where(f => f.IsUpdatable).Except(keys).ToList();

				if (keys.Count == 0)
					throw new LinqException("InsertOrReplace method requires the '{0}' table to have a primary key.", sqlTable.Name);

				var q =
				(
					from k in keys
					join i in sqlQuery.Insert.Items on k equals i.Column
					select new { k, i }
				).ToList();

				var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

				if (missedKey != null)
					throw new LinqException("InsertOrReplace method requires the '{0}.{1}' field to be included in the insert setter.",
						sqlTable.Name,
						missedKey.Name);

				if (fields.Count == 0)
					throw new LinqException("There are no fields to update in the type '{0}'.", sqlTable.Name);

				foreach (var field in fields)
				{
					if (!supported || !fieldDic.TryGetValue(field, out param))
					{
						param = GetParameter(typeof(T), dataContext, field);
						ei.Queries[0].Parameters.Add(param);

						if (supported)
							fieldDic.Add(field, param = GetParameter(typeof(T), dataContext, field));
					}

					sqlQuery.Update.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
				}

				sqlQuery.Update.Keys.AddRange(q.Select(i => i.i));

				// Set the query.
				//
				if (ei.SqlProviderFlags.IsInsertOrUpdateSupported)
					SetNonQueryQuery(ei);
				else
					MakeAlternativeInsertOrUpdate(ei, sqlQuery);

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

#if !NOASYNC

			public static async Task<int> QueryAsync(IDataContext dataContext, T obj, CancellationToken token)
			{
				if (Equals(default(T), obj))
					return 0;

				var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };
				var ei  = _queryChache.GetOrAdd(key, o => CreateQuery(dataContext));

				var result = ei == null ? 0 : await ei.GetElementAsync(dataContext, Expression.Constant(obj), null, token);

				return (int)result;
			}

#endif
		}

		public static void MakeAlternativeInsertOrUpdate(Query query, SelectQuery selectQuery)
		{
			var dic = new Dictionary<ICloneableElement, ICloneableElement>();

			var insertQuery = (SelectQuery)selectQuery.Clone(dic, _ => true);

			insertQuery.QueryType = QueryType.Insert;
			insertQuery.ClearUpdate();
			insertQuery.From.Tables.Clear();

			query.Queries.Add(new QueryInfo
			{
				SelectQuery = insertQuery,
				Parameters  = query.Queries[0].Parameters
					.Select(p => new ParameterAccessor
						(
							p.Expression,
							p.Accessor,
							p.DataTypeAccessor,
							dic.ContainsKey(p.SqlParameter) ? (SqlParameter)dic[p.SqlParameter] : null
						))
					.Where(p => p.SqlParameter != null)
					.ToList(),
			});

			var keys = selectQuery.Update.Keys;

			foreach (var key in keys)
				selectQuery.Where.Expr(key.Column).Equal.Expr(key.Expression);

			selectQuery.ClearInsert();

			if (selectQuery.Update.Items.Count > 0)
			{
				selectQuery.QueryType = QueryType.Update;
				SetNonQueryQuery2(query);
			}
			else
			{
				selectQuery.QueryType = QueryType.Select;
				selectQuery.Select.Columns.Clear();
				selectQuery.Select.Columns.Add(new SelectQuery.Column(selectQuery, new SqlExpression("1")));
				SetQueryQuery2(query);
			}

			query.Queries.Add(new QueryInfo
			{
				SelectQuery = insertQuery,
				Parameters  = query.Queries[0].Parameters.ToList(),
			});
		}
	}
}
