using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Linq
{
	using Async;
	using Builder;
	using Data;
	using Common;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;
	using SqlProvider;
	using System.Diagnostics;

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
				Expression!.EqualsTo(expr, _queryableAccessorDic, _queryDependedObjects);
		}

		readonly Dictionary<Expression,QueryableAccessor> _queryableAccessorDic  = new Dictionary<Expression,QueryableAccessor>();
		readonly List<QueryableAccessor>                  _queryableAccessorList = new List<QueryableAccessor>();
		readonly Dictionary<Expression,Expression>        _queryDependedObjects  = new Dictionary<Expression,Expression>();

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

		Tuple<Func<IDataContext, object?>, Func<IDataContext, Task<object?>>>[]? _preambles;

		public void SetPreambles(
			IEnumerable<Tuple<Func<IDataContext, object?>, Func<IDataContext, Task<object?>>>>? preambles)
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

		public object?[]? InitPreambles(IDataContext dc)
		{
			if (_preambles == null)
				return null;

			var preambules = new object?[_preambles.Length];
			for (var i = 0; i < preambules.Length; i++)
			{
				preambules[i] = _preambles[i].Item1(dc);
			}

			return preambules;
		}

		public async Task<object?[]?> InitPreamblesAsync(IDataContext dc)
		{
			if (_preambles == null)
				return null;

			var preambules = new object?[_preambles.Length];
			for (var i = 0; i < preambules.Length; i++)
			{
				preambules[i] = await _preambles[i].Item2(dc).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}

			return preambules;
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
				Statement   = parseContext.GetResultStatement(),
				Parameters  = sqlParameters,
			});
		}

		#endregion

		#region Properties & Fields

		public bool DoNotCache;

		public Func<IDataContext,Expression,object?[]?,object?[]?,IEnumerable<T>>                      GetIEnumerable      = null!;
		public Func<IDataContext,Expression,object?[]?,object?[]?,IAsyncEnumerable<T>>                 GetIAsyncEnumerable = null!;
		public Func<IDataContext,Expression,object?[]?,object?[]?,Func<T,bool>,CancellationToken,Task> GetForEachAsync     = null!;

		#endregion

		#region Query cache
		class QueryCache
		{
			// lock for cache instance modification
			readonly object _syncCache    = new object();
			// lock for query priority modification
			readonly object _syncPriority = new object();

			// stores all cached queries
			// when query added or removed from cache, query and priority arrays recreated
			Query<T>[] _cache   = Array<Query<T>>.Empty;

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
							_cache   = Array<Query<T>>.Empty;
							_indexes = Array<int     >.Empty;
							_version++;
						}
			}

			/// <summary>
			/// Adds query to cache if it is not cached already.
			/// </summary>
			public void TryAdd(IDataContext dataContext, Query<T> query)
			{
				// because Add is less frequient operation than Find, it is fine to have put bigger locks here
				Query<T>[] cache;
				int        version;

				lock (_syncCache)
				{
					cache   = _cache;
					version = _version;
				}

				for (var i = 0; i < cache.Length; i++)
					if (cache[i].Compare(dataContext, query.Expression!))
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
							if (cache[i].Compare(dataContext, query.Expression!))
								// already added by another thread
								return;
					}

					// create new cache instance and reorder items according to priorities to inprove Find without
					// reorder lock
					var newCache      = new Query<T>[cache.Length == CacheSize ? CacheSize : cache.Length + 1];
					var newPriorities = new int[newCache.Length];

					newCache[0]      = query;
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
			public Query<T>? Find(IDataContext dataContext, Expression expr)
			{
				Query<T>[] cache;
				int[]      indexes;
				int        version;

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

						if (cache[idx].Compare(dataContext, expr))
						{
							// do reorder only if it is not blocked and cache wasn't replaced by new one
							if (i > 0 && version == _version && allowReordering)
							{
								var index      = indexes[i];
								indexes[i]     = indexes[i - 1];
								indexes[i - 1] = index;
							}

							return cache[idx];
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
			expr = ExpressionBuilder.ExpandExpression(expr);

			if (dataContext is IExpressionPreprocessor preprocessor)
				expr = preprocessor.ProcessExpression(expr);

			if (Configuration.Linq.DisableQueryCache)
				return CreateQuery(dataContext, expr);

			var query = _queryCache.Find(dataContext, expr);

			if (query == null)
			{
				query = CreateQuery(dataContext, expr);

				if (!query.DoNotCache)
					_queryCache.TryAdd(dataContext, query);
			}

			return query;
		}

		static Query<T> CreateQuery(IDataContext dataContext, Expression expr)
		{
			if (Configuration.Linq.GenerateExpressionTest)
			{
				var testFile = new ExpressionTestGenerator().GenerateSource(expr);

				if (DataConnection.TraceSwitch.TraceInfo)
					DataConnection.WriteTraceLine(
						$"Expression test code generated: \'{testFile}\'.",
						DataConnection.TraceSwitch.DisplayName,
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
					DataConnection.WriteTraceLine(
						"To generate test code to diagnose the problem set 'LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true'.",
						DataConnection.TraceSwitch.DisplayName,
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
		public SqlStatement  Statement   { get; set; } = null!;
		public object?       Context     { get; set; }
		public List<string>? QueryHints  { get; set; }

		public SqlParameter[] GetParameters()
		{
			var ps = new SqlParameter[Parameters.Count];

			for (var i = 0; i < ps.Length; i++)
				ps[i] = Parameters[i].SqlParameter;

			return ps;
		}

		public List<ParameterAccessor> Parameters = new List<ParameterAccessor>();
	}

	class ParameterAccessor
	{
		public ParameterAccessor(
			Expression                             expression,
			Func<Expression,object?[]?,object?>    accessor,
			Func<Expression,object?[]?,DbDataType> dbDataTypeAccessor,
			SqlParameter                           sqlParameter)
		{
			Expression         = expression;
			Accessor           = accessor;
			DbDataTypeAccessor = dbDataTypeAccessor;
			SqlParameter       = sqlParameter;
		}

		public          Expression                             Expression;
		public readonly Func<Expression,object?[]?,object?>    Accessor;
		public readonly Func<Expression,object?[]?,DbDataType> DbDataTypeAccessor;
		public readonly SqlParameter                           SqlParameter;
	}
}
