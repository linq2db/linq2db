#if NET6_0_OR_GREATER

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common.Internal;
using LinqToDB.Expressions;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.DistinctBy))]
	sealed class DistinctByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(Methods.Enumerable.DistinctBy, Methods.Queryable.DistinctBy);


		static readonly MethodInfo _buildDistinctByViaIndexMethodInfo = MemberHelper.MethodOfGeneric(() => BuildDistinctByViaIndex<int, int>(null!, null!, null!));

		static Expression BuildDistinctByViaIndex<T, TValue>(ExpressionBuilder builder, IBuildContext sequence, Expression<Func<T, TValue>> selector)
		{
			var contextRef = new ContextRefExpression(typeof(IQueryable<T>), sequence);

			IQueryable<T> query = new ExpressionQueryImpl<T>(builder.DataContext, contextRef);

			query = query
				.OrderBy(selector)
				.Select((e, index) => new MTuple<T, int> { Item1 = e, Item2 = index })
				.Where(e => e.Item2 == 0)
				.Select(e => e.Item1);

			return query.Expression;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

			if (builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
			{
				var selector = methodCall.Arguments[1].UnwrapLambda();

				var buildMethod = _buildDistinctByViaIndexMethodInfo.MakeGenericMethod(sequence.ElementType, selector.Body.Type);

				var expression = (Expression)buildMethod.Invoke(null, [builder, sequence, selector])!;

				buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, expression));

				return buildResult;
			}

			return BuildSequenceResult.NotSupported();
		}
	}
}

#endif
