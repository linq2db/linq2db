using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Data;
	using LinqToDB.Expressions;
	using SqlQuery;

	static class QueryRunner
	{
		#region Mapper

		class Mapper<T>
		{
			public Mapper(Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> mapperExpression)
			{
				_expression = mapperExpression;
			}

			readonly Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> _expression;
			         Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> _mapperExpression;
			                    Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>  _mapper;

			bool _isFaulted;

			public IQueryRunner QueryRunner;

			public T Map(
				QueryContext queryContext,
				IDataContext dataContext,
				IDataReader  dataReader,
				Expression   expr,
				object[]     ps)
			{
				if (_mapper == null)
				{
					_mapperExpression = (Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>>)_expression.Transform(e =>
					{
						var ex = e as ConvertFromDataReaderExpression;
						return ex != null ? ex.Reduce(dataReader) : e;
					});

					var qr = QueryRunner;
					if (qr != null)
						qr.MapperExpression = _mapperExpression;

					_mapper = _mapperExpression.Compile();
				}

				try
				{
					return _mapper(queryContext, dataContext, dataReader, expr, ps);
				}
				catch (FormatException ex)
				{
					if (_isFaulted)
						throw;

#if !SILVERLIGHT && !NETFX_CORE
					if (DataConnection.TraceSwitch.TraceInfo)
						DataConnection.WriteTraceLine(
							"Mapper has switched to slow mode. Mapping exception: " + ex.Message,
							DataConnection.TraceSwitch.DisplayName);
#endif

					_isFaulted = true;

					var qr = QueryRunner;
					if (qr != null)
						qr.MapperExpression = _mapperExpression;

					return (_mapper = _expression.Compile())(queryContext, dataContext, dataReader, expr, ps);
				}
				catch (InvalidCastException ex)
				{
					if (_isFaulted)
						throw;

#if !SILVERLIGHT && !NETFX_CORE
					if (DataConnection.TraceSwitch.TraceInfo)
						DataConnection.WriteTraceLine(
							"Mapper has switched to slow mode. Mapping exception: " + ex.Message,
							DataConnection.TraceSwitch.DisplayName);
#endif

					_isFaulted = true;

					var qr = QueryRunner;
					if (qr != null)
						qr.MapperExpression = _mapperExpression;

					return (_mapper = _expression.Compile())(queryContext, dataContext, dataReader, expr, ps);
				}
			}
		}

		#endregion

		static void FinalizeQuery(Query query)
		{
			foreach (var sql in query.Queries)
			{
				sql.SelectQuery = query.SqlOptimizer.Finalize(sql.SelectQuery);
				sql.Parameters  = sql.Parameters
					.Select (p => new { p, idx = sql.SelectQuery.Parameters.IndexOf(p.SqlParameter) })
					.OrderBy(p => p.idx)
					.Select (p => p.p)
					.ToList();
			}
		}

		static void ClearParameters(Query query)
		{
			foreach (var q in query.Queries)
				foreach (var sqlParameter in q.Parameters)
					sqlParameter.Expression = null;
		}

		static int GetParameterIndex(Query query, ISqlExpression parameter)
		{
			var parameters = query.Queries[0].Parameters;

			for (var i = 0; i < parameters.Count; i++)
			{
				var p = parameters[i].SqlParameter;

				if (p == parameter)
					return i;
			}

			throw new InvalidOperationException();
		}

		static Tuple<
			Func<Query,QueryContext,IDataContextEx,Mapper<T>,Expression,object[],int,IEnumerable<T>>,
			Func<Expression,object[],int>,
			Func<Expression,object[],int>>
			GetExecuteQuery<T>(
				Query query,
				Func<Query,QueryContext,IDataContextEx,Mapper<T>,Expression,object[],int,IEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			Func<Expression,object[],int> skip = null, take = null;

			var select = query.Queries[0].SelectQuery.Select;

			if (select.SkipValue != null && !query.SqlProviderFlags.GetIsSkipSupportedFlag(query.Queries[0].SelectQuery))
			{
				var q = queryFunc;

				var value = select.SkipValue as SqlValue;
				if (value != null)
				{
					var n = (int)((IValueContainer)select.SkipValue).Value;

					if (n > 0)
					{
						queryFunc = (qq, qc, db, mapper, expr, ps, qn) => q(qq, qc, db, mapper, expr, ps, qn).Skip(n);
						skip  = (expr, ps) => n;
					}
				}
				else if (select.SkipValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.SkipValue);
					queryFunc = (qq, qc, db, mapper, expr, ps, qn) => q(qq, qc, db, mapper, expr, ps, qn).Skip((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
					skip  = (expr,ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, ps);
				}
			}

			if (select.TakeValue != null && !query.SqlProviderFlags.IsTakeSupported)
			{
				var q = queryFunc;

				var value = select.TakeValue as SqlValue;
				if (value != null)
				{
					var n = (int)((IValueContainer)select.TakeValue).Value;

					if (n > 0)
					{
						queryFunc = (qq, qc, db, mapper, expr, ps, qn) => q(qq, qc, db, mapper, expr, ps, qn).Take(n);
						take  = (expr, ps) => n;
					}
				}
				else if (select.TakeValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.TakeValue);
					queryFunc = (qq, qc, db, mapper, expr, ps, qn) => q(qq, qc, db, mapper, expr, ps, qn).Take((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
					take  = (expr,ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, ps);
				}
			}

			return Tuple.Create(queryFunc, skip, take);
		}

		static IEnumerable<T> ExecuteQuery<T>(
			Query          query,
			QueryContext   queryContext,
			IDataContextEx dataContext,
			Mapper<T>      mapper,
			Expression     expression,
			object[]       ps,
			int            queryNumber)
		{
			if (queryContext == null)
				queryContext = new QueryContext(dataContext, expression, ps);

			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps))
			{
				try
				{
					mapper.QueryRunner = runner;

					var count = 0;

					using (var dr = runner.ExecuteReader())
					{
						while (dr.Read())
						{
							yield return mapper.Map(queryContext, dataContext, dr, expression, ps);
							count++;
						}
					}

					runner.RowsCount = count;
				}
				finally
				{
					mapper.QueryRunner = null;
				}
			}
		}

		static async Task ExecuteQueryAsync<T>(
			Query                         query,
			QueryContext                  queryContext,
			IDataContextEx                dataContext,
			Mapper<T>                     mapper,
			Expression                    expression,
			object[]                      ps,
			int                           queryNumber,
			Action<T>                     action,
			Func<Expression,object[],int> skipAction,
			Func<Expression,object[],int> takeAction,
			CancellationToken             cancellationToken,
			TaskCreationOptions           options)
		{
			if (queryContext == null)
				queryContext = new QueryContext(dataContext, expression, ps);

			Func<IDataReader,T> m = dr => mapper.Map(queryContext, dataContext, dr, expression, ps);

			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps))
			{
				runner.SkipAction = skipAction != null ? () => skipAction(expression, ps) : null as Func<int>;
				runner.TakeAction = takeAction != null ? () => takeAction(expression, ps) : null as Func<int>;

				try
				{
					mapper.QueryRunner = runner;

					var count = 0;

					var dr  = await runner.ExecuteReaderAsync(cancellationToken, options);
					await dr.QueryForEachAsync(m, r => { action(r); count++; }, cancellationToken);

					runner.RowsCount = count;
				}
				finally
				{
					mapper.QueryRunner = null;
				}
			}
		}

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> expression)
		{
			var executeQuery = GetExecuteQuery<T>(query, ExecuteQuery);

			ClearParameters(query);

			var mapper   = new Mapper<T>(expression);
			var runQuery = executeQuery.Item1;

			query.GetIEnumerable = (ctx,db,expr,ps) => runQuery(query, ctx, db, mapper, expr, ps, 0);

			var skipAction = executeQuery.Item2;
			var takeAction = executeQuery.Item3;

			query.GetForEachAsync = (expressionQuery,ctx,db,expr,ps,action,token,options) =>
				ExecuteQueryAsync(query, ctx, db, mapper, expr, ps, 0, action, skipAction, takeAction, token, options);
		}
	}
}
