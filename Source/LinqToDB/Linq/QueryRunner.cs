using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using Builder;
	using Common;
	using Common.Internal.Cache;
	using Common.Logging;
	using Data;
	using Extensions;
	using Async;
	using LinqToDB.Expressions;
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class Cache<T>
		{
			static Cache()
			{
				Query.CacheCleaners.Enqueue(ClearCache);
			}

			public static void ClearCache()
			{
				QueryCache.Clear();
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
			readonly ConcurrentDictionary<Type, ReaderMapperInfo> _mappers = new ConcurrentDictionary<Type, ReaderMapperInfo>();


			class ReaderMapperInfo
			{
				public Expression<Func<IQueryRunner, IDataReader, T>> MapperExpression = null!;
				public Func<IQueryRunner, IDataReader, T>             Mapper = null!;
				public bool                                           IsFaulted;
			}

			public T Map(IDataContext context, IQueryRunner queryRunner, IDataReader dataReader)
			{
				// unwrap early
				// https://github.com/linq2db/linq2db/issues/2499
				dataReader = DataReaderWrapCache.TryUnwrapDataReader(context.MappingSchema, dataReader);

				var dataReaderType = dataReader.GetType();

				if (!_mappers.TryGetValue(dataReaderType, out var mapperInfo))
				{
					var mapperExpression = TransformMapperExpression(context, dataReader, dataReaderType, false);

					queryRunner.MapperExpression = mapperExpression;

					var mapper = mapperExpression.Compile();
					mapperInfo = new ReaderMapperInfo() { MapperExpression = mapperExpression, Mapper = mapper };
					_mappers.TryAdd(dataReaderType, mapperInfo);
				}

				try
				{
					return mapperInfo.Mapper(queryRunner, dataReader);
				}
				// SqlNullValueException: MySqlData
				// OracleNullValueException: managed and native oracle providers
				catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is LinqToDBConvertException || ex.GetType().Name.Contains("NullValueException"))
				{
					// TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
					if (mapperInfo.IsFaulted)
						throw;

					if (context.GetTraceSwitch().TraceInfo)
						context.WriteTraceLine(
							$"Mapper has switched to slow mode. Mapping exception: {ex.Message}",
							context.GetTraceSwitch().DisplayName,
							TraceLevel.Error);

					queryRunner.MapperExpression = mapperInfo.MapperExpression;

					var expression = TransformMapperExpression(context, dataReader, dataReaderType, true);

					// create new instance to avoid race conditions without locks
					var expr   = mapperInfo.MapperExpression;
					mapperInfo = new ReaderMapperInfo()
					{
						MapperExpression = expr,
						Mapper           = expression.Compile(),
						IsFaulted        = true
					};

					_mappers[dataReaderType] = mapperInfo;

					return mapperInfo.Mapper(queryRunner, dataReader);
				}
			}

			// transform extracted to separate method to avoid closures allocation on mapper cache hit
			private Expression<Func<IQueryRunner, IDataReader, T>> TransformMapperExpression(
				IDataContext context,
				IDataReader  dataReader,
				Type         dataReaderType,
				bool         slowMode)
			{
				var variableType  = dataReader.GetType();

				ParameterExpression? oldVariable = null;
				ParameterExpression? newVariable = null;

				Expression expression;
				if (slowMode)
				{
					expression = _expression.Transform(e =>
					{
						if (e is ConvertFromDataReaderExpression ex)
							return new ConvertFromDataReaderExpression(ex.Type, ex.Index, ex.Converter, newVariable!, context).Reduce();

						return replaceVariable(e);
					});
				}
				else
				{
					expression = _expression.Transform(
						e =>
						{
							if (e is ConvertFromDataReaderExpression ex)
								return ex.Reduce(context, dataReader, newVariable!).Transform(replaceVariable);

							return replaceVariable(e);
						});
				}

				if (Configuration.OptimizeForSequentialAccess)
					expression = SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(expression, dataReader.FieldCount, reduce: false);

				return (Expression<Func<IQueryRunner, IDataReader, T>>)expression;

				Expression replaceVariable(Expression e)
				{
					if (e is ParameterExpression vex && vex.Name == "ldr")
					{
						oldVariable = vex;
						return newVariable ??= Expression.Variable(variableType, "ldr");
					}

					if (e is BinaryExpression bex
						&& bex.NodeType == ExpressionType.Assign
						&& bex.Left == oldVariable)
					{
						Expression dataReaderExpression = Expression.Convert(_expression.Parameters[1], dataReaderType);

						return Expression.Assign(newVariable, dataReaderExpression);
					}

					return e;
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

				SqlStatement.PrepareQueryAndAliases(sql.Statement, null, out var aliasesContext);

				sql.Parameters = aliasesContext.GetParameters();
				sql.Aliases    = aliasesContext;
			}
		}

		static void ClearParameters(Query query)
		{
#if !DEBUG
			foreach (var q in query.Queries)
				foreach (var sqlParameter in q.ParameterAccessors)
					sqlParameter.Expression = null!;
#endif
		}

		static int EvaluateTakeSkipValue(Query query, Expression expr, IDataContext? db, object?[]? ps, int qn,
			ISqlExpression sqlExpr)
				{
			var parameterValues = new SqlParameterValues();
			SetParameters(query, expr, db, ps, qn, parameterValues);

			var evaluated = sqlExpr.EvaluateExpression(new EvaluationContext(parameterValues)) as int?;
			if (evaluated == null)
				throw new InvalidOperationException($"Can not evaluate integer expression from '{sqlExpr}'.");
			return evaluated.Value;
		}

		internal static void SetParameters(
			Query query, Expression expression, IDataContext? parametersContext, object?[]? parameters, int queryNumber, SqlParameterValues parameterValues)
		{
			var queryContext = query.Queries[queryNumber];

			foreach (var p in queryContext.ParameterAccessors)
			{
				var value = p.ValueAccessor(expression, parametersContext, parameters);

				if (value is IEnumerable vs)
				{
					var type = vs.GetType();
					var etype = type.GetItemType();

					if (etype == null || etype == typeof(object) || etype.IsEnum ||
						type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
						etype.GetGenericArguments()[0].IsEnum)
					{
						var values = new List<object?>();

						foreach (var v in vs)
						{
							value = v;

							if (v != null)
							{
								var valueType = v.GetType();

								if (valueType.ToNullableUnderlying().IsEnum)
									value = query.GetConvertedEnum(valueType, v);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				var dbDataType = p.DbDataTypeAccessor(expression, parametersContext, parameters);

				parameterValues.AddValue(p.SqlParameter, value, p.SqlParameter.Type.WithSetValues(dbDataType));
			}
		}

		internal static ParameterAccessor GetParameter(Type type, IDataContext dataContext, SqlField field)
		{
			var exprParam = Expression.Parameter(typeof(Expression), "expr");

			Expression getter = Expression.Convert(
				Expression.Property(
					Expression.Convert(exprParam, typeof(ConstantExpression)),
					ReflectionHelper.Constant.Value),
				type);

			var descriptor    = field.ColumnDescriptor;
			var dbValueLambda = descriptor.GetDbParamLambda();

			Expression? valueGetter;
			Expression? dbDataTypeExpression;

			valueGetter = InternalExtensions.ApplyLambdaToExpression(dbValueLambda, getter);

			if (typeof(DataParameter).IsSameOrParentOf(valueGetter.Type))
			{
				dbDataTypeExpression = Expression.Call(Expression.Constant(field.ColumnDescriptor.GetDbDataType(false)),
					DbDataType.WithSetValuesMethodInfo,
					Expression.PropertyOrField(valueGetter, nameof(DataParameter.DbDataType)));
				valueGetter          = Expression.PropertyOrField(valueGetter, nameof(DataParameter.Value));
			}
			else
			{
				var dbDataType       = field.ColumnDescriptor.GetDbDataType(true).WithSystemType(valueGetter.Type);
				dbDataTypeExpression = Expression.Constant(dbDataType);
			}

			var param = ExpressionBuilder.CreateParameterAccessor(
				dataContext, valueGetter, getter, dbDataTypeExpression, valueGetter, exprParam, Expression.Parameter(typeof(object[]), "ps"), Expression.Parameter(typeof(IDataContext), "ctx"), field.Name.Replace('.', '_'));

			return param;
		}

		private static Type GetType<T>([DisallowNull] T obj, IDataContext db)
			//=> typeof(T);
			//=> obj.GetType();
			=> db.MappingSchema.GetEntityDescriptor(typeof(T)).InheritanceMapping?.Count > 0 ? obj!.GetType() : typeof(T);

		#endregion

		#region SetRunQuery

		public delegate int TakeSkipDelegate(
			Query                    query, 
			Expression               expression, 
			IDataContext?            dataContext,
			object?[]?               ps);

		static Tuple<
			Func<Query,IDataContext,Mapper<T>,Expression,object?[]?,object?[]?,int,IEnumerable<T>>,
			TakeSkipDelegate?,
			TakeSkipDelegate?>
			GetExecuteQuery<T>(
				Query query,
				Func<Query,IDataContext,Mapper<T>,Expression,object?[]?,object?[]?,int,IEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			TakeSkipDelegate? skip = null, take = null;

			var selectQuery = query.Queries[0].Statement.SelectQuery!;
			var select      = selectQuery.Select;

			if (select.SkipValue != null && !query.SqlProviderFlags.GetIsSkipSupportedFlag(select.TakeValue, select.SkipValue))
			{
				var q = queryFunc;

				queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Skip(EvaluateTakeSkipValue(qq, expr, db, ps, qn, select.SkipValue));
				skip      = (qq, expr, pc, ps) => EvaluateTakeSkipValue(qq, expr, pc, ps, 0, select.SkipValue);
			}

			if (select.TakeValue != null && !query.SqlProviderFlags.IsTakeSupported)
			{
				var q = queryFunc;

				queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Take(EvaluateTakeSkipValue(qq, expr, db, ps, qn, select.TakeValue));
				take      = (qq, expr, pc, ps) => EvaluateTakeSkipValue(qq, expr, pc, ps, 0, select.TakeValue);
			}

			return Tuple.Create(queryFunc, skip, take);
		}

		static IEnumerable<T> ExecuteQuery<T>(
			Query        query,
			IDataContext dataContext,
			Mapper<T>    mapper,
			Expression   expression,
			object?[]?   ps,
			object?[]?   preambles,
			int          queryNumber)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps, preambles))
			using (var dr     = runner.ExecuteReader())
			{
				while (dr.DataReader!.Read())
				{
					var value = mapper.Map(dataContext, runner, dr.DataReader!);
					runner.RowsCount++;
					yield return value;
				}
			}
		}

		static async Task ExecuteQueryAsync<T>(
			Query                         query,
			IDataContext                  dataContext,
			Mapper<T>                     mapper,
			Expression                    expression,
			object?[]?                    ps,
			object?[]?                    preambles,
			int                           queryNumber,
			Func<T,bool>                  func,
			TakeSkipDelegate?        skipAction,
			TakeSkipDelegate?        takeAction,
			CancellationToken             cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps, preambles))
			{
				var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
#if !NETFRAMEWORK
				await using (dr.ConfigureAwait(Configuration.ContinueOnCapturedContext))
#else
				using (dr)
#endif
				{
					var skip = skipAction?.Invoke(query, expression, dataContext, ps) ?? 0;

					while (skip-- > 0 && await dr.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					{}

					var take = takeAction?.Invoke(query, expression, dataContext, ps) ?? int.MaxValue;

					while (take-- > 0 && await dr.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					{
						runner.RowsCount++;
						if (!func(mapper.Map(dataContext, runner, dr.DataReader)))
							break;
					}
				}
			}
		}

		class AsyncEnumeratorImpl<T> : IAsyncEnumerator<T>
		{
			readonly Query             _query;
			readonly IDataContext      _dataContext;
			readonly Mapper<T>         _mapper;
			readonly Expression        _expression;
			readonly object?[]?        _ps;
			readonly object?[]?        _preambles;
			readonly int               _queryNumber;
			readonly TakeSkipDelegate? _skipAction;
			readonly TakeSkipDelegate? _takeAction;
			readonly CancellationToken _cancellationToken;

			IQueryRunner?     _queryRunner;
			IDataReaderAsync? _dataReader;
			int              _take;

			public AsyncEnumeratorImpl(
				Query             query,
				IDataContext      dataContext,
				Mapper<T>         mapper,
				Expression        expression,
				object?[]?        ps,
				object?[]?        preambles,
				int               queryNumber,
				TakeSkipDelegate? skipAction,
				TakeSkipDelegate? takeAction,
				CancellationToken cancellationToken)
			{
				_query             = query;
				_dataContext       = dataContext;
				_mapper            = mapper;
				_expression        = expression;
				_ps                = ps;
				_preambles         = preambles;
				_queryNumber       = queryNumber;
				_skipAction        = skipAction;
				_takeAction        = takeAction;
				_cancellationToken = cancellationToken;
			}

			public T Current { get; set; } = default!;

#if NETFRAMEWORK
			public async Task<bool> MoveNextAsync()
#else
			public async ValueTask<bool> MoveNextAsync()
#endif
			{
				if (_queryRunner == null)
				{
					_queryRunner = _dataContext.GetQueryRunner(_query, _queryNumber, _expression, _ps, _preambles);

					_dataReader = await _queryRunner.ExecuteReaderAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

					var skip = _skipAction?.Invoke(_query, _expression, _dataContext, _ps) ?? 0;

					while (skip-- > 0)
					{
						if (!await _dataReader.ReadAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
							return false;
					}

					_take = _takeAction?.Invoke(_query, _expression, _dataContext, _ps) ?? int.MaxValue;
				}

				if (_take-- > 0 && await _dataReader!.ReadAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
				{
					_queryRunner.RowsCount++;

					Current = _mapper.Map(_dataContext, _queryRunner, _dataReader.DataReader);

					return true;
				}

				return false;
			}

			public void Dispose()
			{
				_queryRunner?.Dispose();
				_dataReader ?.Dispose();

				_queryRunner = null;
				_dataReader  = null;
			}

#if NETFRAMEWORK
			public Task DisposeAsync()
			{
				Dispose();
				return TaskEx.CompletedTask;
			}
#else
			public async ValueTask DisposeAsync()
			{
				_queryRunner?.Dispose();
				if (_dataReader != null)
				{
					await _dataReader.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					_dataReader = null;
				}
				
				_queryRunner = null;
			}
#endif
		}

		class AsyncEnumerableImpl<T> : IAsyncEnumerable<T>
		{
			readonly Query             _query;
			readonly IDataContext      _dataContext;
			readonly Mapper<T>         _mapper;
			readonly Expression        _expression;
			readonly object?[]?        _ps;
			readonly object?[]?        _preambles;
			readonly int               _queryNumber;
			readonly TakeSkipDelegate? _skipAction;
			readonly TakeSkipDelegate? _takeAction;

			public AsyncEnumerableImpl(
				Query             query,
				IDataContext      dataContext,
				Mapper<T>         mapper,
				Expression        expression,
				object?[]?        ps,
				object?[]?        preambles,
				int               queryNumber,
				TakeSkipDelegate? skipAction,
				TakeSkipDelegate? takeAction)
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

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
			{
				return new AsyncEnumeratorImpl<T>(
					_query, _dataContext, _mapper, _expression, _ps, _preambles, _queryNumber, _skipAction, _takeAction, cancellationToken);
			}
		}

		static IAsyncEnumerable<T> ExecuteQueryAsync<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<T>         mapper,
			Expression        expression,
			object?[]?        ps,
			object?[]?        preambles,
			int               queryNumber,
			TakeSkipDelegate? skipAction,
			TakeSkipDelegate? takeAction)
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
		static readonly PropertyInfo _rowsCountInfo   = MemberHelper.PropertyOf<IQueryRunner>( p => p.RowsCount);

		static Expression<Func<IQueryRunner,IDataReader,T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,T>> expression)
		{
			var queryRunnerParam = expression.Parameters[0];
			var dataReaderParam  = expression.Parameters[2];

			var dataContextVar = expression.Parameters[1];
			var expressionVar  = expression.Parameters[3];
			var parametersVar  = expression.Parameters[4];
			var preamblesVar   = expression.Parameters[5];

			// we can safely assume it is block expression
			if (expression.Body is not BlockExpression block)
				throw new LinqException("BlockExpression missing for mapper");
			return
				Expression.Lambda<Func<IQueryRunner,IDataReader,T>>(
					block.Update(
						new[]
						{
							dataContextVar,
							expressionVar,
							parametersVar,
							preamblesVar
						}.Concat(block.Variables),
						new[]
						{
							Expression.Assign(dataContextVar, Expression.Property(queryRunnerParam, _dataContextInfo)),
							Expression.Assign(expressionVar , Expression.Property(queryRunnerParam, _expressionInfo)),
							Expression.Assign(parametersVar , Expression.Property(queryRunnerParam, _parametersInfo)),
							Expression.Assign(preamblesVar  , Expression.Property(queryRunnerParam, _preamblesInfo))
						}.Concat(block.Expressions)),
					queryRunnerParam,
					dataReaderParam);
		}

		#endregion

		#region SetRunQuery / Cast, Concat, Union, OfType, ScalarSelect, Select, SequenceContext, Table

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Select 2

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,int,T>> expression)
		{
			var queryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
			var dataReaderParam  = Expression.Parameter(typeof(IDataReader),  "dr");

			var dataContextVar = expression.Parameters[1];
			var expressionVar  = expression.Parameters[3];
			var parametersVar  = expression.Parameters[4];
			var preamblesVar   = expression.Parameters[5];
			var rowsCountVar   = expression.Parameters[6];

			// we can safely assume it is block expression
			var block = (BlockExpression)expression.Body;
			var l     = Expression.Lambda<Func<IQueryRunner, IDataReader, T>>(
					block.Update(
						new[]
						{
							dataContextVar,
							expressionVar,
							parametersVar,
							preamblesVar,
							rowsCountVar
						}.Concat(block.Variables),
						new[]
						{
							Expression.Assign(dataContextVar, Expression.Property(queryRunnerParam, _dataContextInfo)),
							Expression.Assign(expressionVar , Expression.Property(queryRunnerParam, _expressionInfo)),
							Expression.Assign(parametersVar , Expression.Property(queryRunnerParam, _parametersInfo)),
							Expression.Assign(preamblesVar  , Expression.Property(queryRunnerParam, _preamblesInfo)),
							Expression.Assign(rowsCountVar  , Expression.Property(queryRunnerParam, _rowsCountInfo))
						}.Concat(block.Expressions)),
					queryRunnerParam,
					dataReaderParam);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Aggregation, All, Any, Contains, Count

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,object>> expression)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			var l      = WrapMapper(expression);
			var mapper = new Mapper<object>(l);

			query.GetElement      = (db, expr, ps, preambles) => ExecuteElement(query, db, mapper, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => ExecuteElementAsync<object?>(query, db, mapper, expr, ps, preambles, token);
		}

		static T ExecuteElement<T>(
			Query          query,
			IDataContext   dataContext,
			Mapper<T>      mapper,
			Expression     expression,
			object?[]?     ps,
			object?[]?     preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
			{
				using (var dr = runner.ExecuteReader())
				{
					while (dr.DataReader!.Read())
					{
						var value = mapper.Map(dataContext, runner, dr.DataReader!);
						runner.RowsCount++;
						return value;
					}
				}

				return Array<T>.Empty.First();
			}
		}

		static async Task<T> ExecuteElementAsync<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<object>    mapper,
			Expression        expression,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
			{
				var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
#if !NETFRAMEWORK
				await using (dr.ConfigureAwait(Configuration.ContinueOnCapturedContext))
#else
				using (dr)
#endif
				{
					if (await dr.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					{
						runner.RowsCount++;

						var item = mapper.Map(dataContext, runner, dr.DataReader);

						return dataContext.MappingSchema.ChangeTypeTo<T>(item);
					}

					return Array<T>.Empty.First();
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

		static object? ScalarQuery(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
				return runner.ExecuteScalar();
		}

		static async Task<object?> ScalarQueryAsync(
			Query             query,
			IDataContext      dataContext,
			Expression        expression,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
				return await runner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
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

		static int NonQueryQuery(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
				return runner.ExecuteNonQuery();
		}

		static async Task<object?> NonQueryQueryAsync(
			Query             query,
			IDataContext      dataContext,
			Expression        expression,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
				return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
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

		static int NonQueryQuery2(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
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

		static async Task<object?> NonQueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object?[]?        parameters,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
			{
				var n = await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (n != 0)
					return n;

				runner.QueryNumber = 1;

				return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
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

			query.GetElement      = (db, expr, ps, preambles)        => QueryQuery2(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => QueryQuery2Async(query, db, expr, ps, preambles, token);
		}

		static int QueryQuery2(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
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

		static async Task<object?> QueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object?[]?        parameters,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
			{
				var n = await runner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (n != null)
					return 0;

				runner.QueryNumber = 1;

				return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}
		}

		#endregion

		#region GetSqlText

		public static string GetSqlText(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles);
			return runner.GetSqlText();
		}

		#endregion
	}
}
