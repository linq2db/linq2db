using System.Linq.Expressions;

using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
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
