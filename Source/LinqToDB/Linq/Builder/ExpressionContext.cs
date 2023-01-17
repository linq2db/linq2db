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

		public override IBuildContext Clone(CloningContext context)
		{
			throw new NotImplementedException();
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return null;
		}
	}
}
