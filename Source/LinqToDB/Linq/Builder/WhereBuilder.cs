using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("Where", "Having")]
	sealed class WhereBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

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

			var result = builder.BuildWhere(
				buildInfo.Parent, sequence, condition: condition,
				checkForSubQuery: !isHaving, enforceHaving: isHaving, out var error);

			if (result == null)
				return BuildSequenceResult.Error(error ?? methodCall);

			result.SetAlias(condition.Parameters[0].Name);

			return BuildSequenceResult.FromContext(result);
		}
	}
}
