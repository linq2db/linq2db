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
			GetIEnumerable   = MakeEnumerable;
		}

		public Func<QueryContext,IDataContext,Expression,object[],object>         GetElement;
		public Func<QueryContext,IDataContext,Expression,object[],IEnumerable<T>> GetIEnumerable;

		IEnumerable<T> MakeEnumerable(QueryContext qc, IDataContext dc, Expression expr, object[] ps)
		{
			yield return ConvertTo<T>.From(GetElement(qc, dc, expr, ps));
		}

		public Query<T> Next;

		#region GetQuery

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
							DataConnection.WriteTraceLine(
								"Expression test code generated: '" + testFile + "'.", 
								DataConnection.TraceSwitch.DisplayName);
#endif
						}

						query = new Query<T>(dataContext, expr);

						try
						{
							new ExpressionBuilder1(query, null).Build();
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
}
