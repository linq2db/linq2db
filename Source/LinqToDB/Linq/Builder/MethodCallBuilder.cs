using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	abstract class MethodCallBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			if (buildInfo.Expression.NodeType == ExpressionType.Call)
				return CanBuildMethodCall(builder, (MethodCallExpression)buildInfo.Expression, buildInfo);
			return false;
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return BuildMethodCall(builder, (MethodCallExpression)buildInfo.Expression, buildInfo);
		}

		public virtual bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var mc = (MethodCallExpression)buildInfo.Expression;

			if (!mc.IsQueryable())
				return false;

			return builder.IsSequence(new BuildInfo(buildInfo, mc.Arguments[0]));
		}

		public virtual bool IsAggregationContext(ExpressionBuilder builder, BuildInfo buildInfo) => false;

		protected abstract bool                CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo);
		protected abstract BuildSequenceResult BuildMethodCall   (ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo);
	}
}
