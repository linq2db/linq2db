using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;

	class ExpressionBuilder1
	{
		public ExpressionBuilder1(MappingSchema mappingSchema)
		{
			MappingSchema = mappingSchema;
		}

		public readonly MappingSchema MappingSchema;

		public Func<IDataContext,Expression,IEnumerable<T>> BuildEnumerable<T>(Expression expression)
		{
			var expr = (QueryExpression)expression.Transform(e => TramsformQuery(e));
			return expr.BuildEnumerable<T>();
		}

		public Func<IDataContext,Expression,T> BuildElement<T>(Expression expression)
		{
			var expr = (QueryExpression)expression.Transform(e => TramsformQuery(e));
			return expr.BuildElement<T>();
		}

		Expression TramsformQuery(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Constant :
					{
						var c = (ConstantExpression)expression;

						if (c.Value is ITable)
							return new QueryExpression(new TableBuilder1(expression));

						break;
					}

				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expression;

						if (call.Method.Name == "GetTable")
							if (typeof(ITable).IsSameOrParentOf(expression.Type))
								return new QueryExpression(new TableBuilder1(expression));

						var attr = MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(call.Method, a => a.Configuration);

						if (attr != null)
							return new QueryExpression(new TableFunctionBuilder(expression));

						if (call.IsQueryable())
						{
							if (call.Object == null && call.Arguments.Count > 0 && call.Arguments[0] != null)
							{
								var qe = call.Arguments[0].Transform(e => TramsformQuery(e)) as QueryExpression;
								if (qe != null)
								{
									switch (call.Method.Name)
									{
										case "Select": return qe.AddClause(new SelectBuilder1(call));
										case "Where" : return qe.AddClause(new WhereBuilder1 (call));
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
