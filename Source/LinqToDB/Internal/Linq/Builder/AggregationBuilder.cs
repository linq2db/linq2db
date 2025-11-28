using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Average", "Min", "Max", "Sum", "Count", "LongCount")]
	sealed class AggregationBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable();

		public static Expression BuildAggregateExecuteExpression(MethodCallExpression methodCall)
		{
			if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));

			var elementType = TypeHelper.GetEnumerableElementType(methodCall.Arguments[0].Type);
			var sourceParam = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "source");
			var resultType  = methodCall.Type;

			Type[] typeArguments = methodCall.Method.IsGenericMethod
				? methodCall.Method.GetGenericArguments().Length == 2 ? [elementType, resultType] : [elementType]
				: [];

			var aggregationBody = Expression.Call(typeof(Enumerable), methodCall.Method.Name,
				typeArguments,
				[sourceParam, ..methodCall.Arguments.Skip(1).Select(a => a.Unwrap())]
			);

			var aggregationLambda = Expression.Lambda(aggregationBody, sourceParam);

			var executeExpression = Expression.Call(typeof(LinqExtensions), nameof(LinqExtensions.AggregateExecute), [elementType, resultType], methodCall.Arguments[0], aggregationLambda);

			return executeExpression;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var newMethodCall = methodCall;

			if (!buildInfo.IsSubQuery)
			{
				var aggregator = BuildAggregateExecuteExpression(newMethodCall);

				var result = builder.TryBuildSequence(new BuildInfo(buildInfo, aggregator));

				return result;
			}

			return BuildSequenceResult.NotSupported();
		}
	}
}
