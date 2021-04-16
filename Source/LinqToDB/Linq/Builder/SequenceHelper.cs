using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	static class SequenceHelper
	{
		public static Expression PrepareBody(LambdaExpression lambda, params IBuildContext[] sequences)
		{
			var body = lambda.GetBody(sequences
				.Select((s, idx) => (Expression)new ContextRefExpression(lambda.Parameters[idx].Type, s)).ToArray());

			return body;
		}

		public static bool IsSameContext(Expression? expression, IBuildContext context)
		{
			return expression == null || expression is ContextRefExpression contextRef && contextRef.BuildContext == context;
		}

		public static Expression? CorrectExpression(Expression? expression, IBuildContext current, IBuildContext underlying)
		{
			if (expression != null)
			{
				var root = current.Builder.GetRootObject(expression);
				if (root is ContextRefExpression refExpression)
				{
					if (refExpression.BuildContext == current)
					{
						var contextRefExpression = new ContextRefExpression(root.Type, underlying);
						expression = expression.Replace(root, contextRefExpression);
					};
				}
			}

			return expression;
		}
		
	}
}
