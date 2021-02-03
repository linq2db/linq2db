using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Linq
{
	using Async;
	using Builder;
	using Common;
	using Common.Logging;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlProvider;
	using SqlQuery;

	public abstract class Query
	{
		public Func<IDataContext,Expression,object?[]?,object?[]?,object?>                         GetElement      = null!;
		public Func<IDataContext,Expression,object?[]?,object?[]?,CancellationToken,Task<object?>> GetElementAsync = null!;

		#region Init

		internal readonly List<QueryInfo> Queries = new List<QueryInfo>(1);

		internal abstract void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters);

		protected Query(IDataContext dataContext, Expression? expression)
		{
			ContextID        = dataContext.ContextID;
			ContextType      = dataContext.GetType();
			Expression       = expression;
			MappingSchema    = dataContext.MappingSchema;
			ConfigurationID  = dataContext.MappingSchema.ConfigurationID;
			SqlOptimizer     = dataContext.GetSqlOptimizer();
			SqlProviderFlags = dataContext.SqlProviderFlags;
			InlineParameters = dataContext.InlineParameters;
		}

		#endregion

		#region Compare

		internal readonly string           ContextID;
		internal readonly Type             ContextType;
		internal readonly Expression?      Expression;
		internal readonly MappingSchema    MappingSchema;
		internal readonly string           ConfigurationID;
		internal readonly bool             InlineParameters;
		internal readonly ISqlOptimizer    SqlOptimizer;
		internal readonly SqlProviderFlags SqlProviderFlags;

		protected bool Compare(IDataContext dataContext, Expression expr)
		{
			return
				ContextID.Length       == dataContext.ContextID.Length &&
				ContextID              == dataContext.ContextID        &&
				ConfigurationID.Length == dataContext.MappingSchema.ConfigurationID.Length &&
				ConfigurationID        == dataContext.MappingSchema.ConfigurationID &&
				InlineParameters       == dataContext.InlineParameters &&
				ContextType            == dataContext.GetType()        &&
				Expression!.EqualsTo(expr, dataContext, _queryableAccessorDic, _queryableMemberAccessorDic, _queryDependedObjects);
		}

		readonly Dictionary<Expression,QueryableAccessor> _queryableAccessorDic  = new Dictionary<Expression,QueryableAccessor>();
		private  Dictionary<MemberInfo, QueryableMemberAccessor>? _queryableMemberAccessorDic;
		readonly List<QueryableAccessor>                  _queryableAccessorList = new List<QueryableAccessor>();
		readonly Dictionary<Expression,Expression>        _queryDependedObjects  = new Dictionary<Expression,Expression>();


		public bool IsFastCacheable => _queryableMemberAccessorDic == null;

		internal int AddQueryableAccessors(Expression expr, Expression<Func<Expression,IQueryable>> qe)
		{
			if (_queryableAccessorDic.TryGetValue(expr, out var e))
				return _queryableAccessorList.IndexOf(e);

			e = new QueryableAccessor { Accessor = qe.Compile() };
			e.Queryable = e.Accessor(expr);

			_queryableAccessorDic. Add(expr, e);
			_queryableAccessorList.Add(e);

			return _queryableAccessorList.Count - 1;
		}

		internal Expression AddQueryableMemberAccessors(MemberInfo memberInfo, IDataContext dataContext, Func<MemberInfo, IDataContext, Expression> qe)
		{
			if (_queryableMemberAccessorDic != null && _queryableMemberAccessorDic.TryGetValue(memberInfo, out var e))
				return e.Expression;

			e = new QueryableMemberAccessor { Accessor = qe };
			e.Expression = e.Accessor(memberInfo, dataContext);

			_queryableMemberAccessorDic ??= new Dictionary<MemberInfo, QueryableMemberAccessor>(MemberInfoComparer.Instance);
			_queryableMemberAccessorDic.Add(memberInfo, e);

			return e.Expression;
		}

		internal void AddQueryDependedObject(Expression expression, SqlQueryDependentAttribute attr)
		{
			foreach (var expr in attr.SplitExpression(expression))
			{
				if (_queryDependedObjects.ContainsKey(expr))
					continue;

				var prepared = attr.PrepareForCache(expr);
				_queryDependedObjects.Add(expr, prepared);
			}
		}

		internal Expression GetIQueryable(int n, Expression expr)
		{
			return _queryableAccessorList[n].Accessor(expr).Expression;
		}

		public void ClearMemberQueryableInfo()
		{
			_queryableMemberAccessorDic = null;
		}

		#endregion

		#region Helpers

		ConcurrentDictionary<Type,Func<object,object>>? _enumConverters;

		internal object GetConvertedEnum(Type valueType, object value)
		{
			if (_enumConverters == null)
				_enumConverters = new ConcurrentDictionary<Type, Func<object, object>>();

			if (!_enumConverters.TryGetValue(valueType, out var converter))
			{
				var toType    = Converter.GetDefaultMappingFromEnumType(MappingSchema, valueType)!;
				var convExpr  = MappingSchema.GetConvertExpression(valueType, toType)!;
				var convParam = Expression.Parameter(typeof(object));

				var lex = Expression.Lambda<Func<object, object>>(
					Expression.Convert(convExpr.GetBody(Expression.Convert(convParam, valueType)), typeof(object)),
					convParam);

				converter = lex.Compile();

				_enumConverters.GetOrAdd(valueType, converter);
			}

			return converter(value);
		}

		#endregion

		#region Cache Support

		internal static readonly ConcurrentQueue<Action> CacheCleaners = new ConcurrentQueue<Action>();

		/// <summary>
		/// Clears query caches for all typed queries.
		/// </summary>
		public static void ClearCaches()
		{
			InternalExtensions.ClearCaches();

			foreach (var cleaner in CacheCleaners)
			{
				cleaner();
			}
		}

		#endregion

		#region Eager Loading

		Tuple<Func<IDataContext, Expression, object?[]?, object?>, Func<IDataContext, Expression, object?[]?, CancellationToken, Task<object?>>>[]? _preambles;

		public void SetPreambles(
			IEnumerable<Tuple<Func<IDataContext, Expression, object?[]?, object?>, Func<IDataContext, Expression, object?[]?, CancellationToken, Task<object?>>>>? preambles)
		{
			_preambles = preambles?.ToArray();
		}

		public bool IsAnyPreambles()
		{
			return _preambles?.Length > 0;
		}

		public int PreamblesCount()
		{
			return _preambles?.Length ?? 0;
		}

		public object?[]? InitPreambles(IDataContext dc, Expression rootExpression, object?[]? ps)
		{
			if (_preambles == null)
				return null;

			var preambles = new object?[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = _preambles[i].Item1(dc, rootExpression, ps);
			}

			return preambles;
		}

		public async Task<object?[]?> InitPreamblesAsync(IDataContext dc, Expression rootExpression, object?[]? ps, CancellationToken cancellationToken)
		{
			if (_preambles == null)
				return null;

			var preambles = new object?[_preambles.Length];
			for (var i = 0; i < preambles.Length; i++)
			{
				preambles[i] = await _preambles[i].Item2(dc, rootExpression, ps, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			return preambles;
		}

		#endregion
	}

	class Query<T> : Query
	{
		#region Init

		public Query(IDataContext dataContext, Expression? expression)
			: base(dataContext, expression)
		{
			DoNotCache = NoLinqCache.IsNoCache;
		}

		internal override void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters)
		{
			Queries.Add(new QueryInfo
			{
				Statement          = parseContext.GetResultStatement(),
				ParameterAccessors = sqlParameters,
			});
		}

		#endregion

		#region Properties & Fields

		public bool DoNotCache;

		public Func<IDataContext,Expression,object?[]?,object?[]?,IEnumerable<T>>      GetIEnumerable = null!;
		public Func<IDataContext,Expression,object?[]?,object?[]?,IAsyncEnumerable<T>> GetIAsyncEnumerable = null!;
		public Func<IDataContext,Expression,object?[]?,object?[]?,Func<T,bool>,CancellationToken,Task> GetForEachAsync = null!;

		#endregion

		#region Query cache
		[Flags]
		enum QueryFlags
		{
			None                = 0,
			/// <summary>
			/// Bit set, when group by guard set for connection.
			/// </summary>
			GroupByGuard        = 0x1,
			/// <summary>
			/// Bit set, when inline parameters enabled for connection.
			/// </summary>
			InlineParameters    = 0x2,
			/// <summary>
			/// Bit set, when inline Take/Skip parameterization is enabled for query.
			/// </summary>
			ParameterizeTakeSkip = 0x4,
		}

		class QueryCache
		{
			class QueryCacheEntry
			{
				public QueryCacheEntry(Query<T> query, QueryFlags flags)
				{
					// query doesn't have GetHashCode now, so we cannot precalculate hashcode to speed-up search
					Query = query;
					Flags = flags;
				}

				public Query<T>   Query { get; }
				public QueryFlags Flags { get; }

				// accepts components to avoid QueryCacheEntry allocation for cached query
				public bool Compare(IDataContext context, Expression queryExpression, QueryFlags flags)
				{
					return Flags == flags && Query.Compare(context, queryExpression);
				}
			}

			// lock for cache instance modification
			readonly object _syncCache    = new object();
			// lock for query priority modification
			readonly object _syncPriority = new object();

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
			internal long CacheMissCount;

			/// <summary>
			/// LINQ query max cache size (per entity type).
			/// </summary>
			const int CacheSize = 100;

			/// <summary>
			/// Empties LINQ query cache for <typeparamref name="T"/> entity type.
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
			public void TryAdd(IDataContext dataContext, Query<T> query, QueryFlags flags)
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
					if (cache[i].Compare(dataContext, query.Expression!, flags))
						// already added by another thread
						return;

				lock (_syncCache)
				{
					var priorities   = _indexes;
					var versionsDiff = _version - version;

					if (versionsDiff > 0)
					{
						cache = _cache;

						// check only added queries, each version could add 1 query to first position, so we
						// test only first N queries
						for (var i = 0; i < cache.Length && i < versionsDiff; i++)
							if (cache[i].Compare(dataContext, query.Expression!, flags))
								// already added by another thread
								return;
					}

					// create new cache instance and reorder items according to priorities to improve Find without
					// reorder lock
					var newCache      = new QueryCacheEntry[cache.Length == CacheSize ? CacheSize : cache.Length + 1];
					var newPriorities = new int[newCache.Length];

					newCache[0]      = new QueryCacheEntry(query, flags);
					newPriorities[0] = 0;

					for (var i = 1; i < newCache.Length; i++)
					{
						newCache[i]      = cache[i - 1];
						newPriorities[i] = i;
					}

					_cache   = newCache;
					_indexes = newPriorities;
					version  = _version;
				}
			}

			/// <summary>
			/// Search for query in cache and of found, try to move it to better position in cache.
			/// </summary>
			public Query<T>? Find(IDataContext dataContext, Expression expr, QueryFlags flags)
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

						if (cache[idx].Compare(dataContext, expr, flags))
						{
							// do reorder only if it is not blocked and cache wasn't replaced by new one
							if (i > 0 && version == _version && allowReordering)
							{
								var index      = indexes[i];
								indexes[i]     = indexes[i - 1];
								indexes[i - 1] = index;
							}

							return cache[idx].Query;
						}
					}
				}
				finally
				{
					if (allowReordering)
						Monitor.Exit(_syncPriority);
				}

				Interlocked.Increment(ref CacheMissCount);

				return null;
			}
		}

		#endregion

		#region Query

		private static readonly QueryCache _queryCache = new QueryCache();

		static Query()
		{
			CacheCleaners.Enqueue(ClearCache);
		}

		/// <summary>
		/// Empties LINQ query cache for <typeparamref name="T"/> entity type.
		/// </summary>
		public static void ClearCache() => _queryCache.Clear();

		public static long CacheMissCount => _queryCache.CacheMissCount;

		public static Query<T> GetQuery(IDataContext dataContext, ref Expression expr)
		{
			expr = ExpressionTreeOptimizationContext.ExpandExpression(expr);

			if (dataContext is IExpressionPreprocessor preprocessor)
				expr = preprocessor.ProcessExpression(expr);

			if (Configuration.Linq.DisableQueryCache)
				return CreateQuery(dataContext, expr);

			// calculate query flags
			var flags = QueryFlags.None;

			if (dataContext.InlineParameters)
				flags |= QueryFlags.InlineParameters;

			// TODO: here we have race condition due to flag being global setting
			// to fix it we must move flags to context level and remove global flags or invalidate caches on
			// global flag change
			if (Configuration.Linq.GuardGrouping)
				flags |= QueryFlags.GroupByGuard;
			if (Configuration.Linq.ParameterizeTakeSkip)
				flags |= QueryFlags.ParameterizeTakeSkip;

			var query = _queryCache.Find(dataContext, expr, flags);

			if (query == null)
			{
				query = CreateQuery(dataContext, expr);

				if (!query.DoNotCache)
					_queryCache.TryAdd(dataContext, query, flags);
			}

			return query;
		}

		static Query<T> CreateQuery(IDataContext dataContext, Expression expr)
		{
			if (Configuration.Linq.GenerateExpressionTest)
			{
				var testFile = new ExpressionTestGenerator().GenerateSource(expr);

				if (dataContext.GetTraceSwitch().TraceInfo)
					dataContext.WriteTraceLine(
						$"Expression test code generated: \'{testFile}\'.",
						dataContext.GetTraceSwitch().DisplayName,
						TraceLevel.Info);
			}

			var query = new Query<T>(dataContext, expr);

			try
			{
				query = new ExpressionBuilder(query, dataContext, expr, null).Build<T>();
			}
			catch (Exception)
			{
				if (!Configuration.Linq.GenerateExpressionTest)
				{
					dataContext.WriteTraceLine(
						"To generate test code to diagnose the problem set 'LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true'.",
						dataContext.GetTraceSwitch().DisplayName,
						TraceLevel.Error);
				}

				throw;
			}

			return query;
		}

		#endregion
	}

	class QueryInfo : IQueryContext
	{
		public SqlStatement    Statement   { get; set; } = null!;
		public object?         Context     { get; set; }
		public List<string>?   QueryHints  { get; set; }
		public SqlParameter[]? Parameters  { get; set; }
		public AliasesContext? Aliases     { get; set; }

		public List<ParameterAccessor> ParameterAccessors = new List<ParameterAccessor>();

		public void AddParameterAccessor(ParameterAccessor accessor)
		{
			ParameterAccessors.Add(accessor);
			accessor.SqlParameter.AccessorId = ParameterAccessors.Count - 1;
		}
	}

	class ParameterAccessor
	{
		public ParameterAccessor(
			Expression                             expression,
			Func<Expression,IDataContext?,object?[]?,object?>    valueAccessor,
			Func<Expression,IDataContext?,object?[]?,object?>    originalAccessor,
			Func<Expression,IDataContext?,object?[]?,DbDataType> dbDataTypeAccessor,
			SqlParameter                           sqlParameter)
		{
			Expression         = expression;
			ValueAccessor      = valueAccessor;
			OriginalAccessor   = originalAccessor;
			DbDataTypeAccessor = dbDataTypeAccessor;
			SqlParameter       = sqlParameter;
		}

		public          Expression                                           Expression;
		public readonly Func<Expression,IDataContext?,object?[]?,object?>    ValueAccessor;
		public readonly Func<Expression,IDataContext?,object?[]?,object?>    OriginalAccessor;
		public readonly Func<Expression,IDataContext?,object?[]?,DbDataType> DbDataTypeAccessor;
		public readonly SqlParameter                                         SqlParameter;
#if DEBUG
		public Expression<Func<Expression,IDataContext?,object?[]?,object?>>? AccessorExpr;
#endif
	}
}
