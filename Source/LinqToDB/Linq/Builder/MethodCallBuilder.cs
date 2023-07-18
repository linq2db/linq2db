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

		public IBuildContext? BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
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

		public Expression Expand(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var mc = (MethodCallExpression)buildInfo.Expression;

			var queryArgument = mc.Arguments[0];
			var corrected = builder.ExpandSequenceExpression(new BuildInfo(buildInfo, queryArgument));

			if (ReferenceEquals(corrected, queryArgument))
				return mc;

			if (corrected.Type != queryArgument.Type)
			{
				corrected = new SqlAdjustTypeExpression(corrected, queryArgument.Type, builder.MappingSchema);
			}

			var args = new Expression[mc.Arguments.Count];
			args[0] = corrected;
			for (var i = 1; i < args.Length; i++) 
				args[i] = mc.Arguments[i];

			return mc.Update(mc.Object, args);
		}

		public virtual bool IsAggregationContext(ExpressionBuilder builder, BuildInfo buildInfo) => false;

		protected abstract bool                 CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo);
		protected abstract IBuildContext?       BuildMethodCall   (ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo);
	}
}
