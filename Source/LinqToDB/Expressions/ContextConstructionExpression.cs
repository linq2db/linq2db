using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Linq.Builder;

	class ContextConstructionExpression : Expression
	{
		public ContextConstructionExpression(IBuildContext buildContext, Expression innerExpression)
		{
			BuildContext    = buildContext;
			InnerExpression = innerExpression;
		}
		 
		public IBuildContext           BuildContext    { get; private set; }
		public Expression              InnerExpression { get; private set; }

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => InnerExpression.Type;
		public override bool           CanReduce => true;
		public override Expression     Reduce()  => InnerExpression;

		public override string ToString()
		{
			return $"Ctx({BuildContextDebuggingHelper.GetContextInfo(BuildContext)}): {InnerExpression}";
		}

		public Expression Update(IBuildContext buildContext, Expression inner)
		{
			if (buildContext != BuildContext || inner != InnerExpression)
			{
				return new ContextConstructionExpression(buildContext, inner);
			}

			return this;
		}
	}
}
