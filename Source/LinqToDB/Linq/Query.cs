using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using Tools;
	using Infrastructure;

	public abstract class Query
	{
		internal Func<IDataContext,Expression,object?[]?,object?[]?,object?>                         GetElement      = null!;
		internal Func<IDataContext,Expression,object?[]?,object?[]?,CancellationToken,Task<object?>> GetElementAsync = null!;

		#region Init

		internal readonly List<QueryInfo> Queries = new (1);

		public IReadOnlyCollection<QueryInfo> GetQueries()    => Queries;
		public bool                           IsFinalized     { get; internal set; }
		public SqlErrorExpression?            ErrorExpression { get; internal set; }

		internal abstract void Init(IBuildContext parseContext);

		internal Query(IDataContext dataContext, Expression? expression)
		{
			ConfigurationID         = dataContext.ConfigurationID;
			ContextType             = dataContext.GetType();
			Expression              = expression;
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
		internal          Expression?      Expression;
		internal readonly MappingSchema    MappingSchema;
		internal readonly bool             InlineParameters;
		internal readonly ISqlOptimizer    SqlOptimizer;
		internal readonly SqlProviderFlags SqlProviderFlags;
		internal readonly DataOptions      DataOptions;
		internal readonly bool             IsEntityServiceProvided;

		protected bool Compare(IDataContext dataContext, Expression expr)
		{
			return
				ConfigurationID         == dataContext.ConfigurationID                                                  &&
				InlineParameters        == dataContext.InlineParameters                                                 &&
				ContextType             == dataContext.GetType()                                                        &&
				IsEntityServiceProvided == dataContext is IInterceptable<IEntityServiceInterceptor> { Interceptor: {} } &&
				Expression!.EqualsTo(expr, dataContext, _parametersDuplicates, _dynamicAccessors);
		}

		List<Expression>? _parameterized;

		List<(Func<Expression, IDataContext?, object?[]?, object?> main, Func<Expression, IDataContext?, object?[]?, object?> substituted)>? _parametersDuplicates;
		List<(Expression used, MappingSchema mappingSchema, Func<IDataContext, MappingSchema, Expression> accessorFunc)>?                    _dynamicAccessors;
		List<ParameterAccessor>?                                                                                                             _parameterAccessors;

		internal List<ParameterAccessor>? ParameterAccessors => _parameterAccessors;

		internal void AddParameterAccessor(ParameterAccessor accessor)
		{
			_parameterAccessors ??= new List<ParameterAccessor>();
			_parameterAccessors.Add(accessor);
		}

		internal bool IsFastCacheable => _dynamicAccessors == null;

		internal void SetParameterized(List<Expression>? parameterized)
		{
			_parameterized = parameterized;
		}

		internal void SetParametersAccessors(List<ParameterAccessor>? parameterAccessors)
		{
			_parameterAccessors = parameterAccessors;
		}

		internal void SetParametersDuplicates(List<(Func<Expression, IDataContext?, object?[]?, object?> main, Func<Expression, IDataContext?, object?[]?, object?> substituted)>? parametersDuplicates)
		{
			_parametersDuplicates = parametersDuplicates;
		}

		public void SetDynamicAccessors(List<(Expression used, MappingSchema mappingSchema, Func<IDataContext, MappingSchema, Expression> accessorFunc)>? dynamicAccessors)
		{
			_dynamicAccessors = dynamicAccessors;
		}

		public Expression? GetExpression() => Expression;

		protected Expression ReplaceParameterizedAndClosures(Expression expression, List<Expression>? newParameterized)
		{
			var result = expression.Transform((parameterized: _parameterized, newParameterized), static (ctx, e) =>
			{
				if (ctx.parameterized != null)
				{
					var idx = ctx.parameterized.IndexOf(e);
					if (idx >= 0)
					{
						var newValue = ctx.newParameterized![idx];

						if (newValue is not ConstantPlaceholderExpression)
						{
							newValue                 = new ConstantPlaceholderExpression(e.Type);
							ctx.newParameterized[idx] = newValue;
						}

						return new TransformInfo(newValue);
					}
				}

				if (e.NodeType == ExpressionType.Constant)
				{
					var c = (ConstantExpression)e;

					if (ExpressionCacheHelpers.ShouldRemoveConstantFromCache(c))
					{
						var replacement = new ConstantPlaceholderExpression(e.Type);
						return new TransformInfo(replacement);
					}
				}

				return new TransformInfo(e);
			});

			return result;
		}

		/// <summary>
		/// Replaces closure references by constants
		/// </summary>
		protected void PrepareForCaching()
		{
			List<Expression>? newParameterized = null;

			if (Expression != null)
			{
				newParameterized = _parameterized?.ToList();

				var result = ReplaceParameterizedAndClosures(Expression, newParameterized);
				Expression = result;
			}

			if (newParameterized != null)
				_parameterized = newParameterized;
		}

		internal void ClearDynamicQueryableInfo()
		{
			_dynamicAccessors = null;
		}

		#endregion

		#region Helpers

		ConcurrentDictionary<Type,Func<object,object>>? _enumConverters;

		internal object GetConvertedEnum(Type valueType, object value)
		{
			_enumConverters ??= new ();

			if (!_enumConverters.TryGetValue(valueType, out var converter))
			{
				var toType    = Converter.GetDefaultMappingFromEnumType(MappingSchema, valueType)!;
				var convExpr  = MappingSchema.GetConvertExpression(valueType, toType)!;
				var convParam = Expression.Parameter(typeof(object));

				var lex = Expression.Lambda<Func<object, object>>(
					Expression.Convert(convExpr.GetBody(Expression.Convert(convParam, valueType)), typeof(object)),
					convParam);

				converter = lex.CompileExpression();

				_enumConverters.GetOrAdd(valueType, converter);
			}

			return converter(value);
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

		internal int PreamblesCount()
		{
			return _preambles?.Length ?? 0;
		}

		internal object?[]? InitPreambles(IDataContext dc, Expression rootExpression, object?[]? ps)
		{
			if (_preambles == null)
				return null;

			var preambles = new object[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = _preambles[i].Execute(dc, rootExpression, ps, preambles);
			}

			return preambles;
		}

		internal async Task<object?[]?> InitPreamblesAsync(IDataContext dc, Expression rootExpression, object?[]? ps, CancellationToken cancellationToken)
		{
			if (_preambles == null)
				return null;

			var preambles = new object[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = await _preambles[i].ExecuteAsync(dc, rootExpression, ps, preambles, cancellationToken).ConfigureAwait(false);
			}

			return preambles;
		}

		#endregion
	}

	public class Query<T> : Query
	{
		#region Init

		internal Query(IDataContext dataContext, Expression? expression)
			: base(dataContext, expression)
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

		internal Func<IDataContext,Expression,object?[]?,object?[]?,IResultEnumerable<T>>                GetResultEnumerable = null!;

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
				public bool Compare(IDataContext context, Expression queryExpression, QueryFlags queryFlags)
				{
					return QueryFlags == queryFlags && Query.Compare(context, queryExpression);
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
			public void TryAdd(IDataContext dataContext, Query<T> query, Expression queryExpression, QueryFlags queryFlags, DataOptions dataOptions)
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
					if (cache[i].Compare(dataContext, queryExpression, queryFlags))
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
							if (cache[i].Compare(dataContext, query.Expression!, queryFlags))
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
			public Query<T>? Find(IDataContext dataContext, Expression expr, QueryFlags queryFlags, bool onlyExpanded)
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
							if (cache[idx].Compare(dataContext, expr, queryFlags))
							{
								// do reorder only if it is not blocked and cache wasn't replaced by new one
								if (i > 0 && version == _version && allowReordering)
								{
									(indexes[i - 1], indexes[i]) = (indexes[i], indexes[i - 1]);
								}

								return cache[idx].Query;
							}
						}
					}
				}
				finally
				{
					if (allowReordering)
						Monitor.Exit(_syncPriority);
				}

				return null;
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

		public static Query<T> GetQuery(IDataContext dataContext, ref Expression expr, out bool dependsOnParameters)
		{
			using var mt = ActivityService.Start(ActivityID.GetQueryTotal);

			ExpressionTreeOptimizationContext optimizationContext;
			DataOptions                       dataOptions;
			var                               queryFlags = QueryFlags.None;
			Query<T>?                         query;
			bool                              useCache;

			using (ActivityService.Start(ActivityID.GetQueryFind))
			{
				using (ActivityService.Start(ActivityID.GetQueryFindExpose))
				{
					optimizationContext = new ExpressionTreeOptimizationContext(dataContext);

					// I hope fast tree optimization for unbalanced Binary Expressions. See Issue447Tests.
					//
					expr = optimizationContext.AggregateExpression(expr);

					dependsOnParameters = false;

					if (dataContext is IExpressionPreprocessor preprocessor)
						expr = preprocessor.ProcessExpression(expr);
				}

				dataOptions = dataContext.Options;

				useCache = !dataOptions.LinqOptions.DisableQueryCache;

				if (useCache)
				{
					queryFlags = dataContext.GetQueryFlags();
					using (ActivityService.Start(ActivityID.GetQueryFindFind))
						query = _queryCache.Find(dataContext, expr, queryFlags, false);

					if (query != null)
						return query;
				}

				// Expose expression, call all needed invocations.
				// After execution there should be no constants which contains IDataContext reference, no constants with ExpressionQueryImpl
				// Parameters with SqlQueryDependentAttribute will be transferred to constants
				// No LambdaExpressions which are located in constants, they will be expanded and injected into tree
				//
				var exposed = ExpressionBuilder.ExposeExpression(expr, dataContext, optimizationContext, null,
					optimizeConditions : true, compactBinary : false /* binary already compacted by AggregateExpression*/);

				if (dataContext is IInterceptable<IQueryExpressionInterceptor> { Interceptor: { } interceptor })
					exposed = interceptor.ProcessExpression(exposed, new QueryExpressionArgs(dataContext, exposed, QueryExpressionArgs.ExpressionKind.Query));

				// simple trees do not mutate
				var isExposed = !ReferenceEquals(exposed, expr);

				expr = exposed;
				if (isExposed && useCache)
				{
					dependsOnParameters = true;

					queryFlags |= QueryFlags.ExpandedQuery;

					// search again
					using (ActivityService.Start(ActivityID.GetQueryFindFind))
						query = _queryCache.Find(dataContext, expr, queryFlags, true);

					if (query != null)
						return query;
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
					 new ParametersContext(expr, optimizationContext, dataContext),
					dataContext,
					expr
				);
			}

			if (useCache && !query.DoNotCache)
			{
				// All non-value type expressions and parameterized expressions will be transformed to ConstantPlaceholderExpression. It prevents from caching big reference classes in cache.
				//
				query.PrepareForCaching();

				_queryCache.TryAdd(dataContext, query, expr, queryFlags, dataOptions);
			}

			return query;
		}

		internal static Query<T> CreateQuery(ExpressionTreeOptimizationContext optimizationContext, ParametersContext parametersContext, IDataContext dataContext, Expression expr)
		{
			var linqOptions = optimizationContext.DataContext.Options.LinqOptions;

			if (linqOptions.GenerateExpressionTest)
			{
				var testFile = new ExpressionTestGenerator(dataContext).GenerateSource(expr);

				if (dataContext.GetTraceSwitch().TraceInfo)
					dataContext.WriteTraceLine(
						$"Expression test code generated: \'{testFile}\'.",
						dataContext.GetTraceSwitch().DisplayName,
						TraceLevel.Info);
			}

			var query = new Query<T>(dataContext, expr);

			try
			{
				query = new ExpressionBuilder(query, false, optimizationContext, parametersContext, dataContext, expr, null, null).Build<T>();
				if (query.ErrorExpression != null)
				{
					query = new Query<T>(dataContext, expr);
					query = new ExpressionBuilder(query, true, optimizationContext, parametersContext, dataContext, expr, null, null).Build<T>();
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
			int                                                          accessorId,
			Func<Expression,IDataContext?,object?[]?,object?>            valueAccessor,
			Func<object?,object?>?                                       itemAccessor,
			Func<Expression,object?,IDataContext?,object?[]?,DbDataType> dbDataTypeAccessor,
			SqlParameter                                                 sqlParameter)
		{
			AccessorId         = accessorId;
			ValueAccessor      = valueAccessor;
			ItemAccessor       = itemAccessor;
			DbDataTypeAccessor = dbDataTypeAccessor;
			SqlParameter       = sqlParameter;
		}

		public readonly int                                                          AccessorId;
		public readonly Func<Expression,IDataContext?,object?[]?,object?>            ValueAccessor;
		public readonly Func<object?,object?>?                                       ItemAccessor;
		public readonly Func<Expression,object?,IDataContext?,object?[]?,DbDataType> DbDataTypeAccessor;
		public readonly SqlParameter                                                 SqlParameter;
#if DEBUG
		public Expression<Func<Expression,IDataContext?,object?[]?,object?>>? AccessorExpr;
		public Expression<Func<object?,object?>>?                             ItemAccessorExpr;
#endif
	}
}
