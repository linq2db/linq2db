﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using LinqToDB.Mapping;
#if !SL4
using System.Threading.Tasks;
#endif

namespace LinqToDB.Linq
{
	using Builder;
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;

	static partial class QueryRunner
	{
		#region Mapper

		class Mapper<T>
		{
			public Mapper(Expression<Func<IQueryRunner,IDataReader,T>> mapperExpression)
			{
				_expression = mapperExpression;
			}

			readonly Expression<Func<IQueryRunner,IDataReader,T>> _expression;
			         Expression<Func<IQueryRunner,IDataReader,T>> _mapperExpression;
			                    Func<IQueryRunner,IDataReader,T>  _mapper;

			bool _isFaulted;

			public IQueryRunner QueryRunner;

			public T Map(IQueryRunner queryRunner, IDataReader dataReader)
			{
				if (_mapper == null)
				{
					_mapperExpression = (Expression<Func<IQueryRunner,IDataReader,T>>)_expression.Transform(e =>
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
					return _mapper(queryRunner, dataReader);
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

					return (_mapper = _expression.Compile())(queryRunner, dataReader);
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

					return (_mapper = _expression.Compile())(queryRunner, dataReader);
				}
			}
		}

		#endregion

		#region Helpers

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
#if !DEBUG
			foreach (var q in query.Queries)
				foreach (var sqlParameter in q.Parameters)
					sqlParameter.Expression = null;
#endif
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

		internal static void SetParameters(
			Query query, IDataContext dataContext, Expression expression, object[] parameters, int queryNumber)
		{
			var queryContext = query.Queries[queryNumber];

			foreach (var p in queryContext.Parameters)
			{
				var value = p.Accessor(expression, parameters);

				var vs = value as IEnumerable;

				if (vs != null)
				{
					var type = vs.GetType();
					var etype = type.GetItemType();

					if (etype == null || etype == typeof(object) || etype.IsEnumEx() ||
						type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
						etype.GetGenericArgumentsEx()[0].IsEnumEx())
					{
						var values = new List<object>();

						foreach (var v in vs)
						{
							value = v;

							if (v != null)
							{
								var valueType = v.GetType();

								if (valueType.ToNullableUnderlying().IsEnumEx())
									value = query.GetConvertedEnum(valueType, value);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				p.SqlParameter.Value = value;

//				if (value != null && dataContext.InlineParameters && p.SqlParameter.IsQueryParameter)
//				{
//					var type = value.GetType();
//
//					if (type != typeof(byte[]) && dataContext.MappingSchema.IsScalarType(type))
//						p.SqlParameter.IsQueryParameter = false;
//				}

				var dataType = p.DataTypeAccessor(expression, parameters);

				if (dataType != DataType.Undefined)
					p.SqlParameter.DataType = dataType;
			}
		}

		static ParameterAccessor GetParameter(Type type, IDataContext dataContext, SqlField field)
		{
			var exprParam = Expression.Parameter(typeof(Expression), "expr");

			Expression getter = Expression.Convert(
				Expression.Property(
					Expression.Convert(exprParam, typeof(ConstantExpression)),
					ReflectionHelper.Constant.Value),
				type);

			var members  = field.Name.Split('.');
			var defValue = Expression.Constant(dataContext.MappingSchema.GetDefaultValue(field.SystemType), field.SystemType);

			for (var i = 0; i < members.Length; i++)
			{
				var        member = members[i];
				Expression pof    = Expression.PropertyOrField(getter, member);

				getter = i == 0 ? pof : Expression.Condition(Expression.Equal(getter, Expression.Constant(null)), defValue, pof);
			}

			Expression dataTypeExpression = Expression.Constant(DataType.Undefined);

			var expr = dataContext.MappingSchema.GetConvertExpression(field.SystemType, typeof(DataParameter), createDefault: false);

			if (expr != null)
			{
				var body = expr.GetBody(getter);

				getter             = Expression.PropertyOrField(body, "Value");
				dataTypeExpression = Expression.PropertyOrField(body, "DataType");
			}

			var param = ExpressionBuilder.CreateParameterAccessor(
				dataContext, getter, dataTypeExpression, getter, exprParam, Expression.Parameter(typeof(object[]), "ps"), field.Name.Replace('.', '_'));

			return param;
		}

#endregion

#region SetRunQuery

		static Tuple<
			Func<Query,IDataContextEx,Mapper<T>,Expression,object[],int,IEnumerable<T>>,
			Func<Expression,object[],int>,
			Func<Expression,object[],int>>
			GetExecuteQuery<T>(
				Query query,
				Func<Query,IDataContextEx,Mapper<T>,Expression,object[],int,IEnumerable<T>> queryFunc)
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
						queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Skip(n);
						skip  = (expr, ps) => n;
					}
				}
				else if (select.SkipValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.SkipValue);
					queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Skip((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
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
						queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Take(n);
						take  = (expr, ps) => n;
					}
				}
				else if (select.TakeValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.TakeValue);
					queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Take((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
					take  = (expr,ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, ps);
				}
			}

			return Tuple.Create(queryFunc, skip, take);
		}

		static IEnumerable<T> ExecuteQuery<T>(
			Query          query,
			IDataContextEx dataContext,
			Mapper<T>      mapper,
			Expression     expression,
			object[]       ps,
			int            queryNumber)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps))
			try
			{
				mapper.QueryRunner = runner;

				using (var dr = runner.ExecuteReader())
				{
					while (dr.Read())
					{
						yield return mapper.Map(runner, dr);
						runner.RowsCount++;
					}
				}
			}
			finally
			{
				mapper.QueryRunner = null;
			}
		}

#if !NOASYNC

		static async Task ExecuteQueryAsync<T>(
			Query                         query,
			IDataContextEx                dataContext,
			Mapper<T>                     mapper,
			Expression                    expression,
			object[]                      ps,
			int                           queryNumber,
			Func<T,bool>                  func,
			Func<Expression,object[],int> skipAction,
			Func<Expression,object[],int> takeAction,
			CancellationToken             cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps))
			{
				Func<IDataReader,T> m = dr => mapper.Map(runner, dr);

				runner.SkipAction = skipAction != null ? () => skipAction(expression, ps) : null as Func<int>;
				runner.TakeAction = takeAction != null ? () => takeAction(expression, ps) : null as Func<int>;

				try
				{
					mapper.QueryRunner = runner;

					var dr = await runner.ExecuteReaderAsync(cancellationToken);
					await dr.QueryForEachAsync(m, r =>
					{
						var b = func(r);
						runner.RowsCount++;
						return b;
					}, cancellationToken);
				}
				finally
				{
					mapper.QueryRunner = null;
				}
			}
		}

#endif

		static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataReader,T>> expression)
		{
			var executeQuery = GetExecuteQuery<T>(query, ExecuteQuery);

			ClearParameters(query);

			var mapper   = new Mapper<T>(expression);
			var runQuery = executeQuery.Item1;

			query.GetIEnumerable = (db, expr, ps) => runQuery(query, db, mapper, expr, ps, 0);

#if !NOASYNC

			var skipAction = executeQuery.Item2;
			var takeAction = executeQuery.Item3;

			query.GetForEachAsync = (db, expr, ps, action, token) =>
				ExecuteQueryAsync(query, db, mapper, expr, ps, 0, action, skipAction, takeAction, token);

#endif
		}

		static readonly PropertyInfo _dataContextInfo  = MemberHelper.PropertyOf<IQueryRunner>( p => p.DataContext);
		static readonly PropertyInfo _expressionInfo   = MemberHelper.PropertyOf<IQueryRunner>( p => p.Expression);
		static readonly PropertyInfo _parametersnfo    = MemberHelper.PropertyOf<IQueryRunner>( p => p.Parameters);
		static readonly PropertyInfo _rowsCountnfo     = MemberHelper.PropertyOf<IQueryRunner>( p => p.RowsCount);

		static Expression<Func<IQueryRunner,IDataReader,T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],T>> expression)
		{
			var queryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
			var dataReaderParam = Expression.Parameter(typeof(IDataReader), "dr");

			return
				Expression.Lambda<Func<IQueryRunner,IDataReader,T>>(
					Expression.Invoke(
						expression, new Expression[]
						{
							queryRunnerParam,
							Expression.Property(queryRunnerParam, _dataContextInfo),
							dataReaderParam,
							Expression.Property(queryRunnerParam, _expressionInfo),
							Expression.Property(queryRunnerParam, _parametersnfo),
						}),
					queryRunnerParam,
					dataReaderParam);
		}

#endregion

#region SetRunQuery / Cast, Concat, Union, OfType, ScalarSelect, Select, SequenceContext, Table

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

#endregion

#region SetRunQuery / Select 2

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],int,T>> expression)
		{
			var queryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
			var dataReaderParam  = Expression.Parameter(typeof(IDataReader),  "dr");

			var l =
				Expression.Lambda<Func<IQueryRunner,IDataReader,T>>(
					Expression.Invoke(
						expression, new Expression[]
						{
							queryRunnerParam,
							Expression.Property(queryRunnerParam, _dataContextInfo),
							dataReaderParam,
							Expression.Property(queryRunnerParam, _expressionInfo),
							Expression.Property(queryRunnerParam, _parametersnfo),
							Expression.Property(queryRunnerParam, _rowsCountnfo),
						}),
					queryRunnerParam,
					dataReaderParam);

			SetRunQuery(query, l);
		}

#endregion

#region SetRunQuery / Aggregation, All, Any, Contains, Count

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],object>> expression)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			var l      = WrapMapper(expression);
			var mapper = new Mapper<object>(l);

			query.GetElement = (db, expr, ps) => ExecuteElement(query, db, mapper, expr, ps);

#if !NOASYNC
			query.GetElementAsync = (db, expr, ps, token) => ExecuteElementAsync<object>(query, db, mapper, expr, ps, token);
#endif
		}

		static T ExecuteElement<T>(
			Query          query,
			IDataContextEx dataContext,
			Mapper<T>      mapper,
			Expression     expression,
			object[]       ps)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps))
			try
			{
				mapper.QueryRunner = runner;

				using (var dr = runner.ExecuteReader())
				{
					while (dr.Read())
					{
						var value = mapper.Map(runner, dr);
						runner.RowsCount++;
						return value;
					}
				}

				return Array<T>.Empty.First();
			}
			finally
			{
				mapper.QueryRunner = null;
			}
		}

#if !NOASYNC

		static async Task<T> ExecuteElementAsync<T>(
			Query             query,
			IDataContextEx    dataContext,
			Mapper<object>    mapper,
			Expression        expression,
			object[]          ps,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps))
			{
				Func<IDataReader,object> m = dr => mapper.Map(runner, dr);

				try
				{
					mapper.QueryRunner = runner;

					var dr = await runner.ExecuteReaderAsync(cancellationToken);

					var item = default(T);
					var read = false;

					await dr.QueryForEachAsync(
						m,
						r =>
						{
							read = true;
							item = dataContext.MappingSchema.ChangeTypeTo<T>(r);
							runner.RowsCount++;
							return false;
						},
						cancellationToken);

					if (read)
						return item;

					return Array<T>.Empty.First();
				}
				finally
				{
					mapper.QueryRunner = null;
				}
			}
		}

#endif

#endregion

#region ScalarQuery

		public static void SetScalarQuery(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement = (db, expr, ps) => ScalarQuery(query, db, expr, ps);

#if !NOASYNC
			query.GetElementAsync = (db, expr, ps, token) => ScalarQueryAsync(query, db, expr, ps, token);
#endif
		}

		static object ScalarQuery(Query query, IDataContextEx dataContext, Expression expr, object[] parameters)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
				return runner.ExecuteScalar();
		}

#if !NOASYNC

		static async Task<object> ScalarQueryAsync(
			Query             query,
			IDataContextEx    dataContext,
			Expression        expression,
			object[]          ps,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps))
				return await runner.ExecuteScalarAsync(cancellationToken);
		}

#endif

#endregion

#region NonQueryQuery

		public static void SetNonQueryQuery(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement = (db, expr, ps) => NonQueryQuery(query, db, expr, ps);

#if !NOASYNC
			query.GetElementAsync = (db, expr, ps, token) => NonQueryQueryAsync(query, db, expr, ps, token);
#endif
		}

		static int NonQueryQuery(Query query, IDataContextEx dataContext, Expression expr, object[] parameters)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
				return runner.ExecuteNonQuery();
		}

#if !NOASYNC

		static async Task<object> NonQueryQueryAsync(
			Query             query,
			IDataContextEx    dataContext,
			Expression        expression,
			object[]          ps,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps))
				return await runner.ExecuteNonQueryAsync(cancellationToken);
		}

#endif

#endregion

#region NonQueryQuery2

		public static void SetNonQueryQuery2(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 2)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement = (db, expr, ps) => NonQueryQuery2(query, db, expr, ps);

#if !NOASYNC
			query.GetElementAsync = (db, expr, ps, token) => NonQueryQuery2Async(query, db, expr, ps, token);
#endif
		}

		static int NonQueryQuery2(Query query, IDataContextEx dataContext, Expression expr, object[] parameters)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
			{
				var n = runner.ExecuteNonQuery();

				if (n != 0)
					return n;

				runner.QueryNumber = 1;

				return runner.ExecuteNonQuery();
			}
		}

#if !NOASYNC

		static async Task<object> NonQueryQuery2Async(
			Query             query,
			IDataContextEx    dataContext,
			Expression        expr,
			object[]          parameters,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
			{
				var n = await runner.ExecuteNonQueryAsync(cancellationToken);

				if (n != 0)
					return n;

				runner.QueryNumber = 1;

				return await runner.ExecuteNonQueryAsync(cancellationToken);
			}
		}

#endif

#endregion

#region QueryQuery2

		public static void SetQueryQuery2(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 2)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement = (db, expr, ps) => QueryQuery2(query, db, expr, ps);

#if !NOASYNC
			query.GetElementAsync = (db, expr, ps, token) => QueryQuery2Async(query, db, expr, ps, token);
#endif
		}

		static int QueryQuery2(Query query, IDataContextEx dataContext, Expression expr, object[] parameters)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
			{
				var n = runner.ExecuteScalar();

				if (n != null)
					return 0;

				runner.QueryNumber = 1;

				return runner.ExecuteNonQuery();
			}
		}

#if !NOASYNC

		static async Task<object> QueryQuery2Async(
			Query             query,
			IDataContextEx    dataContext,
			Expression        expr,
			object[]          parameters,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
			{
				var n = await runner.ExecuteScalarAsync(cancellationToken);

				if (n != null)
					return 0;

				runner.QueryNumber = 1;

				return await runner.ExecuteNonQueryAsync(cancellationToken);
			}
		}

#endif

#endregion

#region GetSqlText

		public static string GetSqlText(Query query, IDataContext dataContext, Expression expr, object[] parameters, int idx)
		{
			var runner = ((IDataContextEx)dataContext).GetQueryRunner(query, 0, expr, parameters);
			return runner.GetSqlText();
		}

#endregion
	}
}
