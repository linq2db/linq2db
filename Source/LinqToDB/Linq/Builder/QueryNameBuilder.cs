using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;

	[BuildsMethodCall(nameof(LinqExtensions.QueryName))]
	sealed class QueryNameBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence    = builder.BuildSequence(new(buildInfo, methodCall.Arguments[0]));
			var elementType = methodCall.Arguments[0].Type.GetGenericArguments()[0];

			sequence.SelectQuery.QueryName = (string?)methodCall.Arguments[1].EvaluateExpression();

			if (typeof(IGrouping<,>).IsSameOrParentOf(elementType))
			{
				// It is special case when we are trying to make subquery from GroupBy

				sequence.ConvertToIndex(null, 0, ConvertFlags.Key);
				var param  = Expression.Parameter(elementType);
				var lambda = Expression.Lambda(Expression.PropertyOrField(param, "Key"), param);

				sequence = new SubQueryContext(sequence);
				return new SelectContext(buildInfo.Parent, lambda, buildInfo.IsSubQuery, sequence);
			}
			
			return new SubQueryContext(sequence);
		}
	}
}
