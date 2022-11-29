using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	sealed class HasUniqueKeyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("HasUniqueKey");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var keySelector = (LambdaExpression) methodCall.Arguments[1].Unwrap();
			var keyContext  = new SelectContext(buildInfo.Parent, keySelector, false, sequence);

			var keySql = builder.ConvertToSqlExpr(keyContext, new ContextRefExpression(keySelector.Parameters[0].Type, keyContext));

			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(keySql);

			sequence.SelectQuery.UniqueKeys.Add(placeholders.Select(p => p.Sql).ToArray());

			return new SubQueryContext(sequence);
		}
	}
}
