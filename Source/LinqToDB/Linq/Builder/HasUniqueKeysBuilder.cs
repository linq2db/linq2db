using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("HasUniqueKey")]
	sealed class HasUniqueKeyBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo buildInfo, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (buildResult.BuildContext == null)
				return buildResult;
				
			var sequence = buildResult.BuildContext;

			var keySelector = methodCall.Arguments[1].UnwrapLambda();

			var keyExpr = SequenceHelper.PrepareBody(keySelector, sequence);
			var keySql  = builder.BuildSqlExpression(sequence, keyExpr);

			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(keySql, false);

			sequence.SelectQuery.UniqueKeys.Add(placeholders.Select(p => p.Sql).ToArray());

			return BuildSequenceResult.FromContext(new SubQueryContext(sequence));
		}
	}
}
