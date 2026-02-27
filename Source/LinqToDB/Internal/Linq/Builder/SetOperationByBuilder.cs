#if NET8_0_OR_GREATER

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.ExceptBy), nameof(Queryable.UnionBy), nameof(Queryable.IntersectBy))]
	sealed class SetOperationByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.Enumerable.ExceptBy, Methods.Queryable.ExceptBy)
			|| call.IsSameGenericMethod(Methods.Enumerable.UnionBy, Methods.Queryable.UnionBy)
			|| call.IsSameGenericMethod(Methods.Enumerable.IntersectBy, Methods.Queryable.IntersectBy);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			// Get the generic arguments
			var sourceType = methodCall.Method.GetGenericArguments()[0];
			var keyType = methodCall.Method.GetGenericArguments()[1];

			// Arguments: source, second, keySelector
			var sourceExpression = methodCall.Arguments[0];
			var secondExpression = methodCall.Arguments[1];
			var keySelector = methodCall.Arguments[2].UnwrapLambda();

			var methodName = methodCall.Method.Name;

			Expression transformedExpression;

			if (methodName == "ExceptBy")
			{
				// source.ExceptBy(second, keySelector)
				// → source.Where(x => !second.Select(keySelector).Distinct().Contains(keySelector(x)))
				transformedExpression = BuildExceptBy(sourceExpression, secondExpression, keySelector, sourceType, keyType);
			}
			else if (methodName == "IntersectBy")
			{
				// source.IntersectBy(second, keySelector)
				// → source.Where(x => second.Select(keySelector).Distinct().Contains(keySelector(x)))
				transformedExpression = BuildIntersectBy(sourceExpression, secondExpression, keySelector, sourceType, keyType);
			}
			else // UnionBy
			{
				// source.UnionBy(second, keySelector)
				// → source.Union(second).DistinctBy(keySelector) + OrderBy wrapper
				// Note: DistinctBy requires OrderBy, so we'll just use Concat + Distinct on keys + Where
				transformedExpression = BuildUnionBy(sourceExpression, secondExpression, keySelector, sourceType, keyType);
			}

			return builder.TryBuildSequence(new BuildInfo(buildInfo, transformedExpression));
		}

		static Expression BuildExceptBy(Expression source, Expression second, LambdaExpression keySelector, Type sourceType, Type keyType)
		{
			// second.Select(keySelector).Distinct()
			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(keyType, keyType);
			var distinctMethod = Methods.Queryable.Distinct.MakeGenericMethod(keyType);

			var secondKeys = Expression.Call(null, selectMethod, second, Expression.Quote(keySelector));
			var distinctKeys = Expression.Call(null, distinctMethod, secondKeys);

			// Create parameter for Where clause: x => !distinctKeys.Contains(keySelector(x))
			var parameter = Expression.Parameter(sourceType, "x");
			var keySelectorBody = keySelector.GetBody(parameter);

			var containsMethod = Methods.Queryable.Contains.MakeGenericMethod(keyType);
			var containsCall = Expression.Call(null, containsMethod, distinctKeys, keySelectorBody);
			var notContains = Expression.Not(containsCall);

			var whereLambda = Expression.Lambda(notContains, parameter);
			var whereMethod = Methods.Queryable.Where.MakeGenericMethod(sourceType);

			return Expression.Call(null, whereMethod, source, Expression.Quote(whereLambda));
		}

		static Expression BuildIntersectBy(Expression source, Expression second, LambdaExpression keySelector, Type sourceType, Type keyType)
		{
			// second.Select(keySelector).Distinct()
			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(keyType, keyType);
			var distinctMethod = Methods.Queryable.Distinct.MakeGenericMethod(keyType);

			var secondKeys = Expression.Call(null, selectMethod, second, Expression.Quote(keySelector));
			var distinctKeys = Expression.Call(null, distinctMethod, secondKeys);

			// Create parameter for Where clause: x => distinctKeys.Contains(keySelector(x))
			var parameter = Expression.Parameter(sourceType, "x");
			var keySelectorBody = keySelector.GetBody(parameter);

			var containsMethod = Methods.Queryable.Contains.MakeGenericMethod(keyType);
			var containsCall = Expression.Call(null, containsMethod, distinctKeys, keySelectorBody);

			var whereLambda = Expression.Lambda(containsCall, parameter);
			var whereMethod = Methods.Queryable.Where.MakeGenericMethod(sourceType);

			return Expression.Call(null, whereMethod, source, Expression.Quote(whereLambda));
		}

		static Expression BuildUnionBy(Expression source, Expression second, LambdaExpression keySelector, Type sourceType, Type keyType)
		{
			// UnionBy needs distinct elements by key from both sources
			// Transform to: source.Concat(second).GroupBy(keySelector).Select(g => g.First())

			var concatMethod = Methods.Queryable.Concat.MakeGenericMethod(sourceType);
			var concatenated = Expression.Call(concatMethod, source, second);

			// GroupBy(keySelector)
			var groupByMethod = Methods.Queryable.GroupBy.MakeGenericMethod(sourceType, keyType);
			var grouped = Expression.Call(groupByMethod, concatenated, Expression.Quote(keySelector));
			var groupingType = typeof(IGrouping<,>).MakeGenericType(keyType, sourceType);
			var groupParam = Expression.Parameter(groupingType, "g");
			var firstMethod = Methods.Queryable.First.MakeGenericMethod(sourceType);
			var firstCall = Expression.Call(firstMethod, groupParam);
			var selectLambda = Expression.Lambda(firstCall, groupParam);

			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(groupingType, sourceType);
			return Expression.Call(selectMethod, grouped, Expression.Quote(selectLambda));
		}
	}
}

#endif
