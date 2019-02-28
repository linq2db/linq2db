using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Parser.Builders
{
	public abstract class MethodCallBuilder : BaseBuilder
	{
		public override bool CanBuild(Expression expression)
		{
			if (expression.NodeType != ExpressionType.Call)
				return false;
			return CanBuild((MethodCallExpression)expression);
		}

		public abstract MethodInfo[] SupportedMethods();

		public override Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo, Expression expression)
		{
			return BuildSequence(builder, parseBuildInfo, (MethodCallExpression)expression);
		}

		public virtual bool CanBuild(MethodCallExpression methodExpression)
		{
			return methodExpression.IsOneOfMethods(SupportedMethods());
		}

		public abstract Sequence BuildSequence(ModelParser builder, ParseBuildInfo parseBuildInfo,
			MethodCallExpression methodCallExpression);
	}
}
