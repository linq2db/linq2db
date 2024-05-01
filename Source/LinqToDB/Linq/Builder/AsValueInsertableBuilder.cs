using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	sealed class AsValueInsertableBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("AsValueInsertable");
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var insertContext = new InsertBuilder.InsertContext(sequence,
				InsertBuilder.InsertContext.InsertTypeEnum.Insert, new SqlInsertStatement(sequence.SelectQuery), null);
			insertContext.RequiresSetters = true;

			return BuildSequenceResult.FromContext(insertContext);
		}
	}
}
