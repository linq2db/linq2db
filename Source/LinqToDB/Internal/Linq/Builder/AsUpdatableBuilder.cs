using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("AsUpdatable")]
	sealed class AsUpdatableBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0])));
		}
	}
}
