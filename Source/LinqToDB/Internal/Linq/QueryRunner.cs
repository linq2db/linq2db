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

		// Folded overload: sources the compiled-query parameters from the context instead of a separate argument.
		internal static void SetParameters(
			Query query, IQueryExpressions expressions, IDataContext? parametersContext, SqlParameterValues parameterValues, SqlCommandExecutionContext? context)
			=> SetParameters(query, expressions, parametersContext, context?.Parameters, parameterValues, context);

		internal static void SetParameters(
			Query query, IQueryExpressions expressions, IDataContext? parametersContext, object?[]? parameters, SqlParameterValues parameterValues, SqlCommandExecutionContext? context = null)
		{
			if (query.ParameterAccessors == null)
				return;

			foreach (var accessor in query.ParameterAccessors)
			{
				var clientValue   = accessor.ClientValueAccessor(expressions, parametersContext, parameters, context);
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

		static Func<Query,IDataContext,Mapper<T>, IQueryExpressions, SqlCommandExecutionContext?,int, IResultEnumerable<T>> GetExecuteQuery<T>(
				Query                                                                                                  query,
				Func<Query,IDataContext,Mapper<T>, IQueryExpressions, SqlCommandExecutionContext?,int, IResultEnumerable<T>> queryFunc)
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

				queryFunc = (qq, db, mapper, expr, harvesters, qn) =>
					new LimitResultEnumerable<T>(q(qq, db, mapper, expr, harvesters, qn),
						EvaluateTakeSkipValue(qq, expr, db, harvesters?.Parameters, skipValue), null);
			}

			return queryFunc;
		}

		sealed class BasicResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly IDataContext      _dataContext;
			readonly IQueryExpressions _expressions;
			readonly Query             _query;
			readonly SqlCommandExecutionContext? _harvesters;
			readonly int               _queryNumber;
			readonly Mapper<T>         _mapper;

			public BasicResultEnumerable(
				IDataContext      dataContext,
				IQueryExpressions expressions,
				Query             query,
				SqlCommandExecutionContext? harvesters,
				int               queryNumber,
				Mapper<T>         mapper)
			{
				_dataContext = dataContext;
				_expressions = expressions;
				_query       = query;
				_harvesters  = harvesters;
				_queryNumber = queryNumber;
				_mapper      = mapper;
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var _      = ActivityService.Start(ActivityID.ExecuteQuery);

				using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _harvesters?.Parameters, _harvesters?.Results);
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
					var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _harvesters?.Parameters, _harvesters?.Results);
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
			readonly SqlCommandExecutionContext? _harvesters;
			readonly int               _queryNumber;
			readonly Mapper<T>         _mapper;
			readonly DbDataReader      _dataReader;

			public ExternalReaderResultEnumerable(
				IDataContext      dataContext,
				IQueryExpressions expressions,
				Query             query,
				SqlCommandExecutionContext? harvesters,
				int               queryNumber,
				Mapper<T>         mapper,
				DbDataReader      dataReader)
			{
				_dataContext = dataContext;
				_expressions = expressions;
				_query       = query;
				_harvesters  = harvesters;
				_queryNumber = queryNumber;
				_mapper      = mapper;
				_dataReader  = dataReader;
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _harvesters?.Parameters, _harvesters?.Results);

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
				var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _harvesters?.Parameters, _harvesters?.Results);
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
		// a very large fan-out). Non-combinable harvesters (KeyedQuery / CteUnion / detached / buffer / no-op) run sequentially
		// FIRST (in index order; they may depend on each other), then the combinable children + main are modelled as a
		// SqlCommandScenario of Reader steps and grouped by PlanScenario. Each combinable child result set is buffered into
		// its HarvesterResult; the main result set (always the last step of the last group) streams lazily from that group's
		// reader, which this enumerable owns. Created only when the provider supports multi-statement batches with multiple
		// result sets and at least one harvester is combinable (see TryGetCombinedEagerEnumerable); a purely non-combinable
		// load falls back to the sequential InitHarvesters path.
		sealed class EagerResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly IDataContext      _dataContext;
			readonly IQueryExpressions _expressions;
			readonly Query<T>          _query;
			readonly object?[]?        _parameters;
			readonly Harvester[]       _harvesters;
			// Harvester indices partitioned by combinability, in build order. A combinable harvester becomes a combined Reader
			// step; a non-combinable one becomes a SelfExecuting step. Both keep their harvester index as the scenario step
			// index (the main query is the last step); combinable + main are the combined groups, self-executing are singletons.
			readonly int[]             _combinableIndexes;
			readonly int[]             _nonCombinableIndexes;

			public EagerResultEnumerable(
				IDataContext      dataContext,
				IQueryExpressions expressions,
				Query<T>          query,
				object?[]?        parameters,
				Harvester[]        harvesters)
			{
				_dataContext = dataContext;
				_expressions = expressions;
				_query       = query;
				_parameters  = parameters;
				_harvesters  = harvesters;

				var combinable    = new List<int>(harvesters.Length);
				var nonCombinable = new List<int>(harvesters.Length);

				for (var i = 0; i < harvesters.Length; i++)
				{
					if (IsCombinable(harvesters[i]))
						combinable.Add(i);
					else
						nonCombinable.Add(i);
				}

				_combinableIndexes    = combinable.ToArray();
				_nonCombinableIndexes = nonCombinable.ToArray();
			}

			static bool IsCombinable(Harvester harvester)
				=> harvester is IStepMaterializer { CanCombine: true } materializer && materializer.GetCombinableStatement() != null;

			// Whether any harvester is reader-combinable — the single definition of the combinable predicate, shared by the
			// ctor's partition and TryGetCombinedEagerEnumerable's gate.
			internal static bool HasCombinable(Harvester[] harvesters)
			{
				foreach (var harvester in harvesters)
					if (IsCombinable(harvester))
						return true;

				return false;
			}

			// Merges every combinable child's and the main query's parameter values into one SqlParameterValues (keyed by
			// SqlParameter node). Genuinely per-run - the values are this execution's - so it is the only part of the old
			// PrepareScenario a warm execution still has to do.
			SqlParameterValues CollectParameterValues()
			{
				var values = new SqlParameterValues();

				foreach (var i in _combinableIndexes)
					((IStepMaterializer)_harvesters[i]).AddCombinableParameterValues(values, _expressions, _dataContext, _parameters);

				SetParameters(_query, _expressions, _dataContext, _parameters, values);

				return values;
			}

			// Models ALL harvesters + main as one scenario, step index == harvester build index (main last): a combinable child
			// is a Reader step carrying its rendered statement; a non-combinable (detached / keyed / CteUnion) harvester is a
			// SelfExecuting step with no statement (it runs its own query through a harvester). The unified index lets the
			// interpreter write and the projection read the same context slot with no translation.
			//
			// Parameter-independent, and needed only on the COLD path: the volatility check and the render read each step's
			// statement from here, and ProjectExecutionSteps derives the cached ExecutionStep[] from it. A warm execution
			// reuses those cached results and never builds a scenario (GetStepStatement covers the one thing it still needs).
			SqlCommandScenario BuildScenario()
			{
				var mainStepIndex = _harvesters.Length;
				var steps         = new SqlCommandStep[mainStepIndex + 1];

				foreach (var i in _combinableIndexes)
					steps[i] = new SqlCommandStep { Statement = GetStepStatement(i), Kind = SqlStepKind.Reader };

				foreach (var i in _nonCombinableIndexes)
					steps[i] = new SqlCommandStep { Statement = null, Kind = SqlStepKind.SelfExecuting };

				steps[mainStepIndex] = new SqlCommandStep { Statement = GetStepStatement(mainStepIndex), Kind = SqlStepKind.Reader };

				return new SqlCommandScenario { Steps = steps, OutcomeSteps = [] };
			}

			// The statement rendered for a combined step, fetched straight from its owner: the main query for the last step,
			// otherwise the combinable child's own query. Lets a warm execution re-render a volatile batch slot without
			// materializing a SqlCommandScenario just to read one statement out of it. Not valid for a self-executing step,
			// which has no rendered SQL and is never part of a combined command.
			SqlStatement GetStepStatement(int stepIndex)
				=> stepIndex == _harvesters.Length
					? _query.QueryInfo.Statement
					: ((IStepMaterializer)_harvesters[stepIndex]).GetCombinableStatement()!;

			// The batch's participants: the combinable children (in build order) followed by the main query, each paired with
			// the context slot its result lands in. Self-executing harvesters are NOT here — they run as their own commands
			// before the batch — so the slots are deliberately sparse, which is why CombinedReaderStep carries the slot rather
			// than relying on position.
			CombinedReaderStep[] BuildBatchSteps()
			{
				var result = new CombinedReaderStep[_combinableIndexes.Length + 1];

				for (var i = 0; i < _combinableIndexes.Length; i++)
					result[i] = new CombinedReaderStep(_combinableIndexes[i], GetStepStatement(_combinableIndexes[i]));

				result[_combinableIndexes.Length] = new CombinedReaderStep(_harvesters.Length, GetStepStatement(_harvesters.Length));

				return result;
			}

			// The physical grouping the interpreter walks.
			//
			// ORDERING INVARIANT (relied on by every eager strategy): non-combinable steps run FIRST, as singleton groups in
			// ascending index order, THEN the combined groups (combinable children + main). So a non-combinable harvester
			// executes before every combinable sibling and must only read result slots that have already run - its own
			// lower-indexed dependencies. Nested eager loads build first, so they get lower indices and materialize earlier; a
			// parent harvester always reads its child slots after they are populated. A non-combinable harvester that read a
			// *combinable* sibling's (higher-lifecycle) slot would find it unpopulated and silently produce an empty collection
			// - no exception. Any new strategy MUST preserve this direction (covered by
			// MixedCombinableAndNonCombinable_NestedKeyedUnderCombinable).
			//
			// Parameter-independent (the partition is fixed and each command's step indices come from the cached template), so
			// it is built on the cold path and cached with the templates.
			SqlCommandGroup[] BuildGroups(IReadOnlyList<CombinedCommand> commands)
			{
				var groups = new SqlCommandGroup[_nonCombinableIndexes.Length + commands.Count];
				var g      = 0;

				foreach (var i in _nonCombinableIndexes)
					groups[g++] = new SqlCommandGroup { StepIndexes = [i] };

				foreach (var command in commands)
					groups[g++] = new SqlCommandGroup { StepIndexes = command.StepIndexes };

				return groups;
			}

			// Builds the unified execution plan for one enumeration.
			//
			// Everything structural - the statement-free step facts, the group list and the rendered command templates - is
			// parameter-independent and cached on QueryInfo, so a WARM execution allocates only this run's parameter values,
			// its DbParameters and the per-group command array. A COLD execution builds the scenario once, renders, and stores
			// all three. commandByGroup aligns with the groups: null for a self-executing singleton group, the bound command
			// for a combined group.
			(ExecutionStep[] Steps, SqlCommandGroup[] Groups, CombinedCommand?[] CommandByGroup) BuildPlan()
			{
				// Held on the same object DataConnection.QueryRunner.GetCommand locks, and for the same reason: that
				// monitor is what licenses GetCommand's Modify-mode (in-place mutating) visitors over
				// QueryInfo.Statement. This path renders that same shared statement, so without the lock it is a
				// traversal racing those mutations. GetCommand takes the lock on every execution, not only cold ones,
				// so this adds no serialization that concurrent executions of one query did not already have. It also
				// makes the EagerCommandCache read/render/publish sequence atomic, so two cold executions cannot both
				// render and both publish.
				lock (_query.QueryInfo)
				{
					var dataConnection = (DataConnection)_dataContext;
					var useBatch       = dataConnection.CanUseDbBatch;   // false on frameworks without the DbBatch API
					var values         = CollectParameterValues();

					// A cache built for the OTHER backend is unusable (batch and concat shapes are not interchangeable).
					var cache = _query.QueryInfo.EagerCommandCache is { } c && c.WasBatch == useBatch ? c : null;

					// The render / split / bind machinery is CombinedReaderBatch's; what stays here is eager-specific — the
					// self-executing preambles, the group ordering they impose, and the terminal main query.
					var commands = new CombinedReaderBatch(dataConnection, BuildBatchSteps())
						.Bind(values, useBatch, cache?.Commands, out var templates);

					// Warm: the step facts and the group list are parameter-independent and were cached with the templates, so
					// neither the scenario nor the grouping is rebuilt (the ?? short-circuits before BuildScenario runs).
					var steps  = cache?.Steps  ?? ScenarioCommandRenderer.ProjectExecutionSteps(BuildScenario());
					var groups = cache?.Groups ?? BuildGroups(commands);

					var commandByGroup = new CombinedCommand?[groups.Length];

					for (var i = 0; i < commands.Count; i++)
						commandByGroup[_nonCombinableIndexes.Length + i] = commands[i];

					if (templates != null)
						_query.QueryInfo.EagerCommandCache = new PreparedScenario(steps, groups, templates, useBatch);

					return (steps, groups, commandByGroup);
				}
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var _ = ActivityService.Start(ActivityID.GetIEnumerable);

				var context        = new SqlCommandExecutionContext(_harvesters.Length, _parameters);
				var dataConnection = (DataConnection)_dataContext;
				var (steps, groups, commandByGroup) = BuildPlan();

				// The combined command's single reader is walked across multiple harvest steps, and each step (plus the main
				// stream below) materializes through a nested query runner. When CloseAfterUse is set - e.g. the EF Core bridge
				// borrows an external connection and closes it after each use - that nested runner's dispose would Close() the
				// context and dispose the shared reader mid-walk (NextResult on a closed reader). Suppress it until the reader
				// is fully consumed, and restore it before the final dispose so the connection is still closed as requested.
				var closeAfterUse = _dataContext.CloseAfterUse;
				_dataContext.CloseAfterUse = false;

				DataReaderWrapper? mainReader = null;

				try
				{
					// One shared group-plan walk: self-executing harvester singletons run their own query; each combined group
					// runs as one command; the main-carrying group hands back its open reader, which the caller streams below.
					mainReader = DataConnection.QueryRunner.RunGroups(
						dataConnection, steps, groups,
						(group, groupIndex) => commandByGroup[groupIndex]!,
						group => steps[group.StepIndexes[0]].Kind == SqlStepKind.SelfExecuting,
						(stepIndex, groupIndex) => context.SetResult(stepIndex, _harvesters[stepIndex].Harvest(_dataContext, _expressions, context, reader: null)),
						(i, dr) => context.SetResult(i, _harvesters[i].Harvest(_dataContext, _expressions, context, dr)),
						terminalStepIndex: _harvesters.Length);

					if (mainReader != null)
						foreach (var item in _query.GetResultFromReader!(_dataContext, _expressions, context, mainReader.DataReader!))
							yield return item;
				}
				finally
				{
					_dataContext.CloseAfterUse = closeAfterUse;
					mainReader?.Dispose();
				}
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			// In case of change the logic of this method, DO NOT FORGET to change the sibling GetEnumerator.
			public async IAsyncEnumerable<T> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				// Mirror the sync GetEnumerator's telemetry scope so async eager materialization emits an enumeration
				// activity span too (there is no eager-specific async ActivityID; the sync eager path uses GetIEnumerable).
				await using (ActivityService.StartAndConfigureAwait(ActivityID.GetIEnumerable))
				{
					var context        = new SqlCommandExecutionContext(_harvesters.Length, _parameters);
					var dataConnection = (DataConnection)_dataContext;
					var (steps, groups, commandByGroup) = BuildPlan();

					// See the sync GetEnumerator: suppress CloseAfterUse while the shared combined reader is walked (each
					// harvest / the main stream materializes through a nested runner whose dispose would otherwise Close() the
					// context mid-walk on an external connection), and restore it before the final dispose.
					var closeAfterUse = _dataContext.CloseAfterUse;
					_dataContext.CloseAfterUse = false;

					DataReaderWrapper? mainReader = null;

					try
					{
						mainReader = await DataConnection.QueryRunner.RunGroupsAsync(
							dataConnection, steps, groups,
							(group, groupIndex) => commandByGroup[groupIndex]!,
							group => steps[group.StepIndexes[0]].Kind == SqlStepKind.SelfExecuting,
							async (stepIndex, groupIndex) => context.SetResult(stepIndex, await _harvesters[stepIndex].HarvestAsync(_dataContext, _expressions, context, null, cancellationToken).ConfigureAwait(false)),
							async (i, dr) => context.SetResult(i, await _harvesters[i].HarvestAsync(_dataContext, _expressions, context, dr, cancellationToken).ConfigureAwait(false)),
							terminalStepIndex: _harvesters.Length,
							cancellationToken).ConfigureAwait(false);

						if (mainReader != null)
							await foreach (var item in _query.GetResultFromReader!(_dataContext, _expressions, context, mainReader.DataReader!).WithCancellation(cancellationToken).ConfigureAwait(false))
								yield return item;
					}
					finally
					{
						_dataContext.CloseAfterUse = closeAfterUse;

						if (mainReader != null)
							await mainReader.DisposeAsync().ConfigureAwait(false);
					}
				}
			}

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				=> GetAsyncEnumerable(cancellationToken).GetAsyncEnumerator(cancellationToken);
		}

		// Returns a combined N+1 -> 1 eager-loading enumerable for the main query, or null when the query can't be
		// combined (no combinable-reader materializer, no harvesters, provider lacks multi-statement / multi-result-set
		// support, the main query isn't a single statement, or any harvester isn't combinable) — callers then fall back
		// to the sequential InitHarvesters + GetResultEnumerable path.
		internal static IResultEnumerable<T>? TryGetCombinedEagerEnumerable<T>(
			Query<T> query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			if (query.GetResultFromReader == null)
				return null;

			var harvesters = query.HarvestersArray;

			if (harvesters == null || harvesters.Length == 0)
				return null;

			// Master switch (off by default in 6.x): combining is observable, not merely faster — it introduces the
			// implicit read-consistency transaction and changes the emitted SQL. Provider capability is checked on top of
			// it, never instead of it.
			if (!dataContext.Options.LinqOptions.UseCombinedCommands)
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

			// Same reason, the other piece of GetCommand-only state: DataConnection.ProcessQuery is a shipped extension
			// point that lets a subclass rewrite the statement before render, and GetCommand is its only caller. This path
			// renders the main query's statement and each combinable child's directly, so a subclass's rewrite would keep
			// applying to the non-combinable children and silently stop applying to everything else. Combining steps aside
			// for such a subclass rather than apply the rewrite to part of the load.
			if (dataConnection.IsProcessQueryOverridden)
				return null;

			if (!EagerResultEnumerable<T>.HasCombinable(harvesters))
				return null;

			return new EagerResultEnumerable<T>(dataContext, expressions, query, parameters, harvesters);
		}

		static IResultEnumerable<T> ExecuteQuery<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<T>         mapper,
			IQueryExpressions expressions,
			SqlCommandExecutionContext? harvesters,
			int               queryNumber
		)
		{
			return new BasicResultEnumerable<T>(dataContext, expressions, query, harvesters, queryNumber, mapper);
		}

		static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner, DbDataReader, T>> expression)
		{
			var executeQuery = GetExecuteQuery<T>(query, ExecuteQuery);

			var mapper   = new Mapper<T>(expression);

			query.GetResultEnumerable = (db, expr, harvesters) =>
			{
				using var _ = ActivityService.Start(ActivityID.GetIEnumerable);
				return executeQuery(query, db, mapper, expr, harvesters, 0);
			};

			query.GetResultFromReader = (db, expr, harvesters, reader) =>
				new ExternalReaderResultEnumerable<T>(db, expr, query, harvesters, 0, mapper, reader);
		}

		static readonly PropertyInfo _dataContextInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.DataContext);
		static readonly PropertyInfo _expressionsInfo = MemberHelper.PropertyOf<IQueryRunner>(p => p.Expressions);
		static readonly PropertyInfo _harvestersInfo  = MemberHelper.PropertyOf<IQueryRunner>(p => p.ExecutionContext);
		// The compiled-query parameters now live on the execution context; the mapper reads qr.ExecutionContext.Parameters.
		static readonly PropertyInfo _executionContextParametersInfo = MemberHelper.PropertyOf<SqlCommandExecutionContext>(c => c.Parameters);

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
			var harvestersVar    = expression.Parameters[5];

			var locals = new List<ParameterExpression>();
			var exprs  = new List<Expression>();

			SetLocal(dataContextVar, _dataContextInfo);
			SetLocal(expressionVar,  _expressionsInfo);
			// parametersVar is the compiled-query args slot. It is assigned only when the mapper body references it, which
			// happens iff this is a compiled query — and a compiled query always allocates an execution context (see the
			// QueryRunnerBase guard), so qr.ExecutionContext is non-null exactly when this two-hop read is emitted.
			SetLocalSource(parametersVar, Expression.Property(Expression.Property(queryRunnerParam, _harvestersInfo), _executionContextParametersInfo));
			SetLocal(harvestersVar,  _harvestersInfo);

			void SetLocal(ParameterExpression local, PropertyInfo prop)
				=> SetLocalSource(local, Expression.Property(queryRunnerParam, prop));

			void SetLocalSource(ParameterExpression local, Expression source)
			{
				if (expression.Body.Find(local) != null)
				{
					locals.Add(local);
					exprs. Add(Expression.Assign(local, source));
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

			query.GetElement      = (db, expr, harvesters) => ExecuteElement(query, db, mapper, expr, harvesters);
			query.GetElementAsync = (db, expr, harvesters, token) => ExecuteElementAsync<object?>(query, db, mapper, expr, harvesters, token);
		}

		static T ExecuteElement<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<T>         mapper,
			IQueryExpressions expressions,
			SqlCommandExecutionContext? harvesters)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteElement);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, harvesters?.Parameters, harvesters?.Results);
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
			SqlCommandExecutionContext? harvesters,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteElementAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, harvesters?.Parameters, harvesters?.Results);
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

			query.GetElement      = (db, expr, harvesters) => ScalarQuery(query, db, expr, harvesters);
			query.GetElementAsync = (db, expr, harvesters, token) => ScalarQueryAsync(query, db, expr, harvesters, token);
		}

		static object? ScalarQuery(Query query, IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? harvesters)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteScalar);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, harvesters?.Parameters, harvesters?.Results);
			return runner.ExecuteScalar();
		}

		static async Task<object?> ScalarQueryAsync(
			Query             query,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			SqlCommandExecutionContext? harvesters,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteScalarAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, harvesters?.Parameters, harvesters?.Results);
				await using (runner.ConfigureAwait(false))
					return await runner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		#endregion

		#region NonQueryQuery

		public static void SetNonQueryQuery(Query query)
		{
			FinalizeQuery(query);

			query.GetElement      = (db, expr, harvesters) => NonQueryQuery(query, db, expr, harvesters);
			query.GetElementAsync = (db, expr, harvesters, token) => NonQueryQueryAsync(query, db, expr, harvesters, token);
		}

		static int NonQueryQuery(Query query, IDataContext dataContext, IQueryExpressions expressions, SqlCommandExecutionContext? harvesters)
		{
			using var m      = ActivityService.Start(ActivityID.ExecuteNonQuery);
			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, harvesters?.Parameters, harvesters?.Results);
			return runner.ExecuteNonQuery();
		}

		static async Task<object?> NonQueryQueryAsync(
			Query             query,
			IDataContext      dataContext,
			IQueryExpressions expressions,
			SqlCommandExecutionContext? harvesters,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteNonQueryAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, harvesters?.Parameters, harvesters?.Results);
				await using (runner.ConfigureAwait(false))
					return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		#endregion

		#region GetSqlText

		public static IReadOnlyList<QuerySql> GetSqlText(Query query, IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? harvesters)
		{
			using var m      = ActivityService.Start(ActivityID.GetSqlText);

			using var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, parameters, harvesters?.Results);
			return runner.GetSqlText();
		}

		#endregion
	}
}
