using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	[BuildsMethodCall(nameof(DataExtensions.SelectQuery))]
	sealed class SelectQueryBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(DataExtensions.SelectQueryMethodInfo);

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return new SelectContext(buildInfo.Parent,
				builder,
				methodCall.Arguments[1].UnwrapLambda().Body,
				buildInfo.SelectQuery, buildInfo.IsSubQuery);
		}

		public override bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
			=> true;
	}
}
