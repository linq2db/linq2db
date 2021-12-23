using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Reflection;
	using LinqToDB.Expressions;
	using System.Reflection;

	class DistinctBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Queryable.Distinct, Methods.Enumerable.Distinct, Methods.LinqToDB.SelectDistinct };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(_supportedMethods);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var prevSequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var sequence = new SubQueryContext(prevSequence);

			sequence.SelectQuery.Select.IsDistinct = true;

			// We do not need all fields for SelectDistinct
			//
			if (methodCall.IsSameGenericMethod(Methods.LinqToDB.SelectDistinct))
			{
				sequence.SelectQuery.Select.OptimizeDistinct = true;
			}
			else
			{
				// create all columns
				var sqlExpr = builder.ConvertToSqlExpr(prevSequence, new ContextRefExpression(methodCall.Arguments[0].Type, prevSequence));
				builder.UpdateNesting(sequence, sqlExpr);
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
