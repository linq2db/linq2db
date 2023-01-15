using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	[BuildsMethodCall("AsUpdatable")]
	sealed class AsUpdatableBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression call, BuildInfo info)
			=> builder.BuildSequence(new BuildInfo(info, call.Arguments[0]));
	}
}
