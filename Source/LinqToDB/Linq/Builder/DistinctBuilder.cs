using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Reflection;
	using LinqToDB.Expressions;

	class DistinctBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(Methods.Queryable.Distinct, Methods.Enumerable.Distinct, Methods.LinqToDB.SelectDistinct);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var sql      = sequence.SelectQuery;

			if (sql.Select.TakeValue != null || sql.Select.SkipValue != null)
				sequence = new SubQueryContext(sequence);

			sequence.SelectQuery.Select.IsDistinct = true;

			// We do not need all fields for SelectDistinct
			//
			if (!methodCall.IsSameGenericMethod(Methods.LinqToDB.SelectDistinct))
			{
				sequence.ConvertToIndex(null, 0, ConvertFlags.All);
			}

			return sequence;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}
	}
}
