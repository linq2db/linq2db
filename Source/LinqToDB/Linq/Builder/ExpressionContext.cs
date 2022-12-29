using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	class ExpressionContext : SequenceContextBase
	{
		public ExpressionContext(IBuildContext? parent, IBuildContext[] sequences, LambdaExpression lambda)
			: base(parent, sequences, lambda)
		{
		}

		public ExpressionContext(IBuildContext? parent, IBuildContext sequence, LambdaExpression lambda)
			: base(parent, sequence, lambda)
		{
		}

		public ExpressionContext(IBuildContext parent, IBuildContext sequence, LambdaExpression lambda, SelectQuery selectQuery)
			: base(parent, sequence, lambda)
		{
			SelectQuery = selectQuery;
		}

		public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			throw new InvalidOperationException();
		}

		public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new InvalidOperationException();
		}

		public override IBuildContext Clone(CloningContext context)
		{
			throw new NotImplementedException();
		}

		public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}
	}
}
