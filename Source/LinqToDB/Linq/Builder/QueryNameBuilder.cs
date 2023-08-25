using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;

	sealed class QueryNameBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(new[] { nameof(LinqExtensions.QueryName) });
		}

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
				sequence = new SelectContext(buildInfo.Parent, lambda, sequence);
			}
			else
			{
				sequence = new SubQueryContext(sequence);
			}

			return sequence;
		}
	}
}
