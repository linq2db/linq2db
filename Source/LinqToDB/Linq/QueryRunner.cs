using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Async;
	using Builder;
	using Common;
	using Common.Internal.Cache;
	using Data;
	using Extensions;
	using LinqToDB.Expressions;
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class Cache<T>
		{
			static Cache()
			{
				Query.CacheCleaners.Add(ClearCache);
			}

			public static void ClearCache()
			{
				QueryCache.Compact(1);
			}

			internal static MemoryCache QueryCache { get; } = new MemoryCache(new MemoryCacheOptions());
		}

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
					_mapperExpression = (Expression<Func<IQueryRunner,IDataReader,T>>)_expression.Transform(
						e => e is ConvertFromDataReaderExpression ex ? ex.Reduce(dataReader) : e);

					var qr = QueryRunner;
					if (qr != null)
						qr.MapperExpression = _mapperExpression;

					_mapper = _mapperExpression.Compile();
				}

				try
				{
					return _mapper(queryRunner, dataReader);
				}
				catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is LinqToDBConvertException)
				{
					if (_isFaulted)
						throw;

					if (DataConnection.TraceSwitch.TraceInfo)
						DataConnection.WriteTraceLine(
							$"Mapper has switched to slow mode. Mapping exception: {ex.Message}",
							DataConnection.TraceSwitch.DisplayName);

					var qr = QueryRunner;
					if (qr != null)
						qr.MapperExpression = _mapperExpression;

					_mapper = _expression.Compile();

					_isFaulted = true;

					return _mapper(queryRunner, dataReader);
				}
			}
		}

		#endregion

		#region Helpers

		static void FinalizeQuery(Query query)
		{
			foreach (var sql in query.Queries)
			{
				sql.Statement = query.SqlOptimizer.Finalize(sql.Statement);
				var parameters =
					sql.Parameters
						.Select(p => new {p, idx = sql.Statement.Parameters.IndexOf(p.SqlParameter)})
						.OrderBy(p => p.idx)
						.Select(p => p.p);

				var alreadyAdded = new HashSet<SqlParameter>(sql.Parameters.Select(pp => pp.SqlParameter));

				var runtime = sql.Statement.Parameters.Where(p => !alreadyAdded.Contains(p));

				// combining with dynamically created parameters

				parameters = parameters.Concat(
					runtime.Select(p => new ParameterAccessor(Expression.Constant(p.Value), (e, o) => p.Value,
						(e, o) => p.DataType != DataType.Undefined || p.Value == null
							? p.DataType
							: query.MappingSchema.GetDataType(p.Value.GetType()).DataType,
						(e, o) => p.DbType,
						(e, o) => p.DbSize,
						p))
				);

				sql.Parameters = parameters.ToList();
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

				if (value is IEnumerable vs)
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

				var dbType = p.DbTypeAccessor(expression, parameters);

				if (!string.IsNullOrEmpty(dbType))
					p.SqlParameter.DbType = dbType;

				var size = p.SizeAccessor(expression, parameters);

				if (size != null)
					p.SqlParameter.DbSize = size;

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

			getter = field.ColumnDescriptor.MemberAccessor.GetterExpression.GetBody(getter);

			Expression dataTypeExpression = Expression.Constant(DataType.Undefined);
			Expression dbTypeExpression   = Expression.Constant(null, typeof(string));
			Expression dbSizeExpression   = Expression.Constant(field.Length, typeof(int?));

			var convertExpression = dataContext.MappingSchema.GetConvertExpression(new DbDataType(field.SystemType, field.DataType, field.DbType, field.Length),
				new DbDataType(typeof(DataParameter), field.DataType, field.DbType, field.Length), createDefault: false);

			if (convertExpression != null)
			{
				var body           = convertExpression.GetBody(getter);
				getter             = Expression.PropertyOrField(body, "Value");
				dataTypeExpression = Expression.PropertyOrField(body, "DataType");
				dbTypeExpression   = Expression.PropertyOrField(body, "DbType");
				dbSizeExpression   = Expression.PropertyOrField(body, "Size");
			}

			var param = ExpressionBuilder.CreateParameterAccessor(
				dataContext, getter, dataTypeExpression, dbTypeExpression, dbSizeExpression, getter, exprParam, Expression.Parameter(typeof(object[]), "ps"), field.Name.Replace('.', '_'), expr: convertExpression);

			return param;
		}

		private static Type GetType<T>(T obj, IDataContext db)
			//=> typeof(T);
			//=> obj.GetType();
			=> db.MappingSchema.GetEntityDescriptor(typeof(T)).InheritanceMapping?.Count > 0 ? obj.GetType() : typeof(T);

		#endregion

		#region SetRunQuery

		static Tuple<
			Func<Query,IDataContext,Mapper<T>,Expression,object[],object[],int,IEnumerable<T>>,
			Func<Expression,object[],int>,
			Func<Expression,object[],int>>
			GetExecuteQuery<T>(
				Query query,
				Func<Query,IDataContext,Mapper<T>,Expression,object[],object[],int,IEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			Func<Expression,object[],int> skip = null, take = null;

			var selectQuery = query.Queries[0].Statement.SelectQuery;
			var select      = selectQuery.Select;

			if (select.SkipValue != null && !query.SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery))
			{
				var q = queryFunc;

				if (select.SkipValue is SqlValue value)
				{
					var n = (int)((IValueContainer)value).Value;

					if (n > 0)
					{
						queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Skip(n);
						skip  = (expr, ps) => n;
					}
				}
				else if (select.SkipValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.SkipValue);
					queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Skip((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
					skip  = (expr,ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, ps);
				}
			}

			if (select.TakeValue != null && !query.SqlProviderFlags.IsTakeSupported)
			{
				var q = queryFunc;

				if (select.TakeValue is SqlValue value)
				{
					var n = (int)((IValueContainer)value).Value;

					if (n > 0)
					{
						queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Take(n);
						take      = (expr, ps) => n;
					}
				}
				else if (select.TakeValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.TakeValue);
					queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Take((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
					take  = (expr,ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, ps);
				}
			}

			return Tuple.Create(queryFunc, skip, take);
		}

		static IEnumerable<T> ExecuteQuery<T>(
			Query        query,
			IDataContext dataContext,
			Mapper<T>    mapper,
			Expression   expression,
			object[]     ps,
			object[]     preambles,
			int          queryNumber)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps, preambles))
			try
			{
				mapper.QueryRunner = runner;

				using (var dr = runner.ExecuteReader())
				{
					while (dr.Read())
					{
						var value = mapper.Map(runner, dr);
						runner.RowsCount++;
						yield return value;
					}
				}
			}
			finally
			{
				mapper.QueryRunner = null;
			}
		}

		static async Task ExecuteQueryAsync<T>(
			Query                         query,
			IDataContext                  dataContext,
			Mapper<T>                     mapper,
			Expression                    expression,
			object[]                      ps,
			object[]                      preambles,
			int                           queryNumber,
			Func<T,bool>                  func,
			Func<Expression,object[],int> skipAction,
			Func<Expression,object[],int> takeAction,
			CancellationToken             cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps, preambles))
			{
				try
				{
					mapper.QueryRunner = runner;

					using (var dr = await runner.ExecuteReaderAsync(cancellationToken))
					{
						var skip = skipAction?.Invoke(expression, ps) ?? 0;

						while (skip-- > 0 && await dr.ReadAsync(cancellationToken))
							{}

						var take = takeAction?.Invoke(expression, ps) ?? int.MaxValue;

						while (take-- > 0 && await dr.ReadAsync(cancellationToken))
						{
							runner.RowsCount++;
							if (!func(mapper.Map(runner, dr.DataReader)))
								break;
						}
					}
				}
				finally
				{
					mapper.QueryRunner = null;
				}
			}
		}

		class AsyncEnumeratorImpl<T> : IAsyncEnumerator<T>
		{
			readonly Query                         _query;
			readonly IDataContext                  _dataContext;
			readonly Mapper<T>                     _mapper;
			readonly Expression                    _expression;
			readonly object[]                      _ps;
			readonly object[]                      _preambles;
			readonly int                           _queryNumber;
			readonly Func<Expression,object[],int> _skipAction;
			readonly Func<Expression,object[],int> _takeAction;

			IQueryRunner     _queryRunner;
			IDataReaderAsync _dataReader;
			int              _take;

			public AsyncEnumeratorImpl(
				Query                         query,
				IDataContext                  dataContext,
				Mapper<T>                     mapper,
				Expression                    expression,
				object[]                      ps,
				object[]                      preambles,
				int                           queryNumber,
				Func<Expression,object[],int> skipAction,
				Func<Expression,object[],int> takeAction)
			{
				_query       = query;
				_dataContext = dataContext;
				_mapper      = mapper;
				_expression  = expression;
				_ps          = ps;
				_preambles   = preambles;
				_queryNumber = queryNumber;
				_skipAction  = skipAction;
				_takeAction  = takeAction;
			}

			public T Current { get; set; }

			public async Task<bool> MoveNext(CancellationToken cancellationToken)
			{
				if (_queryRunner == null)
				{
					_queryRunner = _dataContext.GetQueryRunner(_query, _queryNumber, _expression, _ps, _preambles);

					_mapper.QueryRunner = _queryRunner;

					_dataReader = await _queryRunner.ExecuteReaderAsync(cancellationToken);

					var skip = _skipAction?.Invoke(_expression, _ps) ?? 0;

					while (skip-- > 0)
					{
						if (!await _dataReader.ReadAsync(cancellationToken))
							return false;
					}

					_take = _takeAction?.Invoke(_expression, _ps) ?? int.MaxValue;
				}

				if (_take-- > 0 && await _dataReader.ReadAsync(cancellationToken))
				{
					_queryRunner.RowsCount++;

					Current = _mapper.Map(_queryRunner, _dataReader.DataReader);

					return true;
				}

				return false;
			}

			public void Dispose()
			{
				_queryRunner?.Dispose();
				_dataReader ?.Dispose();

				_mapper.QueryRunner = _queryRunner = null;
			}
		}

		class AsyncEnumerableImpl<T> : IAsyncEnumerable<T>
		{
			readonly Query                         _query;
			readonly IDataContext                  _dataContext;
			readonly Mapper<T>                     _mapper;
			readonly Expression                    _expression;
			readonly object[]                      _ps;
			readonly object[]                      _preambles;
			readonly int                           _queryNumber;
			readonly Func<Expression,object[],int> _skipAction;
			readonly Func<Expression,object[],int> _takeAction;

			public AsyncEnumerableImpl(
				Query                         query,
				IDataContext                  dataContext,
				Mapper<T>                     mapper,
				Expression                    expression,
				object[]                      ps,
				object[]                      preambles,
				int                           queryNumber,
				Func<Expression,object[],int> skipAction,
				Func<Expression,object[],int> takeAction)
			{
				_query       = query;
				_dataContext = dataContext;
				_mapper      = mapper;
				_expression  = expression;
				_ps          = ps;
				_preambles   = preambles;
				_queryNumber = queryNumber;
				_skipAction  = skipAction;
				_takeAction  = takeAction;
			}

			public IAsyncEnumerator<T> GetEnumerator()
			{
				return new AsyncEnumeratorImpl<T>(
					_query, _dataContext, _mapper, _expression, _ps, _preambles, _queryNumber, _skipAction, _takeAction);
			}
		}

		static IAsyncEnumerable<T> ExecuteQueryAsync<T>(
			Query                         query,
			IDataContext                  dataContext,
			Mapper<T>                     mapper,
			Expression                    expression,
			object[]                      ps,
			object[]                      preambles,
			int                           queryNumber,
			Func<Expression,object[],int> skipAction,
			Func<Expression,object[],int> takeAction)
		{
			return new AsyncEnumerableImpl<T>(
				query, dataContext, mapper, expression, ps, preambles, queryNumber, skipAction, takeAction);
		}

		static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataReader,T>> expression)
		{
			var executeQuery = GetExecuteQuery<T>(query, ExecuteQuery);

			ClearParameters(query);

			var mapper   = new Mapper<T>(expression);
			var runQuery = executeQuery.Item1;

			query.GetIEnumerable = (db, expr, ps, preambles) => runQuery(query, db, mapper, expr, ps, preambles, 0);

			var skipAction = executeQuery.Item2;
			var takeAction = executeQuery.Item3;

			query.GetForEachAsync = (db, expr, ps, preambles, action, token) =>
				ExecuteQueryAsync(query, db, mapper, expr, ps, preambles, 0, action, skipAction, takeAction, token);

			query.GetIAsyncEnumerable = (db, expr, ps, preambles) =>
				ExecuteQueryAsync(query, db, mapper, expr, ps, preambles, 0, skipAction, takeAction);
		}

		static readonly PropertyInfo _dataContextInfo = MemberHelper.PropertyOf<IQueryRunner>( p => p.DataContext);
		static readonly PropertyInfo _expressionInfo  = MemberHelper.PropertyOf<IQueryRunner>( p => p.Expression);
		static readonly PropertyInfo _parametersInfo  = MemberHelper.PropertyOf<IQueryRunner>( p => p.Parameters);
		static readonly PropertyInfo _preamblesInfo   = MemberHelper.PropertyOf<IQueryRunner>( p => p.Preambles);
		static readonly PropertyInfo _rowsCountnfo    = MemberHelper.PropertyOf<IQueryRunner>( p => p.RowsCount);

		static Expression<Func<IQueryRunner,IDataReader,T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],object[],T>> expression)
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
							Expression.Property(queryRunnerParam, _parametersInfo),
							Expression.Property(queryRunnerParam, _preamblesInfo),
						}),
					queryRunnerParam,
					dataReaderParam);
		}

		#endregion

		#region SetRunQuery / Cast, Concat, Union, OfType, ScalarSelect, Select, SequenceContext, Table

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],object[],T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Select 2

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],object[],int,T>> expression)
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
							Expression.Property(queryRunnerParam, _parametersInfo),
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
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],object[],object>> expression)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			var l      = WrapMapper(expression);
			var mapper = new Mapper<object>(l);

			query.GetElement      = (db, expr, ps, preambles) => ExecuteElement(query, db, mapper, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => ExecuteElementAsync<object>(query, db, mapper, expr, ps, preambles, token);
		}

		static T ExecuteElement<T>(
			Query          query,
			IDataContext   dataContext,
			Mapper<T>      mapper,
			Expression     expression,
			object[]       ps,
			object[]       preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
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

		static async Task<T> ExecuteElementAsync<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<object>    mapper,
			Expression        expression,
			object[]          ps,
			object[]          preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
			{
				try
				{
					mapper.QueryRunner = runner;

					using (var dr = await runner.ExecuteReaderAsync(cancellationToken))
					{
						if (await dr.ReadAsync(cancellationToken))
						{
							runner.RowsCount++;

							var item = mapper.Map(runner, dr.DataReader);

							return dataContext.MappingSchema.ChangeTypeTo<T>(item);
						}

						return Array<T>.Empty.First();
					}
				}
				finally
				{
					mapper.QueryRunner = null;
				}
			}
		}

		#endregion

		#region ScalarQuery

		public static void SetScalarQuery(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps, preambles) => ScalarQuery(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => ScalarQueryAsync(query, db, expr, ps, preambles, token);
		}

		static object ScalarQuery(Query query, IDataContext dataContext, Expression expr, object[] parameters, object[] preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
				return runner.ExecuteScalar();
		}

		static async Task<object> ScalarQueryAsync(
			Query             query,
			IDataContext      dataContext,
			Expression        expression,
			object[]          ps,
			object[]          preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
				return await runner.ExecuteScalarAsync(cancellationToken);
		}

		#endregion

		#region NonQueryQuery

		public static void SetNonQueryQuery(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps, preambles) => NonQueryQuery(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => NonQueryQueryAsync(query, db, expr, ps, preambles, token);
		}

		static int NonQueryQuery(Query query, IDataContext dataContext, Expression expr, object[] parameters, object[] preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
				return runner.ExecuteNonQuery();
		}

		static async Task<object> NonQueryQueryAsync(
			Query             query,
			IDataContext      dataContext,
			Expression        expression,
			object[]          ps,
			object[]          preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
				return await runner.ExecuteNonQueryAsync(cancellationToken);
		}

		#endregion

		#region NonQueryQuery2

		public static void SetNonQueryQuery2(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 2)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps, preambles)        => NonQueryQuery2(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => NonQueryQuery2Async(query, db, expr, ps, preambles, token);
		}

		static int NonQueryQuery2(Query query, IDataContext dataContext, Expression expr, object[] parameters, object[] preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
			{
				var n = runner.ExecuteNonQuery();

				if (n != 0)
					return n;

				runner.QueryNumber = 1;

				return runner.ExecuteNonQuery();
			}
		}

		static async Task<object> NonQueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object[]          parameters,
			object[]          preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
			{
				var n = await runner.ExecuteNonQueryAsync(cancellationToken);

				if (n != 0)
					return n;

				runner.QueryNumber = 1;

				return await runner.ExecuteNonQueryAsync(cancellationToken);
			}
		}

		#endregion

		#region QueryQuery2

		public static void SetQueryQuery2(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 2)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps, preambles) => QueryQuery2(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => QueryQuery2Async(query, db, expr, ps, preambles, token);
		}

		static int QueryQuery2(Query query, IDataContext dataContext, Expression expr, object[] parameters, object[] preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
			{
				var n = runner.ExecuteScalar();

				if (n != null)
					return 0;

				runner.QueryNumber = 1;

				return runner.ExecuteNonQuery();
			}
		}

		static async Task<object> QueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object[]          parameters,
			object[]          preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
			{
				var n = await runner.ExecuteScalarAsync(cancellationToken);

				if (n != null)
					return 0;

				runner.QueryNumber = 1;

				return await runner.ExecuteNonQueryAsync(cancellationToken);
			}
		}

		#endregion

		#region GetSqlText

		public static string GetSqlText(Query query, IDataContext dataContext, Expression expr, object[] parameters, object[] preambles, int idx)
		{
			var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles);
			return runner.GetSqlText();
		}

		#endregion
	}
}
