using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class HasUniqueKeyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("HasUniqueKey");
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (sequence == null)
				return null;

			var keySelector = methodCall.Arguments[1].UnwrapLambda();

			var keyExpr = SequenceHelper.PrepareBody(keySelector, sequence);
			var keySql  = builder.BuildSqlExpression(sequence, keyExpr, ProjectFlags.SQL);

			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(keySql);

			sequence.SelectQuery.UniqueKeys.Add(placeholders.Select(p => p.Sql).ToArray());

			return new SubQueryContext(sequence);
		}
	}
}
