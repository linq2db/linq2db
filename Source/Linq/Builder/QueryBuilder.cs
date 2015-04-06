using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;

	static class QueryBuilder
	{
		public static Func<IDataContext,Expression,IEnumerable<T>> BuildEnumerable<T>(Query<T> query)
		{
			var expr = query.Expression.Transform(e => TramsformQuery<T>(query, e));

			if (expr is QueryExpression<T>)
			{
				((QueryExpression<T>)expr).BuildQuery(query);
				return query.GetIEnumerable;
			}

			return BuildQuery<IEnumerable<T>>(expr);
		}

		public static Func<IDataContext,Expression,T> BuildElement<T>(Query<T> query)
		{
			var expr = query.Expression.Transform(e => TramsformQuery<T>(query, e));

			if (expr is QueryExpression<T>)
			{
				((QueryExpression<T>)expr).BuildQuery(query);
				return query.GetElement;
			}

			return BuildQuery<T>(expr);
		}

		static Func<IDataContext,Expression,TResult> BuildQuery<TResult>(Expression expr)
		{
			if (expr.Type != typeof(TResult))
				expr = Expression.Convert(expr, typeof(TResult));

			var l = Expression.Lambda<Func<IDataContext,Expression,TResult>>(
				expr, Query.DataContextParameter, Query.ExpressionParameter);

			return l.Compile();
		}

		static Expression TramsformQuery<T>(Query query, Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant :
					{
						var c = (ConstantExpression)expression;
						if (c.Value is ITable)
							return new QueryExpression<T>(new TableBuilder(query, expression));
						break;
					}

				case ExpressionType.MemberAccess:
					{
						if (typeof(ITable).IsSameOrParentOf(expression.Type))
							return new QueryExpression<T>(new TableBuilder(query, expression));
						break;
					}

				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expression;

						if (call.Method.Name == "GetTable")
							if (typeof(ITable).IsSameOrParentOf(expression.Type))
								return new QueryExpression<T>(new TableBuilder(query, expression));

						var attr = query.MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(call.Method, a => a.Configuration);

						if (attr != null)
							return new QueryExpression<T>(new TableFunctionBuilder(query, expression));

						if (call.IsQueryable())
						{
							if (call.Object == null && call.Arguments.Count > 0 && call.Arguments[0] != null)
							{
								var qe = call.Arguments[0].Transform(e => TramsformQuery<T>(query, e)) as QueryExpression<T>;

								if (qe != null)
								{
									switch (call.Method.Name)
									{
										case "Select": return SelectBuilder1.Translate(qe, call);
										case "Where" : return WhereBuilder1. Translate(qe, call));
									}
								}
							}
						}

						break;
					}
			}

			return expression;
		}
	}
}
