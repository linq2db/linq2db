using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.QueryName))]
	sealed class QueryNameBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence    = builder.BuildSequence(new(buildInfo, methodCall.Arguments[0]));

			sequence.SelectQuery.QueryName = (string?)builder.EvaluateExpression(methodCall.Arguments[1]);
			sequence = new SubQueryContext(sequence);

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
