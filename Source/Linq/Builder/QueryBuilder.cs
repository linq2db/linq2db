using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;

	class QueryBuilder
	{
		public QueryBuilder(Query query)
		{
			_query = query;
		}

		readonly Query _query;

		public Func<IDataContext,Expression,IEnumerable<T>> BuildEnumerable<T>(Expression expression)
		{
			return BuildQuery<IEnumerable<T>,T>(expression);
		}

		public Func<IDataContext,Expression,T> BuildElement<T>(Expression expression)
		{
			return BuildQuery<T,T>(expression);
		}

		Func<IDataContext,Expression,TResult> BuildQuery<TResult,TItem>(Expression expr)
		{
			expr = expr.Transform(e => TramsformQuery<TItem>(e));
			//expr = expr.Transform(e => e is QueryExpression<TItem> ? e.Reduce() : e);

			if (expr.Type != typeof(TResult))
				expr = Expression.Convert(expr, typeof(TResult));

			var l = Expression.Lambda<Func<IDataContext,Expression,TResult>>(
				expr, Query.DataContextParameter, Query.ExpressionParameter);

			return l.Compile();
		}

		Expression TramsformQuery<T>(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant :
					{
						var c = (ConstantExpression)expression;
						if (c.Value is ITable)
							return new QueryExpression<T>(new TableBuilder1(_query, expression), expression.Type);
						break;
					}

				case ExpressionType.MemberAccess:
					{
						if (typeof(ITable).IsSameOrParentOf(expression.Type))
							return new QueryExpression<T>(new TableBuilder1(_query, expression), expression.Type);
						break;
					}

				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expression;

						if (call.Method.Name == "GetTable")
							if (typeof(ITable).IsSameOrParentOf(expression.Type))
								return new QueryExpression<T>(new TableBuilder1(_query, expression), expression.Type);

						var attr = _query.MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(call.Method, a => a.Configuration);

						if (attr != null)
							return new QueryExpression<T>(new TableFunctionBuilder(_query, expression), expression.Type);

						if (call.IsQueryable())
						{
							if (call.Object == null && call.Arguments.Count > 0 && call.Arguments[0] != null)
							{
								var qe = call.Arguments[0].Transform(e => TramsformQuery<T>(e)) as QueryExpression<T>;

								if (qe != null)
								{
									switch (call.Method.Name)
									{
										case "Select": return qe.AddBuilder(new SelectBuilder1(call), expression.Type);
										case "Where" : return qe.AddBuilder(new WhereBuilder1 (call), expression.Type);
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
