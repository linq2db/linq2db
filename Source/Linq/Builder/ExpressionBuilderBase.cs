using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	abstract class ExpressionBuilderBase
	{
		protected ExpressionBuilderBase(Expression expression)
		{
			Expression = expression;
		}

		public Type            Type { get { return Expression.Type; } }
		public Expression      Expression;

		public ExpressionBuilderBase Prev;
		public ExpressionBuilderBase Next;

		private QueryExpression _query;
		public  QueryExpression  Query
		{
			get { return _query; }
			set { _query = value; Init(); }
		}

		protected abstract void           Init    ();
		public    abstract SqlBuilderBase GetSqlBuilder();
	}
}
