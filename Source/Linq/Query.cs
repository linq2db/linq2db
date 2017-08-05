using System;
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
		public Func<QueryContext,IDataContextEx,Expression,object[],object> GetElement;
#if !SL4
		public Func<QueryContext,IDataContextEx,Expression,object[],CancellationToken,Task<object>> GetElementAsync;
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
		}

		#endregion

		#region Compare

		public readonly string           ContextID;
		public readonly Expression       Expression;
		public readonly MappingSchema    MappingSchema;
		public readonly string           ConfigurationID;
		public readonly ISqlOptimizer    SqlOptimizer;
		public readonly SqlProviderFlags SqlProviderFlags;

		public bool Compare(string contextID, MappingSchema mappingSchema, Expression expr)
		{
			return
				ContextID.Length       == contextID.Length &&
				ContextID              == contextID        &&
				ConfigurationID.Length == mappingSchema.ConfigurationID.Length &&
				ConfigurationID        == mappingSchema.ConfigurationID &&
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
			// IT : # check
			GetIEnumerable = MakeEnumerable;
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

		public bool     DoNotChache;
		public Query<T> Next;

		public Func<QueryContext,IDataContextEx,Expression,object[],IEnumerable<T>> GetIEnumerable;
#if !SL4
		public Func<QueryContext,IDataContextEx,Expression,object[],Func<T,bool>,CancellationToken,Task> GetForEachAsync;
#endif

		IEnumerable<T> MakeEnumerable(QueryContext qc, IDataContextEx dc, Expression expr, object[] ps)
		{
			yield return ConvertTo<T>.From(GetElement(qc, dc, expr, ps));
		}

		#endregion

		#region GetInfo

		static          Query<T> _first;
		static readonly object   _sync = new object();

		const int CacheSize = 100;

		public static Query<T> GetQuery(IDataContext dataContext, Expression expr)
		{
			var query = FindQuery(dataContext, expr);

			if (query == null)
			{
				lock (_sync)
				{
					query = FindQuery(dataContext, expr);

					if (query == null)
					{
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

						if (!query.DoNotChache)
						{
							query.Next = _first;
							_first = query;
						}
					}
				}
			}

			return query;
		}

		static Query<T> FindQuery(IDataContext dataContext, Expression expr)
		{
			Query<T> prev = null;
			var      n    = 0;

			for (var query = _first; query != null; query = query.Next)
			{
				if (query.Compare(dataContext.ContextID, dataContext.MappingSchema, expr))
				{
					if (prev != null)
					{
						lock (_sync)
						{
							prev.Next  = query.Next;
							query.Next = _first;
							_first     = query;
						}
					}

					return query;
				}

				if (n++ >= CacheSize)
				{
					query.Next = null;
					return null;
				}

				prev = query;
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
			Expression                           expression,
			Func<Expression, object[], object>   accessor,
			Func<Expression, object[], DataType> dataTypeAccessor,
			SqlParameter                         sqlParameter
			)
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
