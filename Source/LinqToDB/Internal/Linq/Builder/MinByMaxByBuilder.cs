#if NET8_0_OR_GREATER

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.MinBy), nameof(Queryable.MaxBy))]
	sealed class MinByMaxByBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Queryable.MinBy, Methods.Queryable.MaxBy };

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(_supportedMethods);

		protected override BuildSequenceResult BuildMethodCall(
			ExpressionBuilder builder,
			MethodCallExpression methodCall,
			BuildInfo buildInfo)
		{
			var sourceExpression = methodCall.Arguments[0];
			var keySelector = methodCall.Arguments[1].UnwrapLambda();
			var isMinBy = methodCall.Method.Name == nameof(Queryable.MinBy);

			// Transform MinBy(selector) -> OrderBy(selector).FirstOrDefault()
			// Transform MaxBy(selector) -> OrderByDescending(selector).FirstOrDefault()

			var elementType = methodCall.Method.GetGenericArguments()[0];
			var keySelectorType = keySelector.ReturnType;

			// Create OrderBy or OrderByDescending call
			var orderByMethod = isMinBy
				? Methods.Queryable.OrderBy.MakeGenericMethod(elementType, keySelectorType)
				: Methods.Queryable.OrderByDescending.MakeGenericMethod(elementType, keySelectorType);

			var orderedExpression = Expression.Call(
				orderByMethod,
				sourceExpression,
				methodCall.Arguments[1]);

			// Create FirstOrDefault() call  
			var firstOrDefaultMethod = Methods.Queryable.FirstOrDefault.MakeGenericMethod(elementType);
			var firstOrDefaultExpression = Expression.Call(firstOrDefaultMethod, orderedExpression);

			// Build the transformed expression
			var result = builder.TryBuildSequence(new BuildInfo(buildInfo, firstOrDefaultExpression));
			return result;
		}
	}
}

#endif
