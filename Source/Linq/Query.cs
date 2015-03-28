using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Builder;
	using Mapping;
	using SqlProvider;

	abstract class Query
	{
		public string           ContextID;
		public Expression       Expression;
		public MappingSchema    MappingSchema;
		public SqlProviderFlags SqlProviderFlags;

		public ParameterExpression DataContextParameter = Expression.Parameter(typeof(IDataContext), "dataContext");
		public ParameterExpression ExpressionParameter  = Expression.Parameter(typeof(Expression),   "expression");

		readonly Dictionary<Expression,QueryableAccessor> _queryableAccessorDic  = new Dictionary<Expression,QueryableAccessor>();

		public bool Compare(string contextID, MappingSchema mappingSchema, Expression expr)
		{
			return
				ContextID.Length == contextID.Length &&
				ContextID        == contextID        &&
				MappingSchema    == mappingSchema    &&
				Expression.EqualsTo(expr, _queryableAccessorDic);
		}
	}

	class Query<T> : Query
	{
		Query(IDataContext dataContext, Expression expression)
		{
			ContextID        = dataContext.ContextID;
			Expression       = expression;
			MappingSchema    = dataContext.MappingSchema;
			SqlProviderFlags = dataContext.SqlProviderFlags;
		}

		public Func<IDataContext,Expression,T>              GetElement;
		public Func<IDataContext,Expression,IEnumerable<T>> GetIEnumerable;

		Query<T> _next;

		#region GetQuery

		static          Query<T> _first;
		static readonly object   _sync = new object();

		const int CacheSize = 100;

		public static Query<T> GetQuery(IDataContext dataContext, Expression expr, bool isEnumerable)
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
							DataConnection.WriteTraceLine(
								"Expression test code generated: '" + testFile + "'.", 
								DataConnection.TraceSwitch.DisplayName);
#endif
						}

						query = new Query<T>(dataContext, expr);

						try
						{
							var builder = new QueryBuilder(query);

							if (isEnumerable) query.GetIEnumerable = builder.BuildEnumerable<T>(query.Expression);
							else              query.GetElement     = builder.BuildElement   <T>(query.Expression);
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
					}
				}
			}

			return query;
		}

		static Query<T> FindQuery(IDataContext dataContext, Expression expr)
		{
			Query<T> prev = null;
			var      n    = 0;

			for (var query = _first; query != null; query = query._next)
			{
				if (query.Compare(dataContext.ContextID, dataContext.MappingSchema, expr))
				{
					if (prev != null)
					{
						lock (_sync)
						{
							prev._next  = query._next;
							query._next = _first;
							_first      = query;
						}
					}

					return query;
				}

				if (n++ >= CacheSize)
				{
					query._next = null;
					return null;
				}

				prev = query;
			}

			return null;
		}

		#endregion
	}
}
