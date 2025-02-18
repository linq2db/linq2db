using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.DisableFilterInternal))]
	sealed class DisableFiltersBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(Methods.LinqToDB.DisableFilterInternal);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			builder.PushDisableFiltersForExpression(methodCall.Arguments[0]);
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			builder.PopDisableFiltersForExpression();

			return sequence;
		}
	}
}
