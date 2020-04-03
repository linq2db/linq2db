﻿using System;
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

		internal void AddQueryDependedObject(Expression expr, SqlQueryDependentAttribute attr)
		{
			if (_queryDependedObjects.ContainsKey(expr))
				return;

			var prepared = attr.PrepareForCache(expr);
			_queryDependedObjects.Add(expr, prepared);
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
			return _preambles.Select(p => p.Item1(dc)).ToArray();
		}

		public async Task<object?[]?> InitPreamblesAsync(IDataContext dc)
		{
			if (_preambles == null)
				return null;
			var result = new List<object?>();
			foreach (var p in _preambles)
			{
				result.Add(await p.Item2(dc).ConfigureAwait(Configuration.ContinueOnCapturedContext));
			}
			return result.ToArray();
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

		public Func<IDataContext,Expression,object?[]?,object?[]?,IEnumerable<T>>      GetIEnumerable = null!;
		public Func<IDataContext,Expression,object?[]?,object?[]?,IAsyncEnumerable<T>> GetIAsyncEnumerable = null!;
		public Func<IDataContext,Expression,object?[]?,object?[]?,Func<T,bool>,CancellationToken,Task> GetForEachAsync = null!;

		#endregion

		#region GetInfo

		static readonly List<Query<T>> _orderedCache;

		/// <summary>
		/// LINQ query cache version. Changed when query added or removed from cache.
		/// Not changed when cache reordered.
		/// </summary>
		static int _cacheVersion;

		/// <summary>
		/// Count of queries which has not been found in cache.
		/// </summary>
		static long _cacheMissCount;

		/// <summary>
		/// LINQ query cache synchronization object.
		/// </summary>
		static readonly object _sync;

		/// <summary>
		/// LINQ query cache size (per entity type).
		/// </summary>
		const int CacheSize = 100;

		static Query()
		{
			_sync         = new object();
			_orderedCache = new List<Query<T>>(CacheSize);

			CacheCleaners.Enqueue(ClearCache);
		}

		/// <summary>
		/// Empties LINQ query cache for <typeparamref name="T"/> entity type.
		/// </summary>
		public static void ClearCache()
		{
			if (_orderedCache.Count != 0)
				lock (_sync)
				{
					if (_orderedCache.Count != 0)
						_cacheVersion++;

					_orderedCache.Clear();
				}
		}

		public static long CacheMissCount => _cacheMissCount;

		public static Query<T> GetQuery(IDataContext dataContext, ref Expression expr)
		{
			expr = ExpressionBuilder.ExpandExpression(expr);

			if (dataContext is IExpressionPreprocessor preprocessor)
				expr = preprocessor.ProcessExpression(expr);

			if (Configuration.Linq.DisableQueryCache)
				return CreateQuery(dataContext, expr);

			var query = FindQuery(dataContext, expr);

			if (query == null)
			{
				var oldVersion = _cacheVersion;
				query = CreateQuery(dataContext, expr);

				// move lock as far as possible, because this method called a lot
				if (!query.DoNotCache)
					lock (_sync)
					{
						if (oldVersion == _cacheVersion || FindQuery(dataContext, expr) == null)
						{
							if (_orderedCache.Count == CacheSize)
								_orderedCache.RemoveAt(CacheSize - 1);

							_orderedCache.Insert(0, query);
							_cacheVersion++;
						}
					}
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

		static Query<T>? FindQuery(IDataContext dataContext, Expression expr)
		{
			Query<T>[] queries;

			// create thread-safe copy
			lock (_sync)
				queries = _orderedCache.ToArray();

			foreach (var query in queries)
			{
				if (query.Compare(dataContext, expr))
				{
					// move found query up in cache
					lock (_sync)
					{
						var oldIndex = _orderedCache.IndexOf(query);
						if (oldIndex > 0)
						{
							var prev = _orderedCache[oldIndex - 1];
							_orderedCache[oldIndex - 1] = query;
							_orderedCache[oldIndex] = prev;
						}
						else if (oldIndex == -1)
						{
							// query were evicted from cache - readd it
							if (_orderedCache.Count == CacheSize)
								_orderedCache.RemoveAt(CacheSize - 1);

							_orderedCache.Insert(0, query);
							_cacheVersion++;
						}
					}

					return query;
				}
			}

			Interlocked.Increment(ref _cacheMissCount);

			return null;
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
