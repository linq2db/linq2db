using System;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.Logging;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq;
using LinqToDB.Metrics;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	public sealed class Query<T> : Query
	{
		#region Init

		public Query(IDataContext dataContext)
			: base(dataContext)
		{
			DoNotCache = NoLinqCache.IsNoCache;
		}

		internal override void Init(IBuildContext parseContext)
		{
			var statement = parseContext.GetResultStatement();

			QueryInfo = new QueryInfo
			{
				Statement          = statement,
			};
		}

		#endregion

		#region Properties & Fields

		internal bool DoNotCache;

		internal IQueryExpressions? CompiledExpressions;

		// The compiled query that built this query, through which the IQueryable a call of it returns resolves the query again.
		internal ICompiledQuerySource<T>? CompiledSource;

		// A query built for a compiled query is kept under a key that does not tell query interceptor instances apart. An
		// interceptor without IInterceptorCacheKey may rewrite what the dynamic accessors return differently for each instance:
		// they are re-evaluated for the executing context first, as the query cache does, and their results supply the
		// parameter values. Otherwise the query is reused as it is.
		internal bool TryReuse(IDataContext dataContext, object?[]? parameterValues, ref QueryCacheCompareInfo.DynamicAccessorResults dynamicAccessorResults, out IQueryExpressions expressions)
		{
			expressions = CompiledExpressions!;

			if (CompareInfo?.DynamicAccessors == null || !dataContext.HasUntrustedQueryInterceptor())
				return true;

			if (!Revalidate(dataContext, expressions.MainExpression, parameterValues, ref dynamicAccessorResults, out var matched))
				return false;

			expressions = matched;

			return true;
		}

		internal Func<IDataContext,IQueryExpressions,SqlCommandExecutionContext?,IResultEnumerable<T>> GetResultEnumerable = null!;

		// Set only for combinable SELECT queries (see SetRunQuery): materializes this query's rows from an
		// externally-opened reader already positioned at its result set — used by combined multi-result-set eager loading
		// so N child collection queries run as one command and each result set is mapped by its own query's mapper.
		internal Func<IDataContext,IQueryExpressions,SqlCommandExecutionContext?,DbDataReader,IResultEnumerable<T>>? GetResultFromReader;

		// Centralizes the eager-load enumerable decision shared by the ExpressionQuery enumeration paths: use the combined
		// multi-result-set executor when eligible, else the sequential InitHarvesters + GetResultEnumerable path. Returns the
		// harvesters and whether the combined path was taken, so the caller sets its Preambles field ONLY for the sequential
		// path — the combined enumerable owns its harvesters, and GetForEachUntilAsync relies on Preambles being untouched there.
		internal (IResultEnumerable<T> Enumerable, SqlCommandExecutionContext? Preambles, bool Combined) GetEagerEnumerable(
			IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters)
		{
			var combined = QueryRunner.TryGetCombinedEagerEnumerable<T>(this, dataContext, expressions, parameters);

			if (combined != null)
				return (combined, null, true);

			var harvesters = InitHarvesters(dataContext, expressions, parameters);

			return (GetResultEnumerable(dataContext, expressions, harvesters), harvesters, false);
		}

		// Async sibling of GetEagerEnumerable.
		internal async Task<(IResultEnumerable<T> Enumerable, SqlCommandExecutionContext? Preambles, bool Combined)> GetEagerEnumerableAsync(
			IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, CancellationToken cancellationToken)
		{
			var combined = QueryRunner.TryGetCombinedEagerEnumerable<T>(this, dataContext, expressions, parameters);

			if (combined != null)
				return (combined, null, true);

			var harvesters = await InitHarvestersAsync(dataContext, expressions, parameters, cancellationToken).ConfigureAwait(false);

			return (GetResultEnumerable(dataContext, expressions, harvesters), harvesters, false);
		}

		#endregion

		#region Query

		// Note: no per-T CacheCleaners registration here.
		// QueryCache.Default registers a single ClearAll cleaner with Query.CacheCleaners,
		// so Query.ClearCaches() empties the whole cache without needing a cleaner per
		// closed generic type. ClearCache() below is still callable directly.

		/// <summary>
		/// Empties LINQ query cache for <typeparamref name="T"/> entity type.
		/// </summary>
		public static void ClearCache() => QueryCache.Default.ClearForType(typeof(T));

		/// <summary>
		/// Count of queries which has not been found in cache.
		/// </summary>
		public static long CacheMissCount => QueryCache.Default.GetMissCount(typeof(T));

		// exposedStageResult: the ExposedQuery stage's result for expr when it already ran in this execution (see
		// IsRewrittenOnExpose); used instead of running the interceptor again if exposing expr changes nothing.
		static Expression ExposeAndPrepareExpression(Expression expr, IDataContext dataContext, ExpressionTreeOptimizationContext optimizationContext, Expression? exposedStageResult)
		{
			var        beforeExpose = expr;
			Expression exposed;
			var        iteration    = 0;
			do
			{
				exposed = ExpressionBuilder.ExposeExpression(beforeExpose, dataContext, optimizationContext, null,
					optimizeConditions: true, compactBinary: false /* binary already compacted by AggregateExpression*/);

				if (iteration > 0 && ReferenceEquals(beforeExpose, exposed))
				{
					// no changes, no need to continue
					break;
				}

				if (dataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
				{
					var processed = iteration == 0 && exposedStageResult != null && ReferenceEquals(exposed, expr)
						? exposedStageResult
						: interceptor.ProcessExpression(exposed, new QueryExpressionArgs(dataContext, exposed, QueryExpressionArgs.ExpressionKind.ExposedQuery));

					if (!ReferenceEquals(processed, exposed))
					{
						// Doe exposing again after interceptor processing
						exposed      = processed;
						beforeExpose = exposed;

						if (++iteration > 10)
						{
							// guard from infinite loop
							break;
						}

						continue;
					}
				}

				break;
			} while (true);

			return exposed;
		}

		// A query found with the unexposed tree was cached by a context whose ExposedQuery stage left that tree unchanged. A
		// query interceptor without IInterceptorCacheKey may rewrite it for this context: the stage runs here, and its result
		// is kept for the expose that follows when it does.
		static bool IsRewrittenOnExpose(IDataContext dataContext, Expression expr, out Expression? exposedStageResult)
		{
			exposedStageResult = null;

			if (!dataContext.HasUntrustedQueryInterceptor())
				return false;

			var interceptor = ((IInterceptable<IQueryExpressionInterceptor>)dataContext).Interceptor!;

			exposedStageResult = interceptor.ProcessExpression(expr, new QueryExpressionArgs(dataContext, expr, QueryExpressionArgs.ExpressionKind.ExposedQuery));

			// a new tree equal to expr leaves the found query valid
			return !exposedStageResult.EqualsTo(expr);
		}

		public static Query<T> GetQuery(IDataContext dataContext, ref IQueryExpressions expressions, out bool dependsOnParameters)
		{
			var query = GetQuery(dataContext, ref expressions, out dependsOnParameters, out var isRewritten);

			dependsOnParameters |= isRewritten;

			return query;
		}

		/// <param name="isRewritten">The returned expressions carry a query interceptor's rewrite: the query may be kept
		/// together with them, but they must not replace the tree the caller holds, which has to outlive the interceptor.</param>
		internal static Query<T> GetQuery(IDataContext dataContext, ref IQueryExpressions expressions, out bool dependsOnParameters, out bool isRewritten)
		{
			using var mt = ActivityService.Start(ActivityID.GetQueryTotal);

			isRewritten = false;

			ExpressionTreeOptimizationContext optimizationContext;
			DataOptions                       dataOptions;
			var                               queryFlags = QueryFlags.None;
			Query<T>?                         query;
			bool                              useCache;
			Expression?                       exposedStageResult = null;

			using (ActivityService.Start(ActivityID.GetQueryFind))
			{
				var expr = expressions.MainExpression;

				using (ActivityService.Start(ActivityID.GetQueryFindExpose))
				{
					optimizationContext = new ExpressionTreeOptimizationContext(dataContext);

					// I hope fast tree optimization for unbalanced Binary Expressions. See StackUseTests.
					//
					expr = BinaryExpressionAggregatorVisitor.Instance.Visit(expr);

					dependsOnParameters = false;

					if (dataContext is IExpressionPreprocessor preprocessor)
						expr = preprocessor.ProcessExpression(expr);

					// The cache key below is built from the rewritten tree, so this runs before the lookup and
					// therefore on every execution. Keying the cache on the pre-interception tree would let a hit
					// skip the rewrite, but only by retaining both trees - one to match on, one to run.
					if (dataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
					{
						var intercepted = interceptor.ProcessExpression(expr, new QueryExpressionArgs(dataContext, expr, QueryExpressionArgs.ExpressionKind.Query));

						// the rewritten tree belongs to the current interceptors: an IQueryable must not keep it in place
						// of its own tree; a new tree equal to expr is not a rewrite
						if (!intercepted.EqualsTo(expr))
						{
							isRewritten = true;
							expr        = intercepted;
						}
					}
				}

				dataOptions = dataContext.Options;

				useCache = !dataOptions.LinqOptions.DisableQueryCache;

				var runtimeExpressions = ReferenceEquals(expr, expressions.MainExpression) ? expressions : new RuntimeExpressionsContainer(expr);

				if (useCache)
				{
					queryFlags = dataContext.GetQueryFlags();
					using (ActivityService.Start(ActivityID.GetQueryFindFind))
					{
						if (QueryCache.Default.TryFind(typeof(T), dataContext, runtimeExpressions, queryFlags, out var foundQuery, out var matchedExpressions)
							&& !IsRewrittenOnExpose(dataContext, expr, out exposedStageResult))
						{
							expressions = matchedExpressions;
							return (Query<T>)foundQuery;
						}
					}
				}

				// Expose expression, call all needed invocations.
				// After execution there should be no constants which contains IDataContext reference, no constants with ExpressionQueryImpl
				// Parameters with SqlQueryDependentAttribute will be transferred to constants
				// No LambdaExpressions which are located in constants, they will be expanded and injected into tree
				//
				var exposed = ExposeAndPrepareExpression(expr, dataContext, optimizationContext, exposedStageResult);

				// simple trees do not mutate
				var isExposed = !ReferenceEquals(exposed, expr);

				expr = exposed;
				var currentQueries = new RuntimeExpressionsContainer(expr);
				expressions = currentQueries;

				// without the query cache too: the exposed tree may carry the query interceptor's rewrite, which the IQueryable
				// must not keep in place of its own tree
				if (isExposed && !useCache && dataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: not null })
					isRewritten = true;

				if (isExposed && useCache)
				{
					dependsOnParameters = true;

					queryFlags |= QueryFlags.ExpandedQuery;

					// search again
					using (ActivityService.Start(ActivityID.GetQueryFindFind))
					{
						if (QueryCache.Default.TryFind(typeof(T), dataContext, currentQueries, queryFlags, out var foundQuery, out var matchedExpressions))
						{
							expressions = matchedExpressions;
							return (Query<T>)foundQuery;
						}
					}
				}

				if (useCache)
				{
					// Cache missed, Build query
					//
					QueryCache.Default.IncrementMissCount(typeof(T));
				}
			}

			using (var mc = ActivityService.Start(ActivityID.GetQueryCreate))
			{
				query = CreateQuery(
					optimizationContext,
					 new ParametersContext(expressions, optimizationContext, dataContext),
					dataContext,
					ref expressions
				);
			}

			if (useCache && !query.DoNotCache)
			{
				QueryCache.Default.TryAdd(typeof(T), dataContext, query, expressions, queryFlags);
			}

			return query;
		}

		internal static Query<T> CreateQuery(ExpressionTreeOptimizationContext optimizationContext, ParametersContext parametersContext, IDataContext dataContext, ref IQueryExpressions expressions)
		{
			var query = new Query<T>(dataContext);

			try
			{
				var validateSubqueries = !ExpressionBuilder.NeedsSubqueryValidation(dataContext);
				query = new ExpressionBuilder(query, validateSubqueries, optimizationContext, parametersContext, dataContext, expressions.MainExpression, null).Build<T>(ref expressions);
				if (query.ErrorExpression != null)
				{
					if (!validateSubqueries)
					{
						query = new Query<T>(dataContext);
						query = new ExpressionBuilder(query, true, optimizationContext, parametersContext, dataContext, expressions.MainExpression, null).Build<T>(ref expressions);
					}

					if (query.ErrorExpression != null)
						throw query.ErrorExpression.CreateException();
				}
			}
			catch
			{
				var linqOptions = optimizationContext.DataContext.Options.LinqOptions;

				if (linqOptions.GenerateExpressionTest)
				{
					var testFile = ExpressionTestGenerator.GenerateSourceFile(dataContext, expressions.MainExpression);

					if (dataContext.GetTraceSwitch().TraceInfo)
					{
						dataContext.WriteTraceLine(
							$"Expression test code generated: \'{testFile}\'.",
							dataContext.GetTraceSwitch().DisplayName,
							TraceLevel.Info);
					}
				}
				else
				{
					dataContext.WriteTraceLine(
						"""
						To generate test code to diagnose the problem set 'LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true'.
						Or specify LINQ options during 'DataContextOptions' building 'options.UseGenerateExpressionTest(true)'
						""",
						dataContext.GetTraceSwitch().DisplayName,
						TraceLevel.Error);
				}

				throw;
			}

			return query;
		}

		#endregion
	}
}
