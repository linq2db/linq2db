using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.AsSubQuery))]
	sealed class AsSubQueryBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence         = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			
			sequence.SelectQuery.DoNotRemove = true;
			if (methodCall.Arguments.Count > 1)
				sequence.SelectQuery.QueryName = (string?)builder.EvaluateExpression(methodCall.Arguments[1]);

			sequence = new AsSubqueryContext(sequence);

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
