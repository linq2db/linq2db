using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	[BuildsMethodCall(nameof(LinqExtensions.AsSubQuery))]
	sealed class AsSubQueryBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			sequence.SelectQuery.DoNotRemove = true;

			if (methodCall.Arguments.Count > 1)
				sequence.SelectQuery.QueryName = (string?)methodCall.Arguments[1].EvaluateExpression();

			return new SubQueryContext(sequence);
		}
	}
}
