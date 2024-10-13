using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	using LinqToDB.Internal.Expressions;

	[BuildsMethodCall("AsUpdatable")]
	sealed class AsUpdatableBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0])));
		}
	}
}
