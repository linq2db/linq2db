#if NET9_0_OR_GREATER

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.CountBy))]
	sealed class CountByBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Queryable.CountBy };

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(_supportedMethods);

		protected override BuildSequenceResult BuildMethodCall(
			ExpressionBuilder builder,
			MethodCallExpression methodCall,
			BuildInfo buildInfo)
		{
			var sourceExpression = methodCall.Arguments[0];
			var keySelector = methodCall.Arguments[1].UnwrapLambda();

			// Transform CountBy(keySelector) -> GroupBy(keySelector).Select(g => new KeyValuePair<TKey, int>(g.Key, g.Count()))

			var elementType = methodCall.Method.GetGenericArguments()[0];
			var keyType = keySelector.ReturnType;

			// Create GroupBy call
			var groupByMethod = Methods.Queryable.GroupBy.MakeGenericMethod(elementType, keyType);

			var groupByExpression = Expression.Call(
				groupByMethod,
				sourceExpression,
				methodCall.Arguments[1]);

			// Create the Select call to transform to KeyValuePair<TKey, int>
			var groupingType = typeof(IGrouping<,>).MakeGenericType(keyType, elementType);
			var groupParam = Expression.Parameter(groupingType, "g");

			var keyProperty = Expression.Property(groupParam, "Key");

			// Use Enumerable.Count() instead of Queryable.Count() since IGrouping is not IQueryable
			var countCall = Expression.Call(
				Methods.Enumerable.Count.MakeGenericMethod(elementType),
				groupParam);

			var kvpType = typeof(System.Collections.Generic.KeyValuePair<,>).MakeGenericType(keyType, typeof(int));
			var kvpCtor = kvpType.GetConstructor(new[] { keyType, typeof(int) })!;
			var newKvp = Expression.New(kvpCtor, keyProperty, countCall);

			var selectLambda = Expression.Lambda(newKvp, groupParam);

			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(groupingType, kvpType);

			var selectExpression = Expression.Call(
				selectMethod,
				groupByExpression,
				Expression.Quote(selectLambda));

			// Build the transformed expression
			var result = builder.TryBuildSequence(new BuildInfo(buildInfo, selectExpression));
			return result;
		}
	}
}

#endif
