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

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var isHaving  = methodCall.Method.Name == "Having";
			var sequenceResult  = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequenceResult.BuildContext == null)
				return sequenceResult;

			var sequence = sequenceResult.BuildContext;

			var condition = methodCall.Arguments[1].UnwrapLambda();

			if (sequence.SelectQuery.Select.IsDistinct        ||
			    sequence.SelectQuery.Select.TakeValue != null ||
			    sequence.SelectQuery.Select.SkipValue != null)
			{
				sequence = new SubQueryContext(sequence);
			}

			var result = builder.BuildWhere(buildInfo.Parent, sequence, condition : condition,
				checkForSubQuery : !isHaving, enforceHaving : isHaving, isTest : buildInfo.IsTest);

			if (result == null)
				return BuildSequenceResult.Error(methodCall);

			result.SetAlias(condition.Parameters[0].Name);

			return BuildSequenceResult.FromContext(result);
		}
	}
}
