using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	static class SequenceHelper
	{
		public static Expression? CorrectExpression(Expression? expression, IBuildContext current, IBuildContext underlying)
		{
			if (expression != null)
			{
				var root = expression.GetRootObject(current.Builder.MappingSchema);
				if (root is ContextRefExpression refExpression)
				{
					if (refExpression.BuildContext == current)
					{
						expression = expression.Replace(root, new ContextRefExpression(root.Type, underlying));
					};
				}
			}

			return expression;
		}
		
	}
}
