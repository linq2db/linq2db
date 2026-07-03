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

			foreach (var sql in query.Queries)
			{
				sql.Statement = query.SqlOptimizer.Finalize(query.MappingSchema, sql.Statement, query.DataOptions);
			}

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

		static Func<Query,IDataContext,Mapper<T>, IQueryExpressions, object?[]?,object?[]?,int, IResultEnumerable<T>> GetExecuteQuery<T>(
				Query                                                                                                  query,
				Func<Query,IDataContext,Mapper<T>, IQueryExpressions, object?[]?,object?[]?,int, IResultEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			var selectQuery = query.Queries[0].Statement.SelectQuery!;
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
					var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles);
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
			readonly object?[]?        _preambles;
			readonly int               _queryNumber;
			readonly Mapper<T>         _mapper;
			readonly DbDataReader      _dataReader;

			public ExternalReaderResultEnumerable(
				IDataContext      dataContext,
				IQueryExpressions expressions,
				Query             query,
				object?[]?        parameters,
				object?[]?        preambles,
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
				using var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles);

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
				var runner = _dataContext.GetQueryRunner(_query, _dataContext, _queryNumber, _expressions, _parameters, _preambles);
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

		// Runs the main query together with all its combinable eager-load child collections as a size-bounded set of
		// multi-result-set commands: the [child1 … childN, main] statements are modelled as a SqlCommandScenario of
		// Reader steps and grouped by PlanScenario (which caps how many merge into one command). Each child result set
		// is buffered into its PreambleResult (mapped by that child query's own materializer); the main result set —
		// always the last step of the last group — streams lazily from that group's reader, which this enumerable owns.
		// One physical eager-load command: a semicolon-concatenated SQL (Sql/Parameters) or, on the DbBatch path, its
		// per-statement (text, parameters) list (BatchStatements). StepIndexes are the scenario step indices it covers,
		// in order; the main query is always the last step of the last command.
		readonly record struct EagerCommand(
			string? Sql,
			DbParameter[]? Parameters,
			IReadOnlyList<(string Sql, DbParameter[]? Parameters)>? BatchStatements,
			int[] StepIndexes);

		// Executes one eager-load command and returns its reader. On the DbBatch path the DbBatch is attached to the
		// reader wrapper (AdditionalDisposable) so it is released when the reader is disposed. Sync sibling below.
		static DataReaderWrapper ExecEagerCommand(DataConnection dataConnection, EagerCommand command)
		{
#if SUPPORTS_DBBATCH
			if (command.BatchStatements != null)
			{
				var batch  = dataConnection.CreateBatch(command.BatchStatements);
				var reader = dataConnection.ExecuteBatchDataReader(batch, System.Data.CommandBehavior.Default);

				reader.AdditionalDisposable = batch;

				return reader;
			}
#endif
			return DataConnection.QueryRunner.ExecuteRendered(dataConnection, command.Sql!, command.Parameters);
		}

		// In case of change the logic of this method, DO NOT FORGET to change the sibling method.
		static async Task<DataReaderWrapper> ExecEagerCommandAsync(DataConnection dataConnection, EagerCommand command, CancellationToken cancellationToken)
		{
#if SUPPORTS_DBBATCH
			if (command.BatchStatements != null)
			{
				var batch  = dataConnection.CreateBatch(command.BatchStatements);
				var reader = await dataConnection.ExecuteBatchDataReaderAsync(batch, System.Data.CommandBehavior.Default, cancellationToken).ConfigureAwait(false);

				reader.AdditionalDisposable = batch;

				return reader;
			}
#endif
			return await DataConnection.QueryRunner.ExecuteRenderedAsync(dataConnection, command.Sql!, command.Parameters, cancellationToken).ConfigureAwait(false);
		}

		// Collapses N+1 eager round-trips to 1 (a few for a very large fan-out); created only when every preamble is
		// combinable and the provider supports multi-statement batches with multiple result sets (see
		// TryGetCombinedEagerEnumerable), otherwise callers fall back to sequential InitPreambles.
		sealed class CombinedEagerResultEnumerable<T> : IResultEnumerable<T>
		{
			readonly IDataContext      _dataContext;
			readonly IQueryExpressions _expressions;
			readonly Query<T>          _query;
			readonly object?[]?        _parameters;
			readonly Preamble[]        _preambles;

			public CombinedEagerResultEnumerable(
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
			}

			// Models the eager load as a scenario of Reader steps [child1 … childN, main] and merges every child's and
			// the main's parameter values into one SqlParameterValues (keyed by SqlParameter node). The main is the last
			// step, so PlanScenario's contiguous grouping keeps it in (and at the end of) the last group.
			(SqlCommandScenario Scenario, SqlParameterValues Values) PrepareScenario()
			{
				var steps  = new SqlCommandStep[_preambles.Length + 1];
				var values = new SqlParameterValues();

				for (var i = 0; i < _preambles.Length; i++)
				{
					steps[i] = new SqlCommandStep { Statement = _preambles[i].GetCombinableStatement()!, Kind = SqlStepKind.Reader };
					_preambles[i].AddCombinableParameterValues(values, _expressions, _dataContext, _parameters);
				}

				steps[_preambles.Length] = new SqlCommandStep { Statement = _query.Queries[0].Statement, Kind = SqlStepKind.Reader };
				SetParameters(_query, _expressions, _dataContext, _parameters, values);

				return (new SqlCommandScenario { Steps = steps, OutcomeSteps = [] }, values);
			}

			// Plans the scenario into size-bounded commands: PlanScenario groups the steps (bounded by statement count),
			// then each group's statements render into one or more commands bounded by SQL length. Each returned command
			// carries the scenario step indices it covers, so the executor knows which child result sets to buffer and
			// where the main result set lands — always the last step of the last command.
			List<EagerCommand> BuildCommands()
			{
				var (scenario, values) = PrepareScenario();

				var dataConnection = (DataConnection)_dataContext;
				var plan           = DataConnection.QueryRunner.PlanEagerScenario(dataConnection, scenario);

				var commands = new List<EagerCommand>();

#if SUPPORTS_DBBATCH
				var useBatch = dataConnection.CanUseDbBatch;
#endif

				foreach (var group in plan.Groups)
				{
					var stepIndexes     = group.StepIndexes;
					var groupStatements = new SqlStatement[stepIndexes.Count];

					for (var k = 0; k < stepIndexes.Count; k++)
						groupStatements[k] = scenario.Steps[stepIndexes[k]].Statement;

#if SUPPORTS_DBBATCH
					// DbBatch: the whole group is one batch (each statement its own DbBatchCommand + parameter scope); no SQL-length
					// split, so it covers every step of the group in order.
					if (useBatch)
					{
						var batchStatements = ScenarioCommandRenderer.RenderBatchStatements(dataConnection, groupStatements, values);
						var batchIndexes    = new int[stepIndexes.Count];

						for (var k = 0; k < stepIndexes.Count; k++)
							batchIndexes[k] = stepIndexes[k];

						commands.Add(new EagerCommand(null, null, batchStatements, batchIndexes));

						continue;
					}
#endif

					var batches = DataConnection.QueryRunner.RenderCombinedBatches(dataConnection, groupStatements, values);

					var offset = 0;

					foreach (var (sql, parameters, statementCount) in batches)
					{
						var indexes = new int[statementCount];

						for (var k = 0; k < statementCount; k++)
							indexes[k] = stepIndexes[offset + k];

						commands.Add(new EagerCommand(sql, parameters, null, indexes));
						offset += statementCount;
					}
				}

				return commands;
			}

			public IEnumerator<T> GetEnumerator()
			{
				using var _ = ActivityService.Start(ActivityID.GetIEnumerable);

				var commands       = BuildCommands();
				var mainStepIndex  = _preambles.Length;
				var preambles      = new object?[_preambles.Length];
				var dataConnection = (DataConnection)_dataContext;

				DataReaderWrapper? mainReader = null;

				try
				{
					foreach (var command in commands)
					{
						var reader      = ExecEagerCommand(dataConnection, command);
						var stepIndexes = command.StepIndexes;
						var hasMain     = stepIndexes[stepIndexes.Length - 1] == mainStepIndex;

						if (!hasMain)
						{
							try
							{
								var dr = reader.DataReader!;

								for (var k = 0; k < stepIndexes.Length; k++)
								{
									preambles[stepIndexes[k]] = _preambles[stepIndexes[k]].MaterializeFromReader(_dataContext, _expressions, _parameters, preambles, dr);

									if (k < stepIndexes.Length - 1)
										dr.NextResult();
								}
							}
							finally
							{
								reader.Dispose();
							}
						}
						else
						{
							mainReader = reader;

							var dr = reader.DataReader!;

							for (var k = 0; k < stepIndexes.Length - 1; k++)
							{
								preambles[stepIndexes[k]] = _preambles[stepIndexes[k]].MaterializeFromReader(_dataContext, _expressions, _parameters, preambles, dr);
								dr.NextResult();
							}

							foreach (var item in _query.GetResultFromReader!(_dataContext, _expressions, _parameters, preambles, dr))
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

			public async IAsyncEnumerable<T> GetAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				var commands       = BuildCommands();
				var mainStepIndex  = _preambles.Length;
				var preambles      = new object?[_preambles.Length];
				var dataConnection = (DataConnection)_dataContext;

				DataReaderWrapper? mainReader = null;

				try
				{
					foreach (var command in commands)
					{
						var reader      = await ExecEagerCommandAsync(dataConnection, command, cancellationToken).ConfigureAwait(false);
						var stepIndexes = command.StepIndexes;
						var hasMain     = stepIndexes[stepIndexes.Length - 1] == mainStepIndex;

						if (!hasMain)
						{
							try
							{
								var dr = reader.DataReader!;

								for (var k = 0; k < stepIndexes.Length; k++)
								{
									preambles[stepIndexes[k]] = await _preambles[stepIndexes[k]].MaterializeFromReaderAsync(_dataContext, _expressions, _parameters, preambles, dr, cancellationToken).ConfigureAwait(false);

									if (k < stepIndexes.Length - 1)
										await dr.NextResultAsync(cancellationToken).ConfigureAwait(false);
								}
							}
							finally
							{
								await reader.DisposeAsync().ConfigureAwait(false);
							}
						}
						else
						{
							mainReader = reader;

							var dr = reader.DataReader!;

							for (var k = 0; k < stepIndexes.Length - 1; k++)
							{
								preambles[stepIndexes[k]] = await _preambles[stepIndexes[k]].MaterializeFromReaderAsync(_dataContext, _expressions, _parameters, preambles, dr, cancellationToken).ConfigureAwait(false);
								await dr.NextResultAsync(cancellationToken).ConfigureAwait(false);
							}

							await foreach (var item in _query.GetResultFromReader!(_dataContext, _expressions, _parameters, preambles, dr).WithCancellation(cancellationToken).ConfigureAwait(false))
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

			if (query.Queries.Count != 1)
				return null;

			foreach (var preamble in preambles)
				if (!preamble.CanCombine || preamble.GetCombinableStatement() == null)
					return null;

			return new CombinedEagerResultEnumerable<T>(dataContext, expressions, query, parameters, preambles);
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

			query.GetResultFromReader = (db, expr, ps, preambles, reader) =>
				new ExternalReaderResultEnumerable<T>(db, expr, query, ps, preambles, 0, mapper, reader);
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
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.ExecuteElementAsync))
			{
				var runner = dataContext.GetQueryRunner(query, dataContext, 0, expressions, ps, preambles);
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
