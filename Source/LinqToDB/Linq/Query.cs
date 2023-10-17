using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Linq
{
#if !NATIVE_ASYNC
	using Async;
#endif
	using Builder;
	using Common;
	using Common.Logging;
	using Interceptors;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	public abstract class Query
	{
		internal Func<IDataContext,Expression,object?[]?,object?[]?,object?>                         GetElement      = null!;
		internal Func<IDataContext,Expression,object?[]?,object?[]?,CancellationToken,Task<object?>> GetElementAsync = null!;

		#region Init

		internal readonly List<QueryInfo> Queries = new (1);

		public IReadOnlyCollection<QueryInfo> GetQueries() => Queries;

		internal abstract void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters);

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
		internal readonly Expression?      Expression;
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
				Expression!.EqualsTo(expr, dataContext, _parametrized, _queryableMemberAccessorDic);
		}

		readonly List<QueryableAccessor>                          _queryableAccessorList = new();
		private  Dictionary<MemberInfo, QueryableMemberAccessor>? _queryableMemberAccessorDic;
		private  List<Expression>?                                _parametrized;

		internal bool IsFastCacheable => _queryableMemberAccessorDic == null;

		internal void SetParametrized(List<Expression>? parametrized)
		{
			_parametrized = parametrized;
		}

		public bool IsParametrized(Expression expr)
		{
			return _parametrized?.Contains(expr) == true;
		}

		internal Expression AddQueryableMemberAccessors<TContext>(TContext context, MemberInfo memberInfo, IDataContext dataContext, Func<TContext, MemberInfo, IDataContext, Expression> qe)
		{
			if (_queryableMemberAccessorDic != null && _queryableMemberAccessorDic.TryGetValue(memberInfo, out var e))
				return e.Expression;

			e = new QueryableMemberAccessor<TContext>(context, qe(context, memberInfo, dataContext), qe);

			_queryableMemberAccessorDic ??= new Dictionary<MemberInfo, QueryableMemberAccessor>(MemberInfoComparer.Instance);
			_queryableMemberAccessorDic.Add(memberInfo, e);

			return e.Expression;
		}

		internal Expression GetIQueryable(int n, Expression expr, bool force)
		{
			var accessor = _queryableAccessorList[n];
			if (force)
			{
				if (accessor.SkipForce)
					accessor.SkipForce = false;
				else
					return (accessor.Queryable = accessor.Accessor(expr)).Expression;
			}

			return accessor.Queryable.Expression;
		}

		internal void ClearMemberQueryableInfo()
		{
			_queryableMemberAccessorDic = null;
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
				preambles[i] = await _preambles[i].ExecuteAsync(dc, rootExpression, ps, preambles, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
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

		internal override void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters)
		{
			var statement = parseContext.GetResultStatement();

			Queries.Add(new QueryInfo
			{
				Statement          = statement,
				ParameterAccessors = sqlParameters,
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
			QueryCacheEntry[] _cache   = Array<QueryCacheEntry>.Empty;

			// stores ordered list of query indexes for Find operation
			int[]      _indexes = Array<int     >.Empty;

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
							_cache   = Array<QueryCacheEntry>.Empty;
							_indexes = Array<int            >.Empty;
							_version++;
						}
			}

			/// <summary>
			/// Adds query to cache if it is not cached already.
			/// </summary>
			public void TryAdd(IDataContext dataContext, Query<T> query, QueryFlags queryFlags, DataOptions dataOptions)
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
					if (cache[i].Compare(dataContext, query.Expression!, queryFlags))
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
			var optimizationContext = new ExpressionTreeOptimizationContext(dataContext);

			// I hope fast tree optimization for unbalanced Binary Expressions. See Issue447Tests.
			//
			expr = optimizationContext.AggregateExpression(expr);

			dependsOnParameters = false;

			if (dataContext is IExpressionPreprocessor preprocessor)
				expr = preprocessor.ProcessExpression(expr);

			var dataOptions = dataContext.Options;

			if (dataOptions.LinqOptions.DisableQueryCache)
				return CreateQuery(optimizationContext, new ParametersContext(expr, optimizationContext, dataContext), dataContext, expr);

			var queryFlags = dataContext.GetQueryFlags();

			var query = _queryCache.Find(dataContext, expr, queryFlags, false);
			if (query != null)
				return query;

			// Expose expression, call all needed invocations.
			// After execution there should be no constants which contains IDataContext reference, no constants with ExpressionQueryImpl  
			// Parameters with SqlQueryDependentAttribute will be transferred to constants
			// No LambdaExpressions which are located in constants, they will be expanded and injected into tree
			//
			var exposed = ExpressionBuilder.ExposeExpression(expr, dataContext, optimizationContext,
				optimizeConditions : true, compactBinary : false /* binary already compacted by AggregateExpression*/);

			// simple trees do not mutate
			var isExposed = !ReferenceEquals(exposed, expr);

			expr = exposed;
			if (isExposed)
			{
				dependsOnParameters = true;

				queryFlags |= QueryFlags.ExpandedQuery;

				// search again
				query = _queryCache.Find(dataContext, expr, queryFlags, true);
				if (query != null)
					return query;
			}

			// Cache missed, Build query
			//
			_queryCache.TriggerCacheMiss();

			query = CreateQuery(optimizationContext, new ParametersContext(expr, optimizationContext, dataContext),
				dataContext, expr);

			if (!query.DoNotCache)
			{
				_queryCache.TryAdd(dataContext, query, queryFlags, dataOptions);
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
				query = new ExpressionBuilder(query, optimizationContext, parametersContext, dataContext, expr, null, null).Build<T>();
			}
			catch (Exception)
			{
				if (!linqOptions.GenerateExpressionTest)
				{
					dataContext.WriteTraceLine(
						"To generate test code to diagnose the problem set 'LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true'.\n" +
						"Or specify LINQ options during 'DataContextOptions' building 'options.UseGenerateExpressionTest(true)'",
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
		public SqlStatement    Statement  { get; set; } = null!;
		public object?         Context    { get; set; }
		public AliasesContext? Aliases    { get; set; }

		internal List<ParameterAccessor> ParameterAccessors = new ();

		internal void AddParameterAccessor(ParameterAccessor accessor)
		{
			ParameterAccessors.Add(accessor);
			accessor.SqlParameter.AccessorId = ParameterAccessors.Count - 1;
		}
	}

	sealed class ParameterAccessor
	{
		public ParameterAccessor(
			Expression                                           expression,
			Func<Expression,IDataContext?,object?[]?,object?>    valueAccessor,
			Func<Expression,IDataContext?,object?[]?,object?>    originalAccessor,
			Func<Expression,IDataContext?,object?[]?,DbDataType> dbDataTypeAccessor,
			SqlParameter                                         sqlParameter)
		{
			Expression         = expression;
			ValueAccessor      = valueAccessor;
			OriginalAccessor   = originalAccessor;
			DbDataTypeAccessor = dbDataTypeAccessor;
			SqlParameter       = sqlParameter;
		}

		public          Expression                                            Expression;
		public readonly Func<Expression,IDataContext?,object?[]?,object?>     ValueAccessor;
		public readonly Func<Expression,IDataContext?,object?[]?,object?>     OriginalAccessor;
		public readonly Func<Expression,IDataContext?,object?[]?,DbDataType>  DbDataTypeAccessor;
		public readonly SqlParameter                                          SqlParameter;
#if DEBUG
		public Expression<Func<Expression,IDataContext?,object?[]?,object?>>? AccessorExpr;
#endif
	}
}
