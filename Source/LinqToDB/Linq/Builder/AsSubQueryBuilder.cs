using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class AsSubQueryBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(nameof(LinqExtensions.AsSubQuery));
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			sequence.SelectQuery.DoNotRemove = true;

			if (methodCall.Arguments.Count > 1)
				sequence.SelectQuery.QueryName = (string?)methodCall.Arguments[1].EvaluateExpression();

			sequence = new SubQueryContext(sequence);
			
			return sequence;
		}
	}
}
