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
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
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
				catch (Exception ex) when (ex is FormatException or InvalidCastException or LinqToDBConvertException || ex.GetType().Name.Contains("NullValueException", StringComparison.Ordinal))
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
					IsFaulted        = true,
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

			static readonly ObjectPool<MapperExpressionTransformer> _mapperExpressionTransformerPool = new(() => new MapperExpressionTransformer(), v => v.Cleanup(), 100);

			sealed class MapperExpressionTransformer : ExpressionVisitorBase
			{
				private bool                 _slowMode;
				private LambdaExpression     _originalMapper = default!;
				private IDataContext         _context        = default!;
				private DbDataReader         _dataReader     = default!;
				private Type                 _dataReaderType = default!;
				private ParameterExpression? _oldVariable;
				private ParameterExpression? _newVariable;

				public override void Cleanup()
				{
					_slowMode       = false;
					_originalMapper = default!;
					_context        = default!;
					_dataReader     = default!;
					_dataReaderType = default!;
					_oldVariable    = null;
					_newVariable    = null;

					base.Cleanup();
				}

				public Expression Transform(
					IDataContext     context,
					DbDataReader     dataReader,
					Type             dataReaderType,
					bool             slowMode,
					LambdaExpression mapper)
				{
					_slowMode       = slowMode;
					_originalMapper = mapper;
					_context        = context;
					_dataReader     = dataReader;
					_dataReaderType = dataReaderType;

					return Visit(mapper);
				}

				public override Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
				{
					if (((IConfigurationID)node.MappingSchema).ConfigurationID == ((IConfigurationID)_context.MappingSchema).ConfigurationID)
					{
						var contextExpr = (Expression)Expression.PropertyOrField(_originalMapper.Parameters[0], nameof(IQueryRunner.DataContext));

						if (contextExpr.Type != node.Type)
							contextExpr = Expression.Convert(contextExpr, node.Type);

						return contextExpr;
					}

					return node;
				}

				internal override Expression VisitConvertFromDataReaderExpression(ConvertFromDataReaderExpression node)
				{
					if (_slowMode)
						return Visit(new ConvertFromDataReaderExpression(node.Type, node.Index, node.Converter, node.DataContextParam, _newVariable!, _context).Reduce());
					else
						return Visit(node.Reduce(_context, _dataReader, _newVariable!));
				}

				protected override Expression VisitParameter(ParameterExpression node)
				{
					if (_oldVariable == null && string.Equals(node.Name, "ldr", StringComparison.Ordinal))
					{
						_oldVariable = node;
						_newVariable = Expression.Variable(_dataReader.GetType(), "ldr");
					}

					if (node == _oldVariable)
						return _newVariable!;

					return node;
				}

				protected override Expression VisitBinary(BinaryExpression node)
				{
					var left = Visit(node.Left);
					Expression? right = null;

					if (node.NodeType == ExpressionType.Assign && node.Left == _oldVariable)
					{
						right = Expression.Convert(_originalMapper.Parameters[1], _dataReaderType);
					}

					return node.Update(
						left,
						VisitAndConvert(node.Conversion, nameof(VisitBinary)),
						right ?? Visit(node.Right));
				}
			}

			// transform extracted to separate method to avoid closures allocation on mapper cache hit
			private Expression<Func<IQueryRunner, DbDataReader, T>> TransformMapperExpression(
				IDataContext context,
				DbDataReader dataReader,
				Type         dataReaderType,
				bool         slowMode)
			{
				using var transformer = _mapperExpressionTransformerPool.Allocate();
				var expression = transformer.Value.Transform(context, dataReader, dataReaderType, slowMode, _expression);

				if (context.Options.LinqOptions.OptimizeForSequentialAccess)
					expression = SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(expression, dataReader.FieldCount, reduce: false);

				return (Expression<Func<IQueryRunner, DbDataReader, T>>)expression;
			}
		}

		#endregion

		#region Helpers

		static void FinalizeQuery(Query query)
		{
			if (query.IsFinalized)
				return;

			using var m = ActivityService.Start(ActivityID.FinalizeQuery);

			var sql = query.QueryInfo;
			sql.Statement = query.SqlOptimizer.Finalize(query.MappingSchema, sql.Statement, query.DataOptions);

			query.IsFinalized = true;
		}

		static int EvaluateTakeSkipValue(Query query, IQueryExpressions expressions, IDataContext? db, object?[]? ps, ISqlExpression sqlExpr)
		{
			var parameterValues = new SqlParameterValues();
			SetParameters(query, expressions, db, ps, parameterValues);

			var evaluated = sqlExpr.EvaluateExpression(new EvaluationContext(parameterValues)) as int?;
			if (evaluated == null)
				throw new InvalidOperationException($"Cannot evaluate integer expression from '{sqlExpr}'.");
			return evaluated.Value;
		}

		internal static void SetParameters(
			Query query, IQueryExpressions expressions, IDataContext? parametersContext, object?[]? parameters, SqlParameterValues parameterValues)
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

		static Func<Query,IDataContext,Mapper<T>, IQueryExpressions, object?[]?,SqlCommandExecutionContext?,int, IResultEnumerable<T>> GetExecuteQuery<T>(
				Query                                                                                                  query,
				Func<Query,IDataContext,Mapper<T>, IQueryExpressions, object?[]?,SqlCommandExecutionContext?,int, IResultEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			var selectQuery = query.QueryInfo.Statement.SelectQuery!;
			var select      = selectQuery.Select;

			if (select.SkipValue != null && !query.SqlProviderFlags.GetIsSkipSupportedFlag(select.TakeValue))
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
						EvaluateTakeSkipValue(qq, expr, db, ps, skipValue), null);
			}

			return queryFunc;
		}

		sealed class BasicResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly IDataContext      _dataContext;
			readonly IQueryExpressions _expressions;
			readonly Query             _query;
			readonly object?[]?        _parameters;
			readonly SqlCommandExecutionContext? _preambles;
			readonly int               _queryNumber;
			readonly Mapper<T>         _mapper;

			public BasicResultEnumerable(
				IDataContext      dataContext,
				IQueryExpressions expressions,
				Query             query,
				object?[]?        parameters,
				SqlCommandExecutionContext? preambles,
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

				using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles?.Results);
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
						catch (Exception ex) when (ex is FormatException or InvalidCastException or LinqToDBConvertException || ex.GetType().Name.Contains("NullValueException", StringComparison.Ordinal))
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
					var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles?.Results);
					await using var _2 = runner.ConfigureAwait(false);

					var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
					await using var _3 = dr.ConfigureAwait(false);

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
							catch (Exception ex) when (ex is FormatException or InvalidCastException or LinqToDBConvertException || ex.GetType().Name.Contains("NullValueException", StringComparison.Ordinal))
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

		// Materializes a query's rows from an externally-opened reader already positioned at its result set (the caller
		// owns the reader lifetime and advances it with NextResult after enumeration). Mirrors BasicResultEnumerable but
		// does NOT open its own reader — used by combined multi-result-set eager loading, where N child queries run as
		// one command and each result set is mapped by its own query's mapper. The runner is created only to give the
		// mapper its context (DataContext / parameters / RowsCount); its command is never executed.
		sealed class ExternalReaderResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly IDataContext      _dataContext;
			readonly IQueryExpressions _expressions;
			readonly Query             _query;
			readonly object?[]?        _parameters;
			readonly SqlCommandExecutionContext? _preambles;
			readonly int               _queryNumber;
			readonly Mapper<T>         _mapper;
			readonly DbDataReader      _dataReader;

			public ExternalReaderResultEnumerable(
				IDataContext      dataContext,
				IQueryExpressions expressions,
				Query             query,
				object?[]?        parameters,
				SqlCommandExecutionContext? preambles,
				int               queryNumber,
				Mapper<T>         mapper,
				DbDataReader      dataReader)
			{
				_dataContext = dataContext;
				_expressions = expressions;
				_query       = query;
				_parameters  = parameters;
				_preambles   = preambles;
				_queryNumber = queryNumber;
				_mapper      = mapper;
				_dataReader  = dataReader;
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles?.Results);

				var dataReader = _dataReader;

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

					var mapperInfo = _mapper.GetMapperInfo(_dataContext, runner, origDataReader);

					do
					{
						var res = _mapper.Map(_dataContext, runner, origDataReader, ref mapperInfo);
						runner.RowsCount++;
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
				var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles?.Results);
				await using var _2 = runner.ConfigureAwait(false);

				var dataReader = _dataReader;

				cancellationToken.ThrowIfCancellationRequested();

				if (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
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

					var mapperInfo = _mapper.GetMapperInfo(_dataContext, runner, origDataReader);

					do
					{
						var res = _mapper.Map(_dataContext, runner, origDataReader, ref mapperInfo);
						runner.RowsCount++;
						yield return res;
						cancellationToken.ThrowIfCancellationRequested();
					}
					while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false));
				}
			}

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
			{
				return GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken);
			}
		}

		// Runs the main query together with its eager-load child collections, collapsing the combinable children (Default
		// strategy, IStepMaterializer) and the main into a size-bounded set of multi-result-set commands (N+1 -> 1, a few for
		// a very large fan-out). Non-combinable preambles (KeyedQuery / CteUnion / detached / buffer / no-op) run sequentially
		// FIRST (in index order; they may depend on each other), then the combinable children + main are modelled as a
		// SqlCommandScenario of Reader steps and grouped by PlanScenario. Each combinable child result set is buffered into
		// its PreambleResult; the main result set (always the last step of the last group) streams lazily from that group's
		// reader, which this enumerable owns. Created only when the provider supports multi-statement batches with multiple
		// result sets and at least one preamble is combinable (see TryGetCombinedEagerEnumerable); a purely non-combinable
		// load falls back to the sequential InitPreambles path.
		sealed class EagerResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly IDataContext      _dataContext;
			readonly IQueryExpressions _expressions;
			readonly Query<T>          _query;
			readonly object?[]?        _parameters;
			readonly Preamble[]        _preambles;
			// Preamble indices that are reader-combinable, in order. Scenario step k (< Length) maps back to preamble
			// _combinableIndexes[k]; scenario step Length is the main query.
			readonly int[]             _combinableIndexes;
			readonly int[]             _nonCombinableIndexes;

			public EagerResultEnumerable(
				IDataContext      dataContext,
				IQueryExpressions expressions,
				Query<T>          query,
				object?[]?        parameters,
				Preamble[]        preambles)
			{
				_dataContext = dataContext;
				_expressions = expressions;
				_query       = query;
				_parameters  = parameters;
				_preambles   = preambles;

				var combinable    = new List<int>(preambles.Length);
				var nonCombinable = new List<int>(preambles.Length);

				for (var i = 0; i < preambles.Length; i++)
				{
					if (IsCombinable(preambles[i]))
						combinable.Add(i);
					else
						nonCombinable.Add(i);
				}

				_combinableIndexes    = combinable.ToArray();
				_nonCombinableIndexes = nonCombinable.ToArray();
			}

			static bool IsCombinable(Preamble preamble)
				=> preamble is IStepMaterializer { CanCombine: true } materializer && materializer.GetCombinableStatement() != null;

			// Whether any preamble is reader-combinable — the single definition of the combinable predicate, shared by the
			// ctor's partition and TryGetCombinedEagerEnumerable's gate.
			internal static bool HasCombinable(Preamble[] preambles)
			{
				foreach (var preamble in preambles)
					if (IsCombinable(preamble))
						return true;

				return false;
			}

			// Models the combinable children + main as a scenario of Reader steps [child1 .. childN, main] and merges every
			// child's and the main's parameter values into one SqlParameterValues (keyed by SqlParameter node). The main is the
			// last step, so PlanScenario's contiguous grouping keeps it at the end of the last group. Scenario step k (< count)
			// maps to preamble _combinableIndexes[k]; step count is the main query.
			(SqlCommandScenario Scenario, SqlParameterValues Values) PrepareScenario()
			{
				var count  = _combinableIndexes.Length;
				var steps  = new SqlCommandStep[count + 1];
				var values = new SqlParameterValues();

				for (var k = 0; k < count; k++)
				{
					var materializer = (IStepMaterializer)_preambles[_combinableIndexes[k]];

					steps[k] = new SqlCommandStep { Statement = materializer.GetCombinableStatement()!, Kind = SqlStepKind.Reader };
					materializer.AddCombinableParameterValues(values, _expressions, _dataContext, _parameters);
				}

				steps[count] = new SqlCommandStep { Statement = _query.QueryInfo.Statement, Kind = SqlStepKind.Reader };
				SetParameters(_query, _expressions, _dataContext, _parameters, values);

				return (new SqlCommandScenario { Steps = steps, OutcomeSteps = [] }, values);
			}

			// Plans the scenario into size-bounded commands: PlanScenario groups the steps (bounded by statement count), then
			// each group's statements render into one or more commands bounded by SQL length. Each returned command carries the
			// scenario step indices it covers, so the executor knows which child result sets to buffer and where the main result
			// set lands (always the last step of the last command).
			List<CombinedCommand> BuildCommands(SqlCommandScenario scenario, SqlParameterValues values)
			{
				var dataConnection = (DataConnection)_dataContext;
				var useBatch       = dataConnection.CanUseDbBatch;   // false on frameworks without the DbBatch API

				// Fast path: reuse the per-command cache built for THIS backend and bind this run's DbParameters, re-rendering
				// only the parameter-dependent (null) batch slots. Skipped when the cache was built for the other backend
				// (batch and concat shapes are not interchangeable) or is absent (concat with a volatile step).
				if (_query.QueryInfo.EagerCommandCache is { } cache && cache.WasBatch == useBatch)
				{
					var bound = new List<CombinedCommand>(cache.Commands.Length);

					foreach (var command in cache.Commands)
						bound.Add(BindCommand(dataConnection, command, useBatch, scenario, values));

					return bound;
				}

				// Per-step parameter-dependence, computed once for this render pass: a volatile step's SQL varies with
				// parameter values, so its template can't be cached (a stable main is cached while a Contains/LIKE child re-renders).
				var volatility  = ComputeStepVolatility(dataConnection, scenario);
				var anyVolatile = false;

				foreach (var v in volatility)
					if (v)
					{
						anyVolatile = true;
						break;
					}

				var plan = DataConnection.QueryRunner.PlanEagerScenario(dataConnection, scenario);

				var commands = new List<CombinedCommand>();
				var prepared = new List<PreparedCommand>();

				foreach (var group in plan.Groups)
				{
					var stepIndexes     = group.StepIndexes;
					var groupStatements = new SqlStatement[stepIndexes.Count];

					for (var k = 0; k < stepIndexes.Count; k++)
						groupStatements[k] = scenario.Steps[stepIndexes[k]].Statement!;

#if SUPPORTS_DBBATCH
					// DbBatch: the whole group is one command carrying one isolated-scope statement per step (each its own
					// DbBatchCommand + parameter scope); no SQL-length split, so it covers every step of the group in order.
					if (useBatch)
					{
						var rendered = ScenarioCommandRenderer.RenderStatementTemplates(dataConnection, groupStatements, values);

						// Run this execution off the freshly-rendered statements (bind all, no re-render).
						commands.Add(BindCommand(dataConnection, new PreparedCommand(stepIndexes, null, rendered), useBatch, scenario, values));

						// Cache with the parameter-dependent slots nulled so a later run re-renders only those.
						var slots = new CommandWithParameters?[rendered.Length];

						for (var k = 0; k < rendered.Length; k++)
							slots[k] = volatility[stepIndexes[k]] ? null : rendered[k];

						prepared.Add(new PreparedCommand(stepIndexes, null, slots));

						continue;
					}
#endif

					// Concat path: each length-split batch is one pre-merged (semicolon-concatenated) command. Its shared,
					// stateful parameter normalizer means a slice can't be half-cached, so the concat cache is all-or-nothing
					// (stored only when the whole scenario is stable, see the store below); otherwise it re-renders each run.
					var batches = DataConnection.QueryRunner.RenderCombinedBatchTemplates(dataConnection, groupStatements, values);

					var offset = 0;

					foreach (var (sql, sqlParameters, statementCount) in batches)
					{
						var indexes = new int[statementCount];

						for (var k = 0; k < statementCount; k++)
							indexes[k] = stepIndexes[offset + k];

						var command = new PreparedCommand(indexes, new CommandWithParameters(sql, sqlParameters), null);

						prepared.Add(command);
						commands.Add(BindCommand(dataConnection, command, useBatch, scenario, values));

						offset += statementCount;
					}
				}

				// Cache the per-command templates for this backend so re-enumeration binds DbParameters instead of
				// re-rendering. Batch caches per-statement (null slots for parameter-dependent steps, re-rendered per run);
				// concat is all-or-nothing (skipped when any step is volatile — its slices share a parameter scope and can't
				// be half-cached), matching today's behavior on that fallback path.
				if (useBatch || !anyVolatile)
					_query.QueryInfo.EagerCommandCache = new PreparedScenario(scenario, plan, prepared.ToArray(), useBatch);

				return commands;
			}

			// Binds a cached PreparedCommand to this execution's DbParameter values, producing the command the executor runs.
			// Shared by the fast (cached) path and the first render. On the batch backend, a null slot is a parameter-dependent
			// statement re-rendered here in its own isolated scope; concat carries a single, always-present merged command.
			static CombinedCommand BindCommand(DataConnection dataConnection, PreparedCommand command, bool useBatch, SqlCommandScenario scenario, SqlParameterValues values)
			{
				var stepIndexes = command.StepIndexes;

				if (useBatch)
				{
					var statements = command.Batch!;
					var rendered   = new RenderedStatement[statements.Length];

					for (var i = 0; i < statements.Length; i++)
					{
						var slot = statements[i];

						if (slot is null)
						{
#if SUPPORTS_DBBATCH
							slot = ScenarioCommandRenderer.RenderStatementTemplates(
								dataConnection, [scenario.Steps[stepIndexes[i]].Statement!], values)[0];
#else
							throw new InvalidOperationException("Eager batch cache slot is null on a non-DbBatch build.");
#endif
						}

						rendered[i] = new RenderedStatement(
							slot.Command,
							ScenarioCommandRenderer.MaterializeDbParameters(dataConnection, slot.SqlParameters, values));
					}

					return new CombinedCommand(rendered, stepIndexes, null);
				}

				// Concat: one pre-merged command, always present (concat is cached only when fully stable).
				var concat = command.Concat!;

				return new CombinedCommand(
					[new RenderedStatement(concat.Command, ScenarioCommandRenderer.MaterializeDbParameters(dataConnection, concat.SqlParameters, values))],
					stepIndexes, null);
			}

			// Per-step parameter-dependence: a step is volatile when its statement's SQL varies with parameter values, so its
			// rendered template can't be cached and must be re-rendered each run. Finer than an all-or-nothing scenario check —
			// a stable main can be cached while a Contains-filtered (LIKE) child re-renders. Each step has a distinct SqlStatement
			// instance, so per-step evaluation is well-defined. Eager loading bypasses GetCommand's check, so it is done here.
			static bool[] ComputeStepVolatility(DataConnection dataConnection, SqlCommandScenario scenario)
			{
				var options      = dataConnection.Options;
				var sqlOptimizer = dataConnection.DataProvider.GetSqlOptimizer(options);
				var steps        = scenario.Steps;
				var result       = new bool[steps.Count];

				for (var i = 0; i < steps.Count; i++)
				{
					// A self-executing step has no rendered SQL, so it is never a cacheable render slot (not volatile here).
					if (steps[i].Statement is not { } statement)
					{
						result[i] = false;
						continue;
					}

					result[i] = statement.IsParameterDependent
						|| sqlOptimizer.IsParameterDependent(NullabilityContext.NonQuery, dataConnection.MappingSchema, statement, options);
				}

				return result;
			}

			// A step in the unified eager walk: a sequential preamble (runs its own Execute) or a combined reader command
			// (ExecuteCombined + materialize its combinable children; the main-carrying command also streams the main rows).
			abstract record EagerStep;
			sealed record SequentialStep(int PreambleIndex) : EagerStep;
			sealed record CombinedReaderStep(CombinedCommand Command, bool CarriesMain) : EagerStep;

			// The ordered eager walk, shared by the sync and async drivers: all non-combinable preambles first (index order;
			// they may depend on each other and feed the combinable materializers + main mapper via the shared context),
			// then the combined reader commands with the main-carrying command last. BuildCommands does not read the context,
			// so rendering it here (before the sequential preambles run) is order-independent. Returns the scenario steps too,
			// so the combined-reader commands drive the shared ExecuteCombinedHarvest (whose walk reads each step's Kind).
			(IReadOnlyList<SqlCommandStep> ScenarioSteps, List<EagerStep> Steps) BuildSteps()
			{
				var (scenario, values) = PrepareScenario();
				var commands           = BuildCommands(scenario, values);

				var steps = new List<EagerStep>(_nonCombinableIndexes.Length + commands.Count);

				foreach (var i in _nonCombinableIndexes)
					steps.Add(new SequentialStep(i));

				var mainStepIndex = _combinableIndexes.Length;

				foreach (var command in commands)
					steps.Add(new CombinedReaderStep(command, command.StepIndexes[command.StepIndexes.Count - 1] == mainStepIndex));

				return (scenario.Steps, steps);
			}

			// Executes a combined command through the shared ExecuteCombinedHarvest seam and harvests its combinable child
			// result sets into the execution context, returning the open reader for the caller to dispose or stream from.
			// harvestStepIndexes are the scenario indices of the children only; a main-carrying command excludes its terminal
			// (main) step, whose result set the walk's trailing NextResult leaves the reader positioned on for streaming.
			DataReaderWrapper ExecuteAndHarvest(DataConnection dataConnection, CombinedCommand command, IReadOnlyList<int> harvestStepIndexes, IReadOnlyList<SqlCommandStep> scenarioSteps, SqlCommandExecutionContext context)
			{
				return DataConnection.QueryRunner.ExecuteCombinedHarvest(dataConnection, command, scenarioSteps, harvestStepIndexes, (i, r) =>
				{
					var preambleIndex = _combinableIndexes[i];

					context.SetResult(preambleIndex, _preambles[preambleIndex].Harvest(_dataContext, _expressions, _parameters, context, preambleIndex, r));
				});
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var _ = ActivityService.Start(ActivityID.GetIEnumerable);

				var context                = new SqlCommandExecutionContext(_preambles.Length);
				var dataConnection         = (DataConnection)_dataContext;
				var (scenarioSteps, steps) = BuildSteps();

				DataReaderWrapper? mainReader = null;

				try
				{
					foreach (var step in steps)
					{
						if (step is SequentialStep sequential)
						{
							context.SetResult(sequential.PreambleIndex, _preambles[sequential.PreambleIndex].Harvest(_dataContext, _expressions, _parameters, context, sequential.PreambleIndex, reader: null));
							continue;
						}

						var readerStep     = (CombinedReaderStep)step;
						var command        = readerStep.Command;
						var stepIndexes    = command.StepIndexes;
						// A main-carrying command's terminal (main) step is its last; harvest only the children, and the walk's
						// trailing NextResult after the last child leaves the reader on the main result set for streaming below.
						var harvestIndexes = readerStep.CarriesMain ? stepIndexes.Take(stepIndexes.Count - 1).ToList() : stepIndexes;

						var reader = ExecuteAndHarvest(dataConnection, command, harvestIndexes, scenarioSteps, context);

						if (!readerStep.CarriesMain)
						{
							reader.Dispose();
						}
						else
						{
							mainReader = reader;

							var dr = reader.DataReader!;

							foreach (var item in _query.GetResultFromReader!(_dataContext, _expressions, _parameters, context, dr))
								yield return item;
						}
					}
				}
				finally
				{
					mainReader?.Dispose();
				}
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			// Async sibling of ExecuteAndHarvest.
			Task<DataReaderWrapper> ExecuteAndHarvestAsync(DataConnection dataConnection, CombinedCommand command, IReadOnlyList<int> harvestStepIndexes, IReadOnlyList<SqlCommandStep> scenarioSteps, SqlCommandExecutionContext context, CancellationToken cancellationToken)
			{
				return DataConnection.QueryRunner.ExecuteCombinedHarvestAsync(dataConnection, command, scenarioSteps, harvestStepIndexes, async (i, r) =>
				{
					var preambleIndex = _combinableIndexes[i];

					context.SetResult(preambleIndex, await _preambles[preambleIndex].HarvestAsync(_dataContext, _expressions, _parameters, context, preambleIndex, r, cancellationToken).ConfigureAwait(false));
				}, cancellationToken);
			}

			// In case of change the logic of this method, DO NOT FORGET to change the sibling GetEnumerator.
			public async IAsyncEnumerable<T> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				var context                = new SqlCommandExecutionContext(_preambles.Length);
				var dataConnection         = (DataConnection)_dataContext;
				var (scenarioSteps, steps) = BuildSteps();

				DataReaderWrapper? mainReader = null;

				try
				{
					foreach (var step in steps)
					{
						if (step is SequentialStep sequential)
						{
							context.SetResult(sequential.PreambleIndex, await _preambles[sequential.PreambleIndex].HarvestAsync(_dataContext, _expressions, _parameters, context, sequential.PreambleIndex, null, cancellationToken).ConfigureAwait(false));
							continue;
						}

						var readerStep     = (CombinedReaderStep)step;
						var command        = readerStep.Command;
						var stepIndexes    = command.StepIndexes;
						// A main-carrying command's terminal (main) step is its last; harvest only the children, and the walk's
						// trailing NextResult after the last child leaves the reader on the main result set for streaming below.
						var harvestIndexes = readerStep.CarriesMain ? stepIndexes.Take(stepIndexes.Count - 1).ToList() : stepIndexes;

						var reader = await ExecuteAndHarvestAsync(dataConnection, command, harvestIndexes, scenarioSteps, context, cancellationToken).ConfigureAwait(false);

						if (!readerStep.CarriesMain)
						{
							await reader.DisposeAsync().ConfigureAwait(false);
						}
						else
						{
							mainReader = reader;

							var dr = reader.DataReader!;

							await foreach (var item in _query.GetResultFromReader!(_dataContext, _expressions, _parameters, context, dr).WithCancellation(cancellationToken).ConfigureAwait(false))
								yield return item;
						}
					}
				}
				finally
				{
					if (mainReader != null)
						await mainReader.DisposeAsync().ConfigureAwait(false);
				}
			}

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				=> GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken);
		}

		// Returns a combined N+1 -> 1 eager-loading enumerable for the main query, or null when the query can't be
		// combined (no combinable-reader materializer, no preambles, provider lacks multi-statement / multi-result-set
		// support, the main query isn't a single statement, or any preamble isn't combinable) — callers then fall back
		// to the sequential InitPreambles + GetResultEnumerable path.
		internal static IResultEnumerable<T>? TryGetCombinedEagerEnumerable<T>(
			Query<T> query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			if (query.GetResultFromReader == null)
				return null;

			var preambles = query.PreamblesArray;

			if (preambles == null || preambles.Length == 0)
				return null;

			if (dataContext is not DataConnection dataConnection
				|| !dataConnection.DataProvider.SqlProviderFlags.IsMultiStatementBatchSupported
				|| !dataConnection.DataProvider.SqlProviderFlags.IsMultipleResultSetsSupported)
				return null;

			// Query hints (context QueryHints / one-shot NextQueryHints) are applied AND cleared by the sequential
			// GetCommand -> GetNextCommandHints path, which the combined executor bypasses. Fall back to sequential when
			// hints are pending; otherwise the hint is dropped from the eager SQL and a one-shot NextQueryHints leaks onto
			// the next query.
			if (dataContext.QueryHints.Count > 0 || dataContext.NextQueryHints.Count > 0)
				return null;

			if (!EagerResultEnumerable<T>.HasCombinable(preambles))
				return null;

			return new EagerResultEnumerable<T>(dataContext, expressions, query, parameters, preambles);
		}

		static IResultEnumerable<T> ExecuteQuery<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<T>         mapper,
			IQueryExpressions expressions,
			object?[]?        ps,
			SqlCommandExecutionContext? preambles,
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

			query.GetResultFromReader = (db, expr, ps, preambles, reader) =>
				new ExternalReaderResultEnumerable<T>(db, expr, query, ps, preambles, 0, mapper, reader);
		}

		static readonly PropertyInfo _dataContextInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.DataContext);
		static readonly PropertyInfo _expressionsInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.Expressions);
		static readonly PropertyInfo _parametersInfo  = MemberHelper.PropertyOf<IQueryRunner>(p => p.Parameters);
		static readonly PropertyInfo _preamblesInfo   = MemberHelper.PropertyOf<IQueryRunner>(p => p.ExecutionContext);

		public static readonly PropertyInfo RowsCountInfo   = MemberHelper.PropertyOf<IQueryRunner>(p => p.RowsCount);
		public static readonly PropertyInfo DataContextInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.DataContext);

		static Expression<Func<IQueryRunner, DbDataReader, T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,IDataContext, DbDataReader, IQueryExpressions, object?[]?,SqlCommandExecutionContext?,T>> expression)
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
			Expression<Func<IQueryRunner,IDataContext,DbDataReader,IQueryExpressions,object?[]?,SqlCommandExecutionContext?,T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Aggregation, All, Any, Contains, Count

		public static void SetRunQuery<T>(
			Query<T>                                                                                                   query,
			Expression<Func<IQueryRunner,IDataContext,DbDataReader,IQueryExpressions,object?[]?,SqlCommandExecutionContext?,object>> expression)
		{
			FinalizeQuery(query);

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
			SqlCommandExecutionContext? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteElement);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles?.Results);
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

#pragma warning disable MA0098 // Use indexer instead of LINQ methods
			return Array.Empty<T>().First();
#pragma warning restore MA0098 // Use indexer instead of LINQ methods
		}

		static async Task<T> ExecuteElementAsync<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<object>    mapper,
			IQueryExpressions expressions,
			object?[]?        ps,
			SqlCommandExecutionContext? preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteElementAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles?.Results);
				await using var _1 = runner.ConfigureAwait(false);

				var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
				await using var _2 = dr.ConfigureAwait(false);

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

#pragma warning disable MA0098 // Use indexer instead of LINQ methods
				return Array.Empty<T>().First();
#pragma warning restore MA0098 // Use indexer instead of LINQ methods
			}
		}

		#endregion

		#region ScalarQuery

		public static void SetScalarQuery(Query query)
		{
			FinalizeQuery(query);

			query.GetElement      = (db, expr, ps, preambles) => ScalarQuery(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => ScalarQueryAsync(query, db, expr, ps, preambles, token);
		}

		static object? ScalarQuery(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteScalar);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles?.Results);
			return runner.ExecuteScalar();
		}

		static async Task<object?> ScalarQueryAsync(
			Query             query,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			object?[]?        ps,
			SqlCommandExecutionContext? preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteScalarAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles?.Results);
				await using (runner.ConfigureAwait(false))
					return await runner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		#endregion

		#region NonQueryQuery

		public static void SetNonQueryQuery(Query query)
		{
			FinalizeQuery(query);

			query.GetElement      = (db, expr, ps, preambles) => NonQueryQuery(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => NonQueryQueryAsync(query, db, expr, ps, preambles, token);
		}

		static int NonQueryQuery(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteNonQuery);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles?.Results);
			return runner.ExecuteNonQuery();
		}

		static async Task<object?> NonQueryQueryAsync(
			Query             query,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			object?[]?        ps,
			SqlCommandExecutionContext? preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteNonQueryAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles?.Results);
				await using (runner.ConfigureAwait(false))
					return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		#endregion

		#region GetSqlText

		public static IReadOnlyList<QuerySql> GetSqlText(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? preambles)
		{
			using var m      = ActivityService.Start(ActivityID.GetSqlText);

			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, preambles?.Results);
			return runner.GetSqlText();
		}

		#endregion
	}
}
