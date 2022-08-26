using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Reflection;

	class DisableGroupingGuardBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(Methods.LinqToDB.DisableGuard);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var saveDisabledFlag = builder.IsGroupingGuardDisabled;
			builder.IsGroupingGuardDisabled = true;
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.IsGroupingGuardDisabled = saveDisabledFlag;

			return sequence;
		}
	}
}
