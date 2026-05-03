#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.CountBy))]
	sealed class CountByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable;

		static readonly MethodInfo _transformToGroupByMethodInfo =
			typeof(CountByBuilder).GetMethod(nameof(TransformToGroupBy), BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException($"Method {nameof(TransformToGroupBy)} not found.");

		static Expression TransformToGroupBy<TSource, TKey>(Expression query, Expression<Func<TSource, TKey>> keySelector)
		{
			var groupByExpression = ExpressionHelpers.MakeCall((IQueryable<TSource> q, Expression<Func<TSource, TKey>> ks) 
				=> q.GroupBy(ks).Select(g => new KeyValuePair<TKey, int>(g.Key, g.Count())), query, Expression.Quote(keySelector));
			return groupByExpression;
		}

		protected override BuildSequenceResult BuildMethodCall(
			ExpressionBuilder builder,
			MethodCallExpression methodCall,
			BuildInfo buildInfo)
		{
			var sourceExpression = methodCall.Arguments[0];
			var keySelector = methodCall.Arguments[1].UnwrapLambda();

			if (methodCall.Arguments.Count >= 3)
			{
				var comparerArg = methodCall.Arguments[2];
				if (!builder.CanBeEvaluatedOnClient(comparerArg))
					return BuildSequenceResult.NotSupported();

				var comparer = builder.EvaluateExpression(methodCall.Arguments[2]);
				if (comparer != null)
					return BuildSequenceResult.NotSupported();
			}

			if (!typeof(IQueryable<>).IsSameOrParentOf(sourceExpression.Type))
			{
				sourceExpression = Expression.Call(
					Methods.Queryable.AsQueryable.MakeGenericMethod(keySelector.Parameters[0].Type),
					sourceExpression);
			}

			var transformMethod = _transformToGroupByMethodInfo.MakeGenericMethod(keySelector.Parameters[0].Type, keySelector.ReturnType);

			var transformedExpression = (Expression)transformMethod.InvokeExt(null, [sourceExpression, keySelector])!;

			var result = builder.TryBuildSequence(new BuildInfo(buildInfo, transformedExpression));

			return result;
		}
	}
}

#endif
