﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

#if !SL4
using System.Threading.Tasks;
#endif

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Linq
{
	using Builder;
	using Data;
	using Common;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	abstract class Query
	{
		public Func<IDataContextEx,Expression,object[],object> GetElement;
#if !SL4
		public Func<IDataContextEx,Expression,object[],CancellationToken,Task<object>> GetElementAsync;
#endif

		#region Init

		public readonly List<QueryInfo> Queries = new List<QueryInfo>(1);

		public abstract void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters);

		protected Query(IDataContext dataContext, Expression expression)
		{
			ContextID        = dataContext.ContextID;
			Expression       = expression;
			MappingSchema    = dataContext.MappingSchema;
			ConfigurationID  = dataContext.MappingSchema.ConfigurationID;
			SqlOptimizer     = dataContext.GetSqlOptimizer();
			SqlProviderFlags = dataContext.SqlProviderFlags;
			InlineParameters = dataContext.InlineParameters;
		}

		#endregion

		#region Compare

		public readonly string           ContextID;
		public readonly Expression       Expression;
		public readonly MappingSchema    MappingSchema;
		public readonly string           ConfigurationID;
		public readonly bool             InlineParameters;
		public readonly ISqlOptimizer    SqlOptimizer;
		public readonly SqlProviderFlags SqlProviderFlags;

		protected bool Compare(IDataContext dataContext, Expression expr)
		{
			return
				ContextID.Length       == dataContext.ContextID.Length &&
				ContextID              == dataContext.ContextID        &&
				ConfigurationID.Length == dataContext.MappingSchema.ConfigurationID.Length &&
				ConfigurationID        == dataContext.MappingSchema.ConfigurationID &&
				InlineParameters       == dataContext.InlineParameters &&
				Expression.EqualsTo(expr, _queryableAccessorDic);
		}

		readonly Dictionary<Expression,QueryableAccessor> _queryableAccessorDic  = new Dictionary<Expression,QueryableAccessor>();
		readonly List<QueryableAccessor>                  _queryableAccessorList = new List<QueryableAccessor>();

		internal int AddQueryableAccessors(Expression expr, Expression<Func<Expression,IQueryable>> qe)
		{
			QueryableAccessor e;

			if (_queryableAccessorDic.TryGetValue(expr, out e))
				return _queryableAccessorList.IndexOf(e);

			e = new QueryableAccessor { Accessor = qe.Compile() };
			e.Queryable = e.Accessor(expr);

			_queryableAccessorDic. Add(expr, e);
			_queryableAccessorList.Add(e);

			return _queryableAccessorList.Count - 1;
		}

		public Expression GetIQueryable(int n, Expression expr)
		{
			return _queryableAccessorList[n].Accessor(expr).Expression;
		}

		#endregion

		#region Helpers

		ConcurrentDictionary<Type,Func<object,object>> _enumConverters;

		public object GetConvertedEnum(Type valueType, object value)
		{
			if (_enumConverters == null)
				_enumConverters = new ConcurrentDictionary<Type, Func<object, object>>();

			Func<object, object> converter;

			if (!_enumConverters.TryGetValue(valueType, out converter))
			{
				var toType    = Converter.GetDefaultMappingFromEnumType(MappingSchema, valueType);
				var convExpr  = MappingSchema.GetConvertExpression(valueType, toType);
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
	}

	class Query<T> : Query
	{
		#region Init

		public Query(IDataContext dataContext, Expression expression)
			: base(dataContext, expression)
		{
#if !SILVERLIGHT && !WINSTORE
			DoNotCache     = NoLinqCache.IsNoCache;
#endif
		}

		public override void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters)
		{
			Queries.Add(new QueryInfo
			{
				SelectQuery = parseContext.SelectQuery,
				Parameters  = sqlParameters,
			});
		}

		#endregion

		#region Properties & Fields

		public          bool            DoNotCache;

		public Func<IDataContextEx,Expression,object[],IEnumerable<T>> GetIEnumerable;
#if !SL4
		public Func<IDataContextEx,Expression,object[],Func<T,bool>,CancellationToken,Task> GetForEachAsync;
#endif

		#endregion

		#region GetInfo

		static          List<Query<T>> _orderedCache = new List<Query<T>>(CacheSize);

		/// <summary>
		/// LINQ query cache version. Changed when query added or removed from cache.
		/// Not changed when cache reordered.
		/// </summary>
		static          int            _cacheVersion;
		/// <summary>
		/// LINQ query cache synchronization object.
		/// </summary>
		static readonly object   _sync = new object();

		/// <summary>
		/// LINQ query cache size (per entity type).
		/// </summary>
		const int CacheSize = 100;

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

		public static Query<T> GetQuery(IDataContext dataContext, ref Expression expr)
		{
			if (Configuration.Linq.UseBinaryAggregateExpression)
				expr = ExpressionBuilder.AggregateExpression(expr);

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

		private static Query<T> CreateQuery(IDataContext dataContext, Expression expr)
					{
			Query<T> query;
						if (Configuration.Linq.GenerateExpressionTest)
						{
							var testFile = new ExpressionTestGenerator().GenerateSource(expr);
#if !SILVERLIGHT && !NETFX_CORE
							if (DataConnection.TraceSwitch.TraceInfo)
								DataConnection.WriteTraceLine(
									"Expression test code generated: '" + testFile + "'.", 
									DataConnection.TraceSwitch.DisplayName);
#endif
						}

						query = new Query<T>(dataContext, expr);

						try
						{
							query = new ExpressionBuilder(query, dataContext, expr, null).Build<T>();
						}
						catch (Exception)
						{
							if (!Configuration.Linq.GenerateExpressionTest)
							{
#if !SILVERLIGHT && !NETFX_CORE
								DataConnection.WriteTraceLine(
									"To generate test code to diagnose the problem set 'LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true'.",
									DataConnection.TraceSwitch.DisplayName);
#endif
							}

							throw;
						}

			return query;
		}

		static Query<T> FindQuery(IDataContext dataContext, Expression expr)
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

			return null;
		}

		#endregion
	}

	class QueryInfo : IQueryContext
	{
		public QueryInfo()
		{
			SelectQuery = new SelectQuery();
		}

		public SelectQuery  SelectQuery { get; set; }
		public object       Context     { get; set; }
		public List<string> QueryHints  { get; set; }

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
			Expression                         expression,
			Func<Expression,object[],object>   accessor,
			Func<Expression,object[],DataType> dataTypeAccessor,
			SqlParameter                       sqlParameter)
		{
			Expression       = expression;
			Accessor         = accessor;
			DataTypeAccessor = dataTypeAccessor;
			SqlParameter     = sqlParameter;
		}

		public          Expression                         Expression;
		public readonly Func<Expression,object[],object>   Accessor;
		public readonly Func<Expression,object[],DataType> DataTypeAccessor;
		public readonly SqlParameter                       SqlParameter;
	}
}
