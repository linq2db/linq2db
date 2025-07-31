using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.Logging;
using LinqToDB.Linq;
using LinqToDB.Metrics;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Internal.Linq
{
	public sealed class Query<T> : Query
	{
		#region Init

		internal Query(IDataContext dataContext)
			: base(dataContext)
		{
			DoNotCache = NoLinqCache.IsNoCache;
		}

		internal override void Init(IBuildContext parseContext)
		{
			var statement = parseContext.GetResultStatement();

			Queries.Add(new QueryInfo
			{
				Statement          = statement,
			});
		}

		#endregion

		#region Properties & Fields

		internal bool DoNotCache;

		internal IQueryExpressions? CompiledExpressions;

		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,IResultEnumerable<T>> GetResultEnumerable = null!;

		#endregion

		#region Query cache

		sealed class QueryCache
		{
			sealed class QueryCacheEntry
			{
				public QueryCacheEntry(Query<T> query, QueryFlags queryFlags)
				{
					// query doesn't have GetHashCode now, so we cannot precalculate hashcode to speed-up search
					Query       = query;
					QueryFlags  = queryFlags;
				}

				public Query<T>   Query      { get; }
				public QueryFlags QueryFlags { get; }

				// accepts components to avoid QueryCacheEntry allocation for cached query
				public bool Compare(IDataContext context, IQueryExpressions queryExpression, QueryFlags queryFlags, [NotNullWhen(true)] out IQueryExpressions? matchedQueryExpressions)
				{
					matchedQueryExpressions = null;
					return QueryFlags == queryFlags && Query.Compare(context, queryExpression, out matchedQueryExpressions);
				}
			}

			// lock for cache instance modification
			readonly Lock   _syncCache    = new ();
			// lock for query priority modification
			// NB: remains an `object` for use with `Monitor.TryEnter()` instead of `lock()`
			readonly object _syncPriority = new ();

			// stores all cached queries
			// when query added or removed from cache, query and priority arrays recreated
			QueryCacheEntry[] _cache = [];

			// stores ordered list of query indexes for Find operation
			int[] _indexes = [];

			// version of cache, increased after each recreation of _cache instance
			int _version;

			/// <summary>
			/// Count of queries which has not been found in cache.
			/// </summary>
			public long CacheMissCount;

			/// <summary>
			/// LINQ query max cache size (per entity type).
			/// </summary>
			const int CacheSize = 100;

			/// <summary>
			/// Empties LINQ query cache.
			/// </summary>
			public void Clear()
			{
				if (_cache.Length > 0)
					lock (_syncCache)
						if (_cache.Length > 0)
						{
							_cache   = [];
							_indexes = [];
							_version++;
						}
			}

			/// <summary>
			/// Adds query to cache if it is not cached already.
			/// </summary>
			public void TryAdd(IDataContext dataContext, Query<T> query, IQueryExpressions queryExpression, QueryFlags queryFlags)
			{
				// because Add is less frequent operation than Find, it is fine to have put bigger locks here
				QueryCacheEntry[] cache;
				int               version;

				lock (_syncCache)
				{
					cache   = _cache;
					version = _version;
				}

				for (var i = 0; i < cache.Length; i++)
					if (cache[i].Compare(dataContext, queryExpression, queryFlags, out _))
						// already added by another thread
						return;

				lock (_syncCache)
				{
					// TODO : IT : check
					var priorities   = _indexes;
					var versionsDiff = _version - version;

					if (versionsDiff > 0)
					{
						cache = _cache;

						// check only added queries, each version could add 1 query to first position, so we
						// test only first N queries
						for (var i = 0; i < cache.Length && i < versionsDiff; i++)
							if (cache[i].Compare(dataContext, queryExpression, queryFlags, out _))
								// already added by another thread
								return;
					}

					// create new cache instance and reorder items according to priorities to improve Find without
					// reorder lock
					var newCache      = new QueryCacheEntry[cache.Length == CacheSize ? CacheSize : cache.Length + 1];
					var newPriorities = new int[newCache.Length];

					newCache[0]      = new QueryCacheEntry(query, queryFlags);
					newPriorities[0] = 0;

					for (var i = 1; i < newCache.Length; i++)
					{
						newCache[i]      = cache[i - 1];
						newPriorities[i] = i;
					}

					_cache   = newCache;
					_indexes = newPriorities;
					_version = version;
				}
			}

			/// <summary>
			/// Search for query in cache and of found, try to move it to better position in cache.
			/// </summary>
			public (Query<T> query, IQueryExpressions expressions)? Find(IDataContext dataContext, IQueryExpressions expressions, QueryFlags queryFlags, bool onlyExpanded)
			{
				QueryCacheEntry[] cache;
				int[]             indexes;
				int               version;

				lock (_syncCache)
				{
					cache   = _cache;
					version = _version;
					indexes = _indexes;
				}

				var allowReordering = Monitor.TryEnter(_syncPriority);

				try
				{
					for (var i = 0; i < cache.Length; i++)
					{
						// if we have reordering lock, we can enumerate queries in priority order
						var idx = allowReordering ? indexes[i] : i;

						if (onlyExpanded == ((cache[idx].QueryFlags & QueryFlags.ExpandedQuery) != 0))
						{
							if (cache[idx].Compare(dataContext, expressions, queryFlags, out var foundQueryExpressions))
							{
								// do reorder only if it is not blocked and cache wasn't replaced by new one
								if (i > 0 && version == _version && allowReordering)
								{
									(indexes[i - 1], indexes[i]) = (indexes[i], indexes[i - 1]);
								}

								return (cache[idx].Query, foundQueryExpressions);
							}
						}
					}
				}
				finally
				{
					if (allowReordering)
						Monitor.Exit(_syncPriority);
				}

				return default;
			}

			public void TriggerCacheMiss()
			{
				Interlocked.Increment(ref CacheMissCount);
			}
		}

		#endregion

		#region Query

		private static readonly QueryCache _queryCache = new ();

		static Query()
		{
			CacheCleaners.Enqueue(ClearCache);
		}

		/// <summary>
		/// Empties LINQ query cache for <typeparamref name="T"/> entity type.
		/// </summary>
		public static void ClearCache() => _queryCache.Clear();

		public static long CacheMissCount => _queryCache.CacheMissCount;

		static Expression ExposeAndPrepareExpression(Expression expr, IDataContext dataContext, ExpressionTreeOptimizationContext optimizationContext)
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
					var processed = interceptor.ProcessExpression(exposed, new QueryExpressionArgs(dataContext, exposed, QueryExpressionArgs.ExpressionKind.ExposedQuery));
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

		public static Query<T> GetQuery(IDataContext dataContext, ref IQueryExpressions expressions, out bool dependsOnParameters)
		{
			using var mt = ActivityService.Start(ActivityID.GetQueryTotal);

			ExpressionTreeOptimizationContext optimizationContext;
			DataOptions                       dataOptions;
			var                               queryFlags = QueryFlags.None;
			Query<T>?                         query;
			bool                              useCache;

			using (ActivityService.Start(ActivityID.GetQueryFind))
			{
				var expr = expressions.MainExpression;

				using (ActivityService.Start(ActivityID.GetQueryFindExpose))
				{
					optimizationContext = new ExpressionTreeOptimizationContext(dataContext);

					// I hope fast tree optimization for unbalanced Binary Expressions. See Issue447Tests.
					//
					expr = BinaryExpressionAggregatorVisitor.Instance.Visit(expr);

					dependsOnParameters = false;

					if (dataContext is IExpressionPreprocessor preprocessor)
						expr = preprocessor.ProcessExpression(expr);

					if (dataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
						expr = interceptor.ProcessExpression(expr, new QueryExpressionArgs(dataContext, expr, QueryExpressionArgs.ExpressionKind.Query));
				}

				dataOptions = dataContext.Options;

				useCache = !dataOptions.LinqOptions.DisableQueryCache;

				var runtimeExpressions = ReferenceEquals(expr, expressions.MainExpression) ? expressions : new RuntimeExpressionsContainer(expr);

				if (useCache)
				{
					queryFlags = dataContext.GetQueryFlags();
					using (ActivityService.Start(ActivityID.GetQueryFindFind))
					{
						var found = _queryCache.Find(dataContext, runtimeExpressions, queryFlags, false);
						if (found != null)
						{
							expressions = found.Value.expressions;
							return found.Value.query;
						}
					}
				}

				// Expose expression, call all needed invocations.
				// After execution there should be no constants which contains IDataContext reference, no constants with ExpressionQueryImpl
				// Parameters with SqlQueryDependentAttribute will be transferred to constants
				// No LambdaExpressions which are located in constants, they will be expanded and injected into tree
				//
				var exposed = ExposeAndPrepareExpression(expr, dataContext, optimizationContext);

				// simple trees do not mutate
				var isExposed = !ReferenceEquals(exposed, expr);

				expr = exposed;
				var currentQueries = new RuntimeExpressionsContainer(expr);
				expressions = currentQueries;

				if (isExposed && useCache)
				{
					dependsOnParameters = true;

					queryFlags |= QueryFlags.ExpandedQuery;

					// search again
					using (ActivityService.Start(ActivityID.GetQueryFindFind))
					{
						var findResult = _queryCache.Find(dataContext, currentQueries, queryFlags, true);
						if (findResult != null)
						{
							expressions = findResult.Value.expressions;
							return findResult.Value.query;
						}
					}
				}

				if (useCache)
				{
					// Cache missed, Build query
					//
					_queryCache.TriggerCacheMiss();
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
				_queryCache.TryAdd(dataContext, query, expressions, queryFlags);
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
					var testFile = new ExpressionTestGenerator(dataContext).GenerateSource(expressions.MainExpression);

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
