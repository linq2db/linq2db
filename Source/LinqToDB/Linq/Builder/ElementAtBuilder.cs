using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("ElementAt", "ElementAtOrDefault", "ElementAtAsync", "ElementAtOrDefaultAsync")]
	sealed class ElementAtBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo buildInfo, ExpressionBuilder builder)
			=> call.IsQueryable();

		public enum MethodKind
		{
			ElementAt,
			ElementAtOrDefault,
		}

		static MethodKind GetMethodKind(string methodName)
		{
			return methodName switch
			{
				"ElementAtOrDefault"      => MethodKind.ElementAtOrDefault,
				"ElementAtOrDefaultAsync" => MethodKind.ElementAtOrDefault,
				"ElementAt"               => MethodKind.ElementAt,
				"ElementAtAsync"          => MethodKind.ElementAt,
				_ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Not supported method.")
			};
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequqnceArg  = methodCall.Arguments[0];
			var elementAtArg = methodCall.Arguments[1];

			var genericArguments = methodCall.Method.GetGenericArguments();
			var methodKind = GetMethodKind(methodCall.Method.Name);

			Expression skipCall;
			Expression firstCall;

			if (methodCall.Method.DeclaringType == typeof(Queryable))
			{
				skipCall = Expression.Call(Methods.Queryable.Skip.MakeGenericMethod(genericArguments), sequqnceArg, elementAtArg);

				if (methodKind == MethodKind.ElementAt)
					firstCall = Expression.Call(Methods.Queryable.First.MakeGenericMethod(genericArguments), skipCall);
				else
					firstCall = Expression.Call(Methods.Queryable.FirstOrDefault.MakeGenericMethod(genericArguments), skipCall);
			}
			else
			{
				if (elementAtArg.NodeType == ExpressionType.Quote)
					skipCall = Expression.Call(Methods.LinqToDB.SkipLambda.MakeGenericMethod(genericArguments), sequqnceArg, elementAtArg);
				else
					skipCall = Expression.Call(Methods.Enumerable.Skip.MakeGenericMethod(genericArguments), sequqnceArg, elementAtArg);

				if (methodKind == MethodKind.ElementAt)
					firstCall = Expression.Call(Methods.Enumerable.First.MakeGenericMethod(genericArguments), skipCall);
				else
					firstCall = Expression.Call(Methods.Enumerable.FirstOrDefault.MakeGenericMethod(genericArguments), skipCall);
			}

			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, firstCall));

			return sequence;
		}
	}
}
