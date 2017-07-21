using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Data;
using LinqToDB.Expressions;

namespace LinqToDB.Linq
{
	using ServiceModel;
	using SqlQuery;

	static class QueryRunner
	{
		// IT : #
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

			public IQueryRunner1 QueryRunner;

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

					if (QueryRunner != null) QueryRunner.MapperExpression = _mapperExpression;

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

					if (QueryRunner != null) QueryRunner.MapperExpression = _expression;

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

					if (QueryRunner != null) QueryRunner.MapperExpression = _expression;

					return (_mapper = _expression.Compile())(queryContext, dataContext, dataReader, expr, ps);
				}
			}
		}

		static int GetParameterIndex(Query query, ISqlExpression parameter)
		{
			for (var i = 0; i < query.Queries[0].Parameters.Count; i++)
			{
				var p = query.Queries[0].Parameters[i].SqlParameter;
				if (p == parameter)
					return i;
			}

			throw new InvalidOperationException();
		}

		static Func<QueryContext,Mapper<T>,IDataContext,Expression,object[],int,IEnumerable<T>> GetQuery<T>(
			Query<T> query,
			Func<QueryContext,Mapper<T>,IDataContext,Expression,object[],int,IEnumerable<T>> runQuery)
		{
			query.FinalizeQuery();

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			var select = query.Queries[0].SelectQuery.Select;

			if (select.SkipValue != null && !query.SqlProviderFlags.GetIsSkipSupportedFlag(query.Queries[0].SelectQuery))
			{
				var q = runQuery;

				var value = select.SkipValue as SqlValue;
				if (value != null)
				{
					var n = (int)((IValueContainer)select.SkipValue).Value;

					if (n > 0)
						runQuery = (ctx, qm, db, expr, ps, qn) => q(ctx, qm, db, expr, ps, qn).Skip(n);
				}
				else if (select.SkipValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.SkipValue);
					runQuery = (ctx, qm, db, expr, ps, qn) => q(ctx, qm, db, expr, ps, qn).Skip((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
				}
			}

			if (select.TakeValue != null && !query.SqlProviderFlags.IsTakeSupported)
			{
				var q = runQuery;

				var value = select.TakeValue as SqlValue;
				if (value != null)
				{
					var n = (int)((IValueContainer)select.TakeValue).Value;

					if (n > 0)
						runQuery = (ctx, qm, db, expr, ps, qn) => q(ctx, qm, db, expr, ps, qn).Take(n);
				}
				else if (select.TakeValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.TakeValue);
					runQuery = (ctx, qm, db, expr, ps, qn) => q(ctx, qm, db, expr, ps, qn).Take((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
				}
			}

			return runQuery;
		}

		static IEnumerable<T> RunQuery<T>(
			QueryContext queryContext,
			Mapper<T>    mapper,
			Query        query,
			IDataContext dataContext,
			Expression   expr,
			object[]     parameters,
			int          queryNumber)
		{
			if (queryContext == null)
				queryContext = new QueryContext(dataContext, expr, parameters);

			//using (var runner = new RemoteDataContextBase.QueryRun(query, dataContext, expr, parameters, queryNumber, true))
			using (var runner = ((IDataContextEx)dataContext).GetQueryRun(query, queryNumber, expr, parameters))
			{
				//mapper.QueryRunner = runner;

				//var count = 0;

				using (var dr = runner.ExecuteReader())
				{
					while (dr.Read())
					{
						yield return mapper.Map(queryContext, dataContext, dr, expr, parameters);
						//count++;
					}
				}

				//runner.RowsCount = count;
			}
		}

		public static void SetRunQuery<T>(Query<T> query,
			Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> expression)
		{
			var mapper = new Mapper<T>(expression);

			var runQuery = GetQuery(query, (ctx, qm, dataContext, expr, parameters, queryNumber) =>
				RunQuery(ctx, qm, query, dataContext, expr, parameters, queryNumber));

			foreach (var q in query.Queries)
				foreach (var sqlParameter in q.Parameters)
					sqlParameter.Expression = null;

			query.GetIEnumerable = (ctx,db,expr,ps) => runQuery(ctx, mapper, db, expr, ps, 0);
		}
	}
}
