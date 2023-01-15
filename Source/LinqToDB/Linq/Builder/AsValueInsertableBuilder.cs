using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	[BuildsMethodCall("AsValueInsertable")]
	sealed class AsValueInsertableBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression call, BuildInfo info)
		{
			var sequence = builder.BuildSequence(new BuildInfo(info, call.Arguments[0]));

			return new InsertBuilder.InsertContext(
				info.Parent,
				sequence,
				InsertBuilder.InsertContext.InsertTypeEnum.Insert,
				new SqlInsertStatement(sequence.SelectQuery), null)
			{
				RequiresSetters = true,
			};
		}

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression call, BuildInfo info, ParameterExpression? param)
			=> null;
	}
}
