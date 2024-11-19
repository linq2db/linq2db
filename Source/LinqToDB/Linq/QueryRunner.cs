﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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
	using Interceptors;
	using LinqToDB.Expressions;
	using Reflection;
	using SqlQuery;
	using Tools;

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

			internal static MemoryCache<IStructuralEquatable,Query<T>> QueryCache { get; } = new(new());
		}

		public static class Cache<T,TR>
		{
			static Cache()
			{
				Query.CacheCleaners.Enqueue(ClearCache);
			}

			public static void ClearCache()
			{
				QueryCache.Clear();
			}

			internal static MemoryCache<IStructuralEquatable,Query<TR>> QueryCache { get; } = new(new());
		}

		#region Mapper

		sealed class Mapper<T>
		{
			public Mapper(Expression<Func<IQueryRunner, DbDataReader, T>> mapperExpression)
			{
				_expression = mapperExpression;
			}

			readonly Expression<Func<IQueryRunner,DbDataReader,T>> _expression;
			readonly ConcurrentDictionary<Type,ReaderMapperInfo>   _mappers = new ();

			public sealed class ReaderMapperInfo
			{
				public Expression<Func<IQueryRunner,DbDataReader,T>> MapperExpression = null!;
				public Func<IQueryRunner,DbDataReader,T>             Mapper = null!;
				public bool                                          IsFaulted;
			}

			public T Map(IDataContext context, IQueryRunner queryRunner, DbDataReader dataReader, ref ReaderMapperInfo mapperInfo)
			{
				var a = Configuration.TraceMaterializationActivity ? ActivityService.Start(ActivityID.Materialization) : null;

				try
				{
					return mapperInfo.Mapper(queryRunner, dataReader);
				}
				// SqlNullValueException: MySqlData
				// OracleNullValueException: managed and native oracle providers
				catch (Exception ex) when (ex is FormatException or InvalidCastException or LinqToDBConvertException || ex.GetType().Name.Contains("NullValueException"))
				{
					// TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
					if (mapperInfo.IsFaulted)
						throw;

					return ReMapOnException(context, queryRunner, dataReader, ref mapperInfo, ex);
				}
				finally
				{
					a?.Dispose();
				}
			}

			public T ReMapOnException(IDataContext context, IQueryRunner queryRunner, DbDataReader dataReader, ref ReaderMapperInfo mapperInfo, Exception ex)
			{
				if (context.GetTraceSwitch().TraceInfo)
					context.WriteTraceLine(
						$"Mapper has switched to slow mode. Mapping exception: {ex.Message}",
						context.GetTraceSwitch().DisplayName,
						TraceLevel.Error);

				queryRunner.MapperExpression = mapperInfo.MapperExpression;

				var dataReaderType = dataReader.GetType();
				var expression     = TransformMapperExpression(context, dataReader, dataReaderType, true);
				var expr           = mapperInfo.MapperExpression; // create new instance to avoid race conditions without locks

				mapperInfo = new ReaderMapperInfo()
				{
					MapperExpression = expr,
					Mapper           = expression.CompileExpression(),
					IsFaulted        = true
				};

				_mappers[dataReaderType] = mapperInfo;

				return mapperInfo.Mapper(queryRunner, dataReader);
			}

			public ReaderMapperInfo GetMapperInfo(IDataContext context, IQueryRunner queryRunner, DbDataReader dataReader)
			{
				var dataReaderType = dataReader.GetType();

				if (!_mappers.TryGetValue(dataReaderType, out var mapperInfo))
				{
					var mapperExpression = TransformMapperExpression(context, dataReader, dataReaderType, false);

					queryRunner.MapperExpression = mapperExpression;

					var mapper = mapperExpression.CompileExpression();

					mapperInfo = new() { MapperExpression = mapperExpression, Mapper = mapper };

					_mappers.TryAdd(dataReaderType, mapperInfo);
				}

				return mapperInfo;
			}

			// transform extracted to separate method to avoid closures allocation on mapper cache hit
			private Expression<Func<IQueryRunner, DbDataReader, T>> TransformMapperExpression(
				IDataContext context,
				DbDataReader dataReader,
				Type         dataReaderType,
				bool         slowMode)
			{
				var ctx = new TransformMapperExpressionContext(_expression, context, dataReader, dataReaderType);

				Expression expression;

				if (slowMode)
				{
					expression = _expression.Transform(
						ctx,
						static (context, e) =>
						{
							if (e is ConvertFromDataReaderExpression ex)
								return new ConvertFromDataReaderExpression(ex.Type, ex.Index, ex.Converter, context.NewVariable!, context.Context).Reduce();

							return ReplaceVariable(context, e);
						});
				}
				else
				{
					expression = _expression.Transform(
						ctx,
						static (context, e) =>
						{
							if (e is ConvertFromDataReaderExpression ex)
								return ex.Reduce(context.Context, context.DataReader, context.NewVariable!).Transform(context, ReplaceVariable);

							return ReplaceVariable(context, e);
						});
				}

				if (Configuration.OptimizeForSequentialAccess)
					expression = SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(expression, dataReader.FieldCount, reduce: false);

				return (Expression<Func<IQueryRunner, DbDataReader, T>>)expression;
			}

			static Expression ReplaceVariable(TransformMapperExpressionContext context, Expression e)
			{
				if (e is ParameterExpression { Name: "ldr" } vex)
				{
					context.OldVariable = vex;
					return context.NewVariable ??= Expression.Variable(context.DataReader.GetType(), "ldr");
				}

				if (e is BinaryExpression { NodeType: ExpressionType.Assign } bex && bex.Left == context.OldVariable)
				{
					var dataReaderExpression = Expression.Convert(context.Expression.Parameters[1], context.DataReaderType);

					return Expression.Assign(context.NewVariable!, dataReaderExpression);
				}

				return e;
			}

			sealed class TransformMapperExpressionContext
			{
				public TransformMapperExpressionContext(Expression<Func<IQueryRunner, DbDataReader, T>> expression, IDataContext context, DbDataReader dataReader, Type dataReaderType)
				{
					Expression     = expression;
					Context        = context;
					DataReader     = dataReader;
					DataReaderType = dataReaderType;
				}

				public Expression<Func<IQueryRunner,DbDataReader,T>> Expression;
				public readonly IDataContext                         Context;
				public readonly DbDataReader                         DataReader;
				public readonly Type                                 DataReaderType;

				public ParameterExpression? OldVariable;
				public ParameterExpression? NewVariable;
			}
		}

		#endregion

		#region Helpers

		static void FinalizeQuery(Query query)
		{
			if (query.IsFinalized)
				return;

			using var m = ActivityService.Start(ActivityID.FinalizeQuery);

			foreach (var sql in query.Queries)
			{
				sql.Statement   = query.SqlOptimizer.Finalize(query.MappingSchema, sql.Statement, query.DataOptions);
			}

			query.IsFinalized = true;
		}

		static int EvaluateTakeSkipValue(Query query, Expression expr, IDataContext? db, object?[]? ps, int qn, ISqlExpression sqlExpr)
		{
			var parameterValues = new SqlParameterValues();
			SetParameters(query, expr, db, ps, qn, parameterValues);

			var evaluated = sqlExpr.EvaluateExpression(new EvaluationContext(parameterValues)) as int?;
			if (evaluated == null)
				throw new InvalidOperationException($"Cannot evaluate integer expression from '{sqlExpr}'.");
			return evaluated.Value;
		}

		internal static void SetParameters(
			Query query, Expression expression, IDataContext? parametersContext, object?[]? parameters, int queryNumber, SqlParameterValues parameterValues)
		{
			var queryContext = query.Queries[queryNumber];

			foreach (var p in queryContext.ParameterAccessors)
			{
				var providerValue = p.ValueAccessor(expression, parametersContext, parameters);

				DbDataType? dbDataType = null;

				if (providerValue is IEnumerable items && p.ItemAccessor != null)
				{
					var values = new List<object?>();

					foreach (var item in items)
					{
						values.Add(p.ItemAccessor(item));
						dbDataType ??= p.DbDataTypeAccessor(expression, item, parametersContext, parameters);
					}

					providerValue = values;
				}

				dbDataType ??= p.DbDataTypeAccessor(expression, null, parametersContext, parameters);

				parameterValues.AddValue(p.SqlParameter, providerValue, p.SqlParameter.Type.WithSetValues(dbDataType.Value));
			}
		}

		internal static ParameterAccessor GetParameter(Type type, IDataContext dataContext, SqlField field)
		{
			Expression getter = Expression.Convert(
				Expression.Property(
					Expression.Convert(ExpressionBuilder.ExpressionParam, typeof(ConstantExpression)),
					ReflectionHelper.Constant.Value),
				type);

			var descriptor    = field.ColumnDescriptor;
			var dbValueLambda = descriptor.GetDbParamLambda();

			Expression? dbDataTypeExpression;

			var valueGetter = InternalExtensions.ApplyLambdaToExpression(dbValueLambda, getter);

			if (typeof(DataParameter).IsSameOrParentOf(valueGetter.Type))
			{
				dbDataTypeExpression = Expression.Call(Expression.Constant(field.ColumnDescriptor.GetDbDataType(false)),
					DbDataType.WithSetValuesMethodInfo,
					Expression.Property(valueGetter, Methods.LinqToDB.DataParameter.DbDataType));
				valueGetter          = Expression.Property(valueGetter, Methods.LinqToDB.DataParameter.Value);
			}
			else
			{
				var dbDataType       = field.ColumnDescriptor.GetDbDataType(true).WithSystemType(valueGetter.Type);
				dbDataTypeExpression = Expression.Constant(dbDataType);
			}

			var param = ParametersContext.CreateParameterAccessor(
				dataContext, valueGetter, null, dbDataTypeExpression, valueGetter, parametersExpression: null, name: field.Name.Replace('.', '_'));

			return param;
		}

		static Type GetType<T>(T obj, IDataContext db)
			//=> typeof(T);
			//=> obj.GetType();
			=> db.MappingSchema.GetEntityDescriptor(typeof(T), db.Options.ConnectionOptions.OnEntityDescriptorCreated).InheritanceMapping?.Count > 0 ? obj!.GetType() : typeof(T);

		#endregion

		#region SetRunQuery

		public delegate int TakeSkipDelegate(
			Query         query,
			Expression    expression,
			IDataContext? dataContext,
			object?[]?    ps);

		static Func<Query,IDataContext,Mapper<T>,Expression,object?[]?,object?[]?,int, IResultEnumerable<T>> GetExecuteQuery<T>(
				Query                                                                                         query,
				Func<Query,IDataContext,Mapper<T>,Expression,object?[]?,object?[]?,int, IResultEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			var selectQuery = query.Queries[0].Statement.SelectQuery!;
			var select      = selectQuery.Select;

			if (select.SkipValue != null && !query.SqlProviderFlags.GetIsSkipSupportedFlag(select.TakeValue, select.SkipValue))
			{
				var newTakeValue = select.SkipValue;
				if (select.TakeValue != null)
				{
					newTakeValue = new SqlBinaryExpression(typeof(int), newTakeValue, "+", select.TakeValue);
				}
				else
				{
					newTakeValue = null;
				}

				var skipValue = select.SkipValue;

				select.TakeValue = newTakeValue;
				select.SkipValue = null;

				var q = queryFunc;

				queryFunc = (qq, db, mapper, expr, ps, preambles, qn) =>
					new LimitResultEnumerable<T>(q(qq, db, mapper, expr, ps, preambles, qn),
						EvaluateTakeSkipValue(qq, expr, db, ps, qn, skipValue), null);
			}

			if (select.TakeValue != null && !query.SqlProviderFlags.IsTakeSupported)
			{
				var takeValue = select.TakeValue;

				var q = queryFunc;

				queryFunc = (qq, db, mapper, expr, ps, preambles, qn) =>
					new LimitResultEnumerable<T>(q(qq, db, mapper, expr, ps, preambles, qn),
						null, EvaluateTakeSkipValue(qq, expr, db, ps, qn, takeValue));
			}

			return queryFunc;
		}

		class BasicResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly IDataContext      _dataContext;
			readonly Expression        _expression;
			readonly Query             _query;
			readonly object?[]?        _parameters;
			readonly object?[]?        _preambles;
			readonly int               _queryNumber;
			readonly Mapper<T>         _mapper;

			public BasicResultEnumerable(
				IDataContext      dataContext,
				Expression        expression,
				Query             query,
				object?[]?        parameters,
				object?[]?        preambles,
				int               queryNumber,
				Mapper<T> mapper)
			{
				_dataContext = dataContext;
				_expression  = expression;
				_query       = query;
				_parameters  = parameters;
				_preambles   = preambles;
				_queryNumber = queryNumber;
				_mapper      = mapper;
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var _      = ActivityService.Start(ActivityID.ExecuteQuery);

				using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expression, _parameters, _preambles);
				using var dr     = runner.ExecuteReader();

				var dataReader = dr.DataReader!;

				if (dataReader.Read())
				{
					DbDataReader origDataReader;

					if (_dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
					{
						using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
							origDataReader = interceptor.UnwrapDataReader(_dataContext, dataReader);
					}
					else
					{
						origDataReader = dataReader;
					}

					var mapperInfo   = _mapper.GetMapperInfo(_dataContext, runner, origDataReader);
					var traceMapping = Configuration.TraceMaterializationActivity;

					do
					{
						T res;
						var a = traceMapping ? ActivityService.Start(ActivityID.Materialization) : null;

						try
						{
							res = mapperInfo.Mapper(runner, origDataReader);
							runner.RowsCount++;
						}
						catch (Exception ex) when (ex is FormatException or InvalidCastException or LinqToDBConvertException || ex.GetType().Name.Contains("NullValueException"))
						{
							// TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
							if (mapperInfo.IsFaulted)
								throw;

							res = _mapper.ReMapOnException(_dataContext, runner, origDataReader, ref mapperInfo, ex);
							runner.RowsCount++;
						}
						finally
						{
							a?.Dispose();
						}

						yield return res;
					}
					while (dataReader.Read());
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public async IAsyncEnumerable<T> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteQueryAsync))
				{
#pragma warning disable CA2007
					await using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expression, _parameters, _preambles);
					await using var dr     = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
#pragma warning restore CA2007

					var dataReader = dr.DataReader!;

					cancellationToken.ThrowIfCancellationRequested();

					if (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
					{
						DbDataReader origDataReader;

						if (_dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
						{
							using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
								origDataReader = interceptor.UnwrapDataReader(_dataContext, dr.DataReader);
						}
						else
						{
							origDataReader = dr.DataReader;
						}

						var mapperInfo   = _mapper.GetMapperInfo(_dataContext, runner, origDataReader);
						var traceMapping = Configuration.TraceMaterializationActivity;

						do
						{
							T res;
							var a = traceMapping ? ActivityService.Start(ActivityID.Materialization) : null;

							try
							{
								res = mapperInfo.Mapper(runner, origDataReader);
								runner.RowsCount++;
							}
							catch (Exception ex) when (ex is FormatException or InvalidCastException or LinqToDBConvertException || ex.GetType().Name.Contains("NullValueException"))
							{
								// TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
								if (mapperInfo.IsFaulted)
									throw;

								res = _mapper.ReMapOnException(_dataContext, runner, origDataReader, ref mapperInfo, ex);
								runner.RowsCount++;
							}
							finally
							{
								a?.Dispose();
							}

							yield return res;
							cancellationToken.ThrowIfCancellationRequested();
						}
						while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false));
					}
				}
			}

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
			{
				return GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken);
			}
		}

		static IResultEnumerable<T> ExecuteQuery<T>(
			Query        query,
			IDataContext dataContext,
			Mapper<T>    mapper,
			Expression   expression,
			object?[]?   ps,
			object?[]?   preambles,
			int          queryNumber
		)
		{
			return new BasicResultEnumerable<T>(dataContext, expression, query, ps, preambles, queryNumber, mapper);
		}

		sealed class AsyncEnumeratorImpl<T> : IAsyncEnumerator<T>
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
			int               _take;

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

			public async ValueTask<bool> MoveNextAsync()
			{
				if (_queryRunner == null)
				{
					_queryRunner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expression, _ps, _preambles);
					_dataReader  = await _queryRunner.ExecuteReaderAsync(_cancellationToken).ConfigureAwait(false);

					var skip = _skipAction?.Invoke(_query, _expression, _dataContext, _ps) ?? 0;

					while (skip-- > 0)
					{
						if (!await _dataReader.ReadAsync(_cancellationToken).ConfigureAwait(false))
							return false;
					}

					_take = _takeAction?.Invoke(_query, _expression, _dataContext, _ps) ?? int.MaxValue;
				}

				if (_take-- > 0 && await _dataReader!.ReadAsync(_cancellationToken).ConfigureAwait(false))
				{
					DbDataReader dataReader;

					if (_dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
					{
						using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
							dataReader = interceptor.UnwrapDataReader(_dataContext, _dataReader.DataReader);
					}
					else
					{
						dataReader = _dataReader.DataReader;
					}

					var mapperInfo = _mapper.GetMapperInfo(_dataContext, _queryRunner, dataReader);

					Current = _mapper.Map(_dataContext, _queryRunner, dataReader, ref mapperInfo);

					_queryRunner.RowsCount++;

					return true;
				}

				return false;
			}

			public void Dispose()
			{
				_dataReader ?.Dispose();
				_queryRunner?.Dispose();

				_queryRunner = null;
				_dataReader  = null;
			}

			public async ValueTask DisposeAsync()
			{
				if (_dataReader != null)
					await _dataReader.DisposeAsync().ConfigureAwait(false);

				if (_queryRunner != null)
					await _queryRunner.DisposeAsync().ConfigureAwait(false);

				_queryRunner = null;
				_dataReader  = null;
			}
		}

		public static void WrapRunQuery<TSource, TResult>(
			Query<TSource>                                               query,
			Query<TResult>                                               destQuery,
			Func<IResultEnumerable<TSource>, IResultEnumerable<TResult>> wrapper)
		{
			var executeQuery = query.GetResultEnumerable;
			destQuery.GetResultEnumerable = (db, expr, ps, preambles) => wrapper(executeQuery(db, expr, ps, preambles));
		}

		static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner, DbDataReader, T>> expression)
		{
			var executeQuery = GetExecuteQuery<T>(query, ExecuteQuery);

			var mapper   = new Mapper<T>(expression);

			query.GetResultEnumerable = (db, expr, ps, preambles) =>
			{
				using var _ = ActivityService.Start(ActivityID.GetIEnumerable);
				return executeQuery(query, db, mapper, expr, ps, preambles, 0);
			};
		}

		static readonly PropertyInfo _dataContextInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.DataContext);
		static readonly PropertyInfo _expressionInfo  = MemberHelper.PropertyOf<IQueryRunner>(p => p.Expression);
		static readonly PropertyInfo _parametersInfo  = MemberHelper.PropertyOf<IQueryRunner>(p => p.Parameters);
		static readonly PropertyInfo _preamblesInfo   = MemberHelper.PropertyOf<IQueryRunner>(p => p.Preambles);

		public static readonly PropertyInfo RowsCountInfo   = MemberHelper.PropertyOf<IQueryRunner>(p => p.RowsCount);

		static Expression<Func<IQueryRunner, DbDataReader, T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,IDataContext, DbDataReader, Expression,object?[]?,object?[]?,T>> expression)
		{
			var queryRunnerParam = expression.Parameters[0];
			var dataReaderParam  = expression.Parameters[2];

			var dataContextVar   = expression.Parameters[1];
			var expressionVar    = expression.Parameters[3];
			var parametersVar    = expression.Parameters[4];
			var preamblesVar     = expression.Parameters[5];

			var locals = new List<ParameterExpression>();
			var exprs  = new List<Expression>();

			SetLocal(dataContextVar, _dataContextInfo);
			SetLocal(expressionVar,  _expressionInfo);
			SetLocal(parametersVar,  _parametersInfo);
			SetLocal(preamblesVar,   _preamblesInfo);

			void SetLocal(ParameterExpression local, PropertyInfo prop)
			{
				if (expression.Body.Find(local) != null)
				{
					locals.Add(local);
					exprs. Add(Expression.Assign(local, Expression.Property(queryRunnerParam, prop)));
				}
			}

			// we can safely assume it is block expression
			if (expression.Body is not BlockExpression block)
				throw new LinqException("BlockExpression missing for mapper");

			return
				Expression.Lambda<Func<IQueryRunner, DbDataReader, T>>(
					block.Update(
						locals.Concat(block.Variables),
						exprs.Concat(block.Expressions)),
					queryRunnerParam,
					dataReaderParam);
		}

		#endregion

		#region SetRunQuery / Cast, Concat, Union, OfType, ScalarSelect, Select, SequenceContext, Table

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext, DbDataReader, Expression,object?[]?,object?[]?,T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Aggregation, All, Any, Contains, Count

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext, DbDataReader, Expression,object?[]?,object?[]?,object>> expression)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

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
			using var m      = ActivityService.Start(ActivityID.ExecuteElement);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expression, ps, preambles);
			using var dr     = runner.ExecuteReader();

			DbDataReader dataReader;

			if (dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
			{
				using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
					dataReader = interceptor.UnwrapDataReader(dataContext, dr.DataReader!);
			}
			else
			{
				dataReader = dr.DataReader!;
			}

			var mapperInfo = mapper.GetMapperInfo(dataContext, runner, dataReader);

			if (dr.DataReader!.Read())
			{
				var ret = mapper.Map(dataContext, runner, dataReader, ref mapperInfo);
				runner.RowsCount++;
				return ret;
			}

			return Array.Empty<T>().First();
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
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteElementAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expression, ps, preambles);
				await using (runner.ConfigureAwait(false))
				{
					var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
					await using (dr.ConfigureAwait(false))
					{
						if (await dr.ReadAsync(cancellationToken).ConfigureAwait(false))
						{
							DbDataReader dataReader;

							if (dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
							{
								using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
									dataReader = interceptor.UnwrapDataReader(dataContext, dr.DataReader);
							}
							else
							{
								dataReader = dr.DataReader;
							}

							var mapperInfo = mapper.GetMapperInfo(dataContext, runner, dataReader);
							var item       = mapper.Map(dataContext, runner, dataReader, ref mapperInfo);

							var ret = dataContext.MappingSchema.ChangeTypeTo<T>(item);
							runner.RowsCount++;
							return ret;
						}

						return Array.Empty<T>().First();
					}
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

			query.GetElement      = (db, expr, ps, preambles) => ScalarQuery(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => ScalarQueryAsync(query, db, expr, ps, preambles, token);
		}

		static object? ScalarQuery(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteScalar);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expr, parameters, preambles);
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
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteScalarAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expression, ps, preambles);
				await using (runner.ConfigureAwait(false))
					return await runner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		#endregion

		#region NonQueryQuery

		public static void SetNonQueryQuery(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			query.GetElement      = (db, expr, ps, preambles) => NonQueryQuery(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => NonQueryQueryAsync(query, db, expr, ps, preambles, token);
		}

		static int NonQueryQuery(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteNonQuery);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expr, parameters, preambles);
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
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteNonQueryAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expression, ps, preambles);
				await using (runner.ConfigureAwait(false))
					return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		#endregion

		#region NonQueryQuery2

		public static void SetNonQueryQuery2(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 2)
				throw new InvalidOperationException();

			query.GetElement      = (db, expr, ps, preambles)        => NonQueryQuery2(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => NonQueryQuery2Async(query, db, expr, ps, preambles, token);
		}

		static int NonQueryQuery2(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteNonQuery2);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expr, parameters, preambles);
			var n = runner.ExecuteNonQuery();

			if (n != 0)
				return n;

			runner.QueryNumber = 1;

			return runner.ExecuteNonQuery();
		}

		static async Task<object?> NonQueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object?[]?        parameters,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteNonQuery2Async))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expr, parameters, preambles);
				await using (runner.ConfigureAwait(false))
				{
					var n = await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

					if (n != 0)
						return n;

					runner.QueryNumber = 1;

					return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
				}
			}
		}

		#endregion

		#region QueryQuery2

		public static void SetQueryQuery2(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 2)
				throw new InvalidOperationException();

			query.GetElement      = (db, expr, ps, preambles)        => QueryQuery2(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => QueryQuery2Async(query, db, expr, ps, preambles, token);
		}

		static int QueryQuery2(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteScalarAlternative);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expr, parameters, preambles);
			var n = runner.ExecuteScalar();

			if (n != null)
				return 0;

			runner.QueryNumber = 1;

			return runner.ExecuteNonQuery();
		}

		static async Task<object?> QueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object?[]?        parameters,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteScalarAlternativeAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expr, parameters, preambles);
				await using (runner.ConfigureAwait(false))
				{
					var n = await runner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

					if (n != null)
						return 0;

					runner.QueryNumber = 1;

					return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
				}
			}
		}

		#endregion

		#region GetSqlText

		public static string GetSqlText(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.GetSqlText);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expr, parameters, preambles);
			return runner.GetSqlText();
		}

		#endregion
	}
}
