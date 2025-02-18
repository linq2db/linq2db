using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Linq.Builder
{
	abstract class MethodCallBuilder : ISequenceBuilder
	{
		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
			=> BuildMethodCall(builder, (MethodCallExpression)buildInfo.Expression, buildInfo);

		public virtual bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var mc = (MethodCallExpression)buildInfo.Expression;
			return mc.IsQueryable()
				? builder.IsSequence(new BuildInfo(buildInfo, mc.Arguments[0]))
				: false;
		}

		protected abstract BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo);
	}
}
