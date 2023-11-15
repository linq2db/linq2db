using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class WhereBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames = { "Where", "Having" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var isHaving  = methodCall.Method.Name == "Having";
			var sequence  = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence == null)
				return null;

			var condition = methodCall.Arguments[1].UnwrapLambda();

			if (sequence.SelectQuery.Select.IsDistinct        ||
			    sequence.SelectQuery.Select.TakeValue != null ||
			    sequence.SelectQuery.Select.SkipValue != null)
			{
				sequence = new SubQueryContext(sequence);
			}

			var result = builder.BuildWhere(buildInfo.Parent, sequence, condition: condition,
				checkForSubQuery: !isHaving, enforceHaving: isHaving, isTest: buildInfo.IsTest, isAggregationTest: buildInfo.AggregationTest);

			if (result == null)
				return null;

			result.SetAlias(condition.Parameters[0].Name);

			return result;
		}
	}
}
