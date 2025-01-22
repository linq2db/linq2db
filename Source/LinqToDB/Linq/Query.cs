using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Linq
{
	using Builder;
	using Common;
	using Common.Logging;
	using Interceptors;
	using LinqToDB.Expressions;
	using LinqToDB.Expressions.ExpressionVisitors;
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using Tools;
	using Internal;

	public abstract class Query
	{
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,object?>                         GetElement      = null!;
		internal Func<IDataContext,IQueryExpressions,object?[]?,object?[]?,CancellationToken,Task<object?>> GetElementAsync = null!;

		#region Init

		internal readonly List<QueryInfo> Queries = new (1);

		public IReadOnlyCollection<QueryInfo> GetQueries()    => Queries;
		public bool                           IsFinalized     { get; internal set; }
		public SqlErrorExpression?            ErrorExpression { get; internal set; }

		internal abstract void Init(IBuildContext parseContext);

		internal Query(IDataContext dataContext)
		{
			ConfigurationID         = dataContext.ConfigurationID;
			ContextType             = dataContext.GetType();
			MappingSchema           = dataContext.MappingSchema;
			SqlOptimizer            = dataContext.GetSqlOptimizer(dataContext.Options);
			SqlProviderFlags        = dataContext.SqlProviderFlags;
			DataOptions             = dataContext.Options;
			InlineParameters        = dataContext.InlineParameters;
			IsEntityServiceProvided = dataContext is IInterceptable<IEntityServiceInterceptor> { Interceptor: {} };
		}

		#endregion

		#region Compare

		internal readonly int              ConfigurationID;
		internal readonly Type             ContextType;
		internal readonly MappingSchema    MappingSchema;
		internal readonly bool             InlineParameters;
		internal readonly ISqlOptimizer    SqlOptimizer;
		internal readonly SqlProviderFlags SqlProviderFlags;
		internal readonly DataOptions      DataOptions;
		internal readonly bool             IsEntityServiceProvided;

		protected bool Compare(IDataContext dataContext, IQueryExpressions expressions, [NotNullWhen(true)] out IQueryExpressions? matchedQueryExpressions)
		{
			matchedQueryExpressions = null;

			if (CompareInfo == null)
				return false;

			var result =
				ConfigurationID         == dataContext.ConfigurationID                                                  &&
				InlineParameters        == dataContext.InlineParameters                                                 &&
				ContextType             == dataContext.GetType()                                                        &&
				IsEntityServiceProvided == dataContext is IInterceptable<IEntityServiceInterceptor> { Interceptor: {} } &&
				CompareInfo.MainExpression.EqualsTo(expressions.MainExpression, dataContext);

			if (!result)
				return false;

			var runtimeExpressions = new RuntimeExpressionsContainer(expressions.MainExpression);
			matchedQueryExpressions = runtimeExpressions;

			if (CompareInfo.DynamicAccessors != null)
			{
				List<(int, Expression)>? testedExpressions = null;

				foreach (var da in CompareInfo.DynamicAccessors)
				{
					var current = da.AccessorFunc(dataContext, da.MappingSchema);
					result = da.Used.EqualsTo(current, dataContext);
					if (!result)
						return false;

					testedExpressions ??= new List<(int, Expression)>();
					testedExpressions.Add((da.ExpressionId, current));
				}

				if (testedExpressions != null)
				{
					foreach (var (expressionId, expression) in testedExpressions)
					{
						runtimeExpressions.AddExpression(expressionId, expression);
					}
				}
			}

			if (CompareInfo.ComparisionFunctions != null)
			{
				foreach (var (main, other) in CompareInfo.ComparisionFunctions)
				{
					var value1 = main(matchedQueryExpressions, dataContext, null);
					var value2 = other(matchedQueryExpressions, dataContext, null);
					result = value1 == null && value2 == null || value1 != null && value1.Equals(value2);

					if (!result)
						return false;
				}
			}

			return true;
		}

		List<ParameterAccessor>? _parameterAccessors;

		internal QueryCacheCompareInfo? CompareInfo;

		internal List<ParameterAccessor>? ParameterAccessors
		{
			get => _parameterAccessors;
			set => _parameterAccessors = value;
		}

		internal List<SqlParameter>? BuiltParameters;

		internal void AddParameterAccessor(ParameterAccessor accessor)
		{
			_parameterAccessors ??= new List<ParameterAccessor>();
			_parameterAccessors.Add(accessor);
		}

		internal void SetParametersAccessors(List<ParameterAccessor>? parameterAccessors)
		{
			_parameterAccessors = parameterAccessors;
		}

		#endregion

		#region Cache Support

		internal static readonly ConcurrentQueue<Action> CacheCleaners = new ();

		/// <summary>
		/// Clears query caches for all typed queries.
		/// </summary>
		public static void ClearCaches()
		{
			foreach (var cleaner in CacheCleaners)
			{
				cleaner();
			}
		}

		#endregion

		#region Eager Loading

		Preamble[]? _preambles;

		internal void SetPreambles(List<Preamble>? preambles)
		{
			_preambles = preambles?.ToArray();
		}

		internal bool IsAnyPreambles()
		{
			return _preambles?.Length > 0;
		}

		internal object?[]? InitPreambles(IDataContext dc, IQueryExpressions expressions, object?[]? ps)
		{
			if (_preambles == null)
				return null;

			var preambles = new object[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = _preambles[i].Execute(dc, expressions, ps, preambles);
			}

			return preambles;
		}

		internal async Task<object?[]?> InitPreamblesAsync(IDataContext dc, IQueryExpressions expressions, object?[]? ps, CancellationToken cancellationToken)
		{
			if (_preambles == null)
				return null;

			var preambles = new object[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = await _preambles[i].ExecuteAsync(dc, expressions, ps, preambles, cancellationToken).ConfigureAwait(false);
			}

			return preambles;
		}

		#endregion
	}

	public class Query<T> : Query
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
			readonly object _syncCache    = new ();
			// lock for query priority modification
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
			public void TryAdd(IDataContext dataContext, Query<T> query, IQueryExpressions queryExpression, QueryFlags queryFlags, DataOptions dataOptions)
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
				_queryCache.TryAdd(dataContext, query, expressions, queryFlags, dataOptions);
			}

			return query;
		}

		internal static Query<T> CreateQuery(ExpressionTreeOptimizationContext optimizationContext, ParametersContext parametersContext, IDataContext dataContext, ref IQueryExpressions expressions)
		{
			var linqOptions = optimizationContext.DataContext.Options.LinqOptions;

			if (linqOptions.GenerateExpressionTest)
			{
				var testFile = new ExpressionTestGenerator(dataContext).GenerateSource(expressions.MainExpression);

				if (dataContext.GetTraceSwitch().TraceInfo)
					dataContext.WriteTraceLine(
						$"Expression test code generated: \'{testFile}\'.",
						dataContext.GetTraceSwitch().DisplayName,
						TraceLevel.Info);
			}

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
			catch (Exception)
			{
				if (!linqOptions.GenerateExpressionTest)
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

	public class QueryInfo : IQueryContext
	{
		#if DEBUG

		// For debugging purposes only in multithreading environment
		static          long _uniqueIdCounter;
		public readonly long UniqueId;

		public QueryInfo()
		{
			UniqueId = Interlocked.Increment(ref _uniqueIdCounter);
		}

		#endif

		public SqlStatement    Statement       { get; set; } = null!;
		public object?         Context         { get; set; }
		public bool            IsContinuousRun { get; set; }
		public AliasesContext? Aliases         { get; set; }
		public DataOptions?    DataOptions     { get; set; }
	}

	sealed class ParameterAccessor
	{
		public ParameterAccessor(
			int                                                       accessorId,
			Func<IQueryExpressions, IDataContext?,object?[]?,object?> clientValueAccessor,
			Func<object?,object?>?                                    clientToProviderConverter,
			Func<object?,object?>?                                    itemAccessor,
			Func<object?,DbDataType>?                                 dbDataTypeAccessor,
			SqlParameter                                              sqlParameter)
		{
			AccessorId                = accessorId;
			ClientToProviderConverter = clientToProviderConverter;
			ClientValueAccessor       = clientValueAccessor;
			ItemAccessor              = itemAccessor;
			DbDataTypeAccessor        = dbDataTypeAccessor;
			SqlParameter              = sqlParameter;
		}

		public readonly int                                                      AccessorId;
		public readonly Func<IQueryExpressions,IDataContext?,object?[]?,object?> ClientValueAccessor;
		public readonly Func<object?,object?>?                                   ClientToProviderConverter;
		public readonly Func<object?,object?>?                                   ItemAccessor;
		public readonly Func<object?,DbDataType>?                                DbDataTypeAccessor;
		public readonly SqlParameter                                             SqlParameter;
#if DEBUG
		public Expression<Func<IQueryExpressions,IDataContext?,object?[]?,object?>>? AccessorExpr;
#endif
	}
}
