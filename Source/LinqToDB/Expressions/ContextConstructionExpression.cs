using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class ContextConstructionExpression : Expression
	{
		public ContextConstructionExpression(IBuildContext buildContext, Expression innerExpression, List<LambdaExpression>? postProcess = null)
		{
			BuildContext    = buildContext;
			InnerExpression = innerExpression;
			PostProcess     = postProcess;
		}

		public IBuildContext           BuildContext    { get; private set; }
		public Expression              InnerExpression { get; private set; }
		public List<LambdaExpression>? PostProcess     { get; private set; }

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => InnerExpression.Type;
		public override bool           CanReduce => true;
		public override Expression     Reduce()  => InnerExpression;

		public override string ToString()
		{
			return $"Ctx({BuildContextDebuggingHelper.GetContextInfo(BuildContext)}): {InnerExpression}";
		}

		public Expression Update(IBuildContext buildContext, Expression inner, List<LambdaExpression>? postProcess)
		{
			if (buildContext!= BuildContext || inner != InnerExpression || !ReferenceEquals(PostProcess, postProcess))
			{
				// this expression is mutable, so just update properties
				//return new ContextConstructionExpression(buildContext, inner, postProcess);
				BuildContext    = buildContext;
				InnerExpression = inner;
				PostProcess     = postProcess;
			}

			return this;
		}
	}
}
