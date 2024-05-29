using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class AsUpdatableBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("AsUpdatable");
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0])));
		}
	}
}
