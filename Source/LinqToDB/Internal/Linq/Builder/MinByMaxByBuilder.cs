#if NET8_0_OR_GREATER

using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.MinBy), nameof(Queryable.MaxBy))]
	sealed class MinByMaxByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call is { IsQueryable: true, Arguments.Count: 2 };

		protected override BuildSequenceResult BuildMethodCall(
			ExpressionBuilder builder,
			MethodCallExpression methodCall,
			BuildInfo buildInfo)
		{

			var sourceExpression = methodCall.Arguments[0];
			var keySelector = methodCall.Arguments[1].UnwrapLambda();
			var isMinBy = methodCall.Method.Name is nameof(Queryable.MinBy);
			var sourceOrderBy = WindowFunctionHelpers.ExtractOrderByPart(sourceExpression, out var nonOrderedSource);

			// Transform MinBy(selector) -> OrderBy(selector).FirstOrDefault()
			// Transform MaxBy(selector) -> OrderByDescending(selector).FirstOrDefault()

			var elementType = methodCall.Method.GetGenericArguments()[0];
			var keySelectorType = keySelector.ReturnType;

			// Create OrderBy or OrderByDescending call
			var orderByMethod = isMinBy
				? Methods.Queryable.OrderBy.MakeGenericMethod(elementType, keySelectorType)
				: Methods.Queryable.OrderByDescending.MakeGenericMethod(elementType, keySelectorType);

			nonOrderedSource = BuildExpressionUtils.EnsureQueryable(nonOrderedSource, elementType);

			Expression orderedExpression = Expression.Call(
				orderByMethod,
				nonOrderedSource,
				methodCall.Arguments[1]);

			foreach (var (orderByLambda, isDescending) in sourceOrderBy)
			{
				orderedExpression = Expression.Call(
					typeof(Queryable),
					isDescending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy),
					[elementType, orderByLambda.ReturnType],
					orderedExpression,
					Expression.Quote(orderByLambda));
			}

			// Create FirstOrDefault() or First() call
			var firstCall = !elementType.IsNullableOrReferenceType && !buildInfo.IsSubQuery
				? Methods.Queryable.First.MakeGenericMethod(elementType)
				: Methods.Queryable.FirstOrDefault.MakeGenericMethod(elementType);

			var firstExpression = Expression.Call(firstCall, orderedExpression);

			// Build the transformed expression
			var result = builder.TryBuildSequence(new BuildInfo(buildInfo, firstExpression));
			return result;
		}
	}
}

#endif
