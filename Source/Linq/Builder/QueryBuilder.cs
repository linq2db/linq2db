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
			return BuildQuery<IEnumerable<T>>(expression);
		}

		public Func<IDataContext,Expression,T> BuildElement<T>(Expression expression)
		{
			return BuildQuery<T>(expression);
		}

		Func<IDataContext,Expression,T> BuildQuery<T>(Expression expr)
		{
			expr = expr.Transform(e => TramsformQuery(e));

			if (expr.Type != typeof(T))
				expr = Expression.Convert(expr, typeof(T));

			var l = Expression.Lambda<Func<IDataContext,Expression,T>>(
				expr, Query.DataContextParameter, Query.ExpressionParameter);

			return l.Compile();
		}

		Expression TramsformQuery(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant :
					{
						var c = (ConstantExpression)expression;
						if (c.Value is ITable)
							return new QueryExpression(new TableBuilder1(_query, expression), expression.Type);
						break;
					}

				case ExpressionType.MemberAccess:
					{
						if (typeof(ITable).IsSameOrParentOf(expression.Type))
							return new QueryExpression(new TableBuilder1(_query, expression), expression.Type);
						break;
					}

				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expression;

						if (call.Method.Name == "GetTable")
							if (typeof(ITable).IsSameOrParentOf(expression.Type))
								return new QueryExpression(new TableBuilder1(_query, expression), expression.Type);

						var attr = _query.MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(call.Method, a => a.Configuration);

						if (attr != null)
							return new QueryExpression(new TableFunctionBuilder(_query, expression), expression.Type);

						if (call.IsQueryable())
						{
							if (call.Object == null && call.Arguments.Count > 0 && call.Arguments[0] != null)
							{
								var qe = call.Arguments[0].Transform(e => TramsformQuery(e)) as QueryExpression;

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
