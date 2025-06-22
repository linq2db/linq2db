using System;
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

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.Logging;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metrics;

namespace LinqToDB.Internal.Linq
{
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
				var a = LinqToDB.Common.Configuration.TraceMaterializationActivity ? ActivityService.Start(ActivityID.Materialization) : null;

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

				expression = _expression.Transform(
					ctx,
					static (context, e) =>
					{
						if (e is SqlQueryRootExpression root)
						{
							if (((IConfigurationID)root.MappingSchema).ConfigurationID ==
								((IConfigurationID)context.Context.MappingSchema).ConfigurationID)
							{
								var lambda      = (LambdaExpression)context.Expression;
								var contextExpr = (Expression)Expression.PropertyOrField(lambda.Parameters[0], nameof(IQueryRunner.DataContext));

								if (contextExpr.Type != e.Type)
									contextExpr = Expression.Convert(contextExpr, e.Type);
								return contextExpr;
							}
						}

						return e;
					});

				if (slowMode)
				{
					expression = expression.Transform(
						ctx,
						static (context, e) =>
						{
							if (e is ConvertFromDataReaderExpression ex)
								return new ConvertFromDataReaderExpression(ex.Type, ex.Index, ex.Converter, ex.DataContextParam, context.NewVariable!, context.Context).Reduce();

							return ReplaceVariable(context, e);
						});
				}
				else
				{
					expression = expression.Transform(
						ctx,
						static (context, e) =>
						{
							if (e is ConvertFromDataReaderExpression ex)
								return ex.Reduce(context.Context, context.DataReader, context.NewVariable!).Transform(context, ReplaceVariable);

							return ReplaceVariable(context, e);
						});
				}

				if (LinqToDB.Common.Configuration.OptimizeForSequentialAccess)
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
				sql.Statement = query.SqlOptimizer.Finalize(query.MappingSchema, sql.Statement, query.DataOptions);
			}

			query.IsFinalized = true;
		}

		static int EvaluateTakeSkipValue(Query query, IQueryExpressions expressions, IDataContext? db, object?[]? ps, int qn, ISqlExpression sqlExpr)
		{
			var parameterValues = new SqlParameterValues();
			SetParameters(query, expressions, db, ps, qn, parameterValues);

			var evaluated = sqlExpr.EvaluateExpression(new EvaluationContext(parameterValues)) as int?;
			if (evaluated == null)
				throw new InvalidOperationException($"Cannot evaluate integer expression from '{sqlExpr}'.");
			return evaluated.Value;
		}

		internal static void SetParameters(
			Query query, IQueryExpressions expressions, IDataContext? parametersContext, object?[]? parameters, int queryNumber, SqlParameterValues parameterValues)
		{
			if (query.ParameterAccessors == null)
				return;

			foreach (var accessor in query.ParameterAccessors)
			{
				var clientValue   = accessor.ClientValueAccessor(expressions, parametersContext, parameters);
				var providerValue = clientValue;

				DbDataType? dbDataType = null;

				if (accessor.ItemAccessor != null && clientValue is IEnumerable items)
				{
					var values = new List<object?>();

					foreach (var item in items)
					{
						values.Add(accessor.ItemAccessor(item));

						if (dbDataType == null && accessor.DbDataTypeAccessor != null)
						{
							dbDataType = accessor.DbDataTypeAccessor(item);
						}
					}

					providerValue = values;
				}
				else
				{
					if (accessor.ClientToProviderConverter != null)
						providerValue = accessor.ClientToProviderConverter(clientValue); 

					if (dbDataType == null && accessor.DbDataTypeAccessor != null)
					{
						dbDataType = accessor.DbDataTypeAccessor(clientValue);
					}
				}

				if (dbDataType != null)
					dbDataType = accessor.SqlParameter.Type.WithSetValues(dbDataType.Value);
				else
					dbDataType = accessor.SqlParameter.Type;

				parameterValues.AddValue(accessor.SqlParameter, providerValue, clientValue, dbDataType.Value);
			}
		}

		internal static ParameterAccessor GetParameter(IUniqueIdGenerator<ParameterAccessor> accessorIdGenerator, Type type, IDataContext dataContext, SqlField field)
		{
			Expression clientValueGetter = Expression.Convert(
				Expression.Property(
					Expression.Convert(Expression.Property(ExpressionBuilder.QueryExpressionContainerParam, nameof(IQueryExpressions.MainExpression)), typeof(ConstantExpression)),
					ReflectionHelper.Constant.Value),
				type);

			var descriptor    = field.ColumnDescriptor;
			var dbValueLambda = descriptor.GetDbParamLambda();

			var        clientValueParameter       = Expression.Parameter(typeof(object), "clientValue");
			Expression defaultProviderValueGetter = Expression.Convert(clientValueParameter, clientValueGetter.Type);
			var        providerValueGetter        = defaultProviderValueGetter;

			providerValueGetter = InternalExtensions.ApplyLambdaToExpression(dbValueLambda, providerValueGetter);

			Expression? dbDataTypeExpression = null;
			DbDataType  dbDataType;

			if (typeof(DataParameter).IsSameOrParentOf(providerValueGetter.Type))
			{
				dbDataType           = field.ColumnDescriptor.GetDbDataType(false);
				dbDataTypeExpression = Expression.Property(providerValueGetter, Methods.LinqToDB.DataParameter.DbDataType);
				providerValueGetter  = Expression.Property(providerValueGetter, Methods.LinqToDB.DataParameter.Value);
			}
			else
			{
				dbDataType = field.ColumnDescriptor.GetDbDataType(true).WithSystemType(providerValueGetter.Type);
			}

			Func<object?, object?>? providerValueFunc = null;
			if (!ReferenceEquals(providerValueGetter, defaultProviderValueGetter))
			{
				providerValueGetter = ParametersContext.CorrectAccessorExpression(providerValueGetter, dataContext);
				if (providerValueGetter.Type != typeof(object))
					providerValueGetter = Expression.Convert(providerValueGetter, typeof(object));

				var providerValueConverter = Expression.Lambda<Func<object?, object?>>(providerValueGetter, clientValueParameter);
				providerValueFunc = providerValueConverter.CompileExpression();
			}

			Func<object?, DbDataType>? dbDataTypeFunc = null;
			if (dbDataTypeExpression != null)
			{
				dbDataTypeExpression = ParametersContext.CorrectAccessorExpression(dbDataTypeExpression, dataContext);
				var dbDataTypeLambda = Expression.Lambda<Func<object?, DbDataType>>(dbDataTypeExpression, clientValueParameter);
				dbDataTypeFunc = dbDataTypeLambda.CompileExpression();
			}

			var param = ParametersContext.CreateParameterAccessor(
				accessorIdGenerator,
				dataContext,
				clientValueGetter,
				providerValueFunc,
				itemProviderConvertFunc: null,
				dbDataType, 
				dbDataTypeFunc,
				providerValueGetter,
				parametersExpression: null,
				name: field.Name.Replace('.', '_')
			);

			return param;
		}

		static Type GetType<T>(T obj, IDataContext db)
			//=> typeof(T);
			//=> obj.GetType();
			=> db.MappingSchema.GetEntityDescriptor(typeof(T), db.Options.ConnectionOptions.OnEntityDescriptorCreated).InheritanceMapping?.Count > 0 ? obj!.GetType() : typeof(T);

		#endregion

		#region SetRunQuery

		public delegate int TakeSkipDelegate(
			Query             query,
			IQueryExpressions expressions,
			IDataContext?     dataContext,
			object?[]?        ps);

		static Func<Query,IDataContext,Mapper<T>, IQueryExpressions, object?[]?,object?[]?,int, IResultEnumerable<T>> GetExecuteQuery<T>(
				Query                                                                                                  query,
				Func<Query,IDataContext,Mapper<T>, IQueryExpressions, object?[]?,object?[]?,int, IResultEnumerable<T>> queryFunc)
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

			return queryFunc;
		}

		class BasicResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly IDataContext      _dataContext;
			readonly IQueryExpressions _expressions;
			readonly Query             _query;
			readonly object?[]?        _parameters;
			readonly object?[]?        _preambles;
			readonly int               _queryNumber;
			readonly Mapper<T>         _mapper;

			public BasicResultEnumerable(
				IDataContext      dataContext,
				IQueryExpressions expressions,
				Query             query,
				object?[]?        parameters,
				object?[]?        preambles,
				int               queryNumber,
				Mapper<T>         mapper)
			{
				_dataContext = dataContext;
				_expressions = expressions;
				_query       = query;
				_parameters  = parameters;
				_preambles   = preambles;
				_queryNumber = queryNumber;
				_mapper      = mapper;
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var _      = ActivityService.Start(ActivityID.ExecuteQuery);

				using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles);
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
					var traceMapping = LinqToDB.Common.Configuration.TraceMaterializationActivity;

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
					await using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles);
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
						var traceMapping = LinqToDB.Common.Configuration.TraceMaterializationActivity;

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
			Query             query,
			IDataContext      dataContext,
			Mapper<T>         mapper,
			IQueryExpressions expressions,
			object?[]?        ps,
			object?[]?        preambles,
			int               queryNumber
		)
		{
			return new BasicResultEnumerable<T>(dataContext, expressions, query, ps, preambles, queryNumber, mapper);
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
		static readonly PropertyInfo _expressionsInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.Expressions);
		static readonly PropertyInfo _parametersInfo  = MemberHelper.PropertyOf<IQueryRunner>(p => p.Parameters);
		static readonly PropertyInfo _preamblesInfo   = MemberHelper.PropertyOf<IQueryRunner>(p => p.Preambles);

		public static readonly PropertyInfo RowsCountInfo   = MemberHelper.PropertyOf<IQueryRunner>(p => p.RowsCount);
		public static readonly PropertyInfo DataContextInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.DataContext);

		static Expression<Func<IQueryRunner, DbDataReader, T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,IDataContext, DbDataReader, IQueryExpressions, object?[]?,object?[]?,T>> expression)
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
			SetLocal(expressionVar,  _expressionsInfo);
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
				throw new LinqToDBException("BlockExpression missing for mapper");

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
			Query<T>                                                                                              query,
			Expression<Func<IQueryRunner,IDataContext,DbDataReader,IQueryExpressions,object?[]?,object?[]?,T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Aggregation, All, Any, Contains, Count

		public static void SetRunQuery<T>(
			Query<T>                                                                                                   query,
			Expression<Func<IQueryRunner,IDataContext,DbDataReader,IQueryExpressions,object?[]?,object?[]?,object>> expression)
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
			Query             query,
			IDataContext      dataContext,
			Mapper<T>         mapper,
			IQueryExpressions expressions,
			object?[]?        ps,
			object?[]?        preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteElement);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles);
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
			IQueryExpressions expressions,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteElementAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles);
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

		static object? ScalarQuery(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteScalar);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles);
			return runner.ExecuteScalar();
		}

		static async Task<object?> ScalarQueryAsync(
			Query             query,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteScalarAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles);
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

		static int NonQueryQuery(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteNonQuery);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles);
			return runner.ExecuteNonQuery();
		}

		static async Task<object?> NonQueryQueryAsync(
			Query             query,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteNonQueryAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles);
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

		static int NonQueryQuery2(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteNonQuery2);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles);
			var       n      = runner.ExecuteNonQuery();

			if (n != 0)
				return n;

			runner.QueryNumber = 1;

			return runner.ExecuteNonQuery();
		}

		static async Task<object?> NonQueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			object?[]?        parameters,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteNonQuery2Async))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles);
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

		static int QueryQuery2(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteScalarAlternative);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles);
			var n = runner.ExecuteScalar();

			if (n != null)
				return 0;

			runner.QueryNumber = 1;

			return runner.ExecuteNonQuery();
		}

		static async Task<object?> QueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			object?[]?        parameters,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteScalarAlternativeAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles);
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

		public static IReadOnlyList<QuerySql> GetSqlText(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.GetSqlText);

			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles);
			return runner.GetSqlText();
		}

		#endregion
	}
}
