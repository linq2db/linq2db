using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	abstract class QueryExpression : Expression
	{
		protected QueryExpression(IExpressionBuilder expressionBuilder)
		{
			AddBuilder(First = expressionBuilder);
		}

		protected Type               QueryType;
		protected IExpressionBuilder First;
		protected IExpressionBuilder Last;

		public override Type           Type      { get { return QueryType;  } }
		public override bool           CanReduce { get { return true;  } }
		public override ExpressionType NodeType  { get { return ExpressionType.Extension; } }

		public QueryExpression AddBuilder(IExpressionBuilder expressionBuilder)
		{
			for (var builder = expressionBuilder; builder != null; builder = builder.Next)
			{
				if (Last != null)
				{
					Last.Next = expressionBuilder;
					expressionBuilder.Prev = Last;
				}

				QueryType = expressionBuilder.Type;
				Last      = expressionBuilder;
			}

			return this;
		}
	}

	class QueryExpression<T> : QueryExpression
	{
		public QueryExpression(IExpressionBuilder expressionBuilder) : base(expressionBuilder)
		{
		}

		public override Expression Reduce()
		{
			var expr = Last.BuildQuery<T>();

			QueryType = expr.Type;

			return expr;
		}

		public void BuildQuery(Query<T> query)
		{
			Last.BuildQuery(query);
		}
	}
}
