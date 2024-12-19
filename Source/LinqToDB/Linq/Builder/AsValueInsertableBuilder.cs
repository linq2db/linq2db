using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	[BuildsMethodCall("AsValueInsertable")]
	sealed class AsValueInsertableBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var insertContext = new InsertBuilder.InsertContext(sequence,
				InsertBuilder.InsertContext.InsertTypeEnum.Insert, new SqlInsertStatement(sequence.SelectQuery), null, false)
			{
				RequiresSetters = true
			};

			return BuildSequenceResult.FromContext(insertContext);
		}
	}
}
