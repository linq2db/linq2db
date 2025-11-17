#if NET6_0_OR_GREATER

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.DistinctBy))]
	sealed class DistinctByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.Enumerable.DistinctBy, Methods.Queryable.DistinctBy);

		static readonly MethodInfo _buildDistinctByViaRowNumberMethodInfo = MemberHelper.MethodOfGeneric(() => BuildDistinctByViaRowNumber<int>(null!, null!, null!, null!));

		static Expression BuildDistinctByViaRowNumber<T>(ExpressionBuilder builder, IBuildContext sequence, Expression[] partitionPart, (LambdaExpression lambda, bool isDescending)[] orderByPart)
		{
			var           contextRef = new ContextRefExpression(typeof(IQueryable<T>), sequence);
			IQueryable<T> query      = new ExpressionQueryImpl<T>(builder.DataContext, contextRef);

			var orderByPrepared = orderByPart
				.Select(o => (SequenceHelper.PrepareBody(o.lambda, sequence), o.isDescending))
				.ToArray();

			var rnCall = WindowFunctionHelpers.BuildRowNumber(partitionPart, orderByPrepared);

			var resultExpression = ExpressionHelpers.MakeCall((IQueryable<T> q, long rn) =>
					q
						.Select(e => new { Entity = e, RowNumber = rn })
						.Where(e => e.RowNumber == 1)
						.Select(e => e.Entity)
				,
				query.Expression,
				rnCall);

			return resultExpression;
		}

		static readonly MethodInfo _buildDistinctByViaOuterApplyMethodInfo = MemberHelper.MethodOfGeneric(() => BuildDistinctByViaOuterApply<int, int>(null!, null!, null!, null!));

		static Expression BuildDistinctByViaOuterApply<T, TSelector>(ExpressionBuilder builder, Expression nonOrdered, (LambdaExpression lambda, bool isDescending)[] orderByPart, 
			Expression<Func<T, TSelector>>                                                          selector)
		{
			IQueryable<T> query = new ExpressionQueryImpl<T>(builder.DataContext, nonOrdered);

			if (builder.DataContext.SqlProviderFlags.IsCommonTableExpressionsSupported)
				query = query.AsCte();

			var distinctCall = query
				.Select(selector)
				.Distinct();

			var innerQuery = query.Provider.CreateQuery<T>(WindowFunctionHelpers.ApplyOrderBy(query.Expression, orderByPart));

#pragma warning disable RS0030
			var outerApplyCall = ExpressionHelpers.MakeCall((IQueryable<TSelector> outer, IQueryable<T> inner, Expression<Func<T, TSelector>> sctor) =>
					from o in outer
					from i in inner
						.Where(i => sctor.Compile()(i)!.Equals(o))
						.Take(1)
					select i,
				distinctCall.Expression,
				innerQuery.Expression,
				Expression.Quote(selector)
			);
#pragma warning restore RS0030

			// Exposing Invoke call
			outerApplyCall = Internals.ExposeQueryExpression(builder.DataContext, outerApplyCall);

			var resultExpression = WindowFunctionHelpers.ApplyOrderBy(outerApplyCall, orderByPart);

			return resultExpression;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceExpression = methodCall.Arguments[0];

			var orderByPart = WindowFunctionHelpers.ExtractOrderByPart(sequenceExpression, out var nonOrderedPart);
			if (orderByPart.Length == 0)
				return BuildSequenceResult.Error(sequenceExpression, ErrorHelper.Error_DistinctByRequiresOrderBy);

			var selector = methodCall.Arguments[1].UnwrapLambda();

			if (builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
			{
				var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, nonOrderedPart));

				if (buildResult.BuildContext == null)
					return buildResult;

				var sequence      = buildResult.BuildContext;

				var partitionBody = SequenceHelper.PrepareBody(selector, sequence);

				var partitionPart = ExpressionHelpers.CollectMembers(partitionBody).ToArray();
				if (partitionPart.Length == 0)
					partitionPart = [Expression.Constant(1)];

				var buildMethod = _buildDistinctByViaRowNumberMethodInfo.MakeGenericMethod(sequence.ElementType);

				var expression = (Expression)buildMethod.InvokeExt(null, [builder, sequence, partitionPart, orderByPart])!;

				expression = WindowFunctionHelpers.ApplyOrderBy(expression, orderByPart);

				buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, expression));
				return buildResult;
			}

			// Rare case when outer apply is supported and window functions are not
			if (builder.DataContext.SqlProviderFlags.IsOuterApplyJoinSupportsCondition && buildInfo.Parent == null)
			{
				var elementType = selector.Parameters[0].Type;

				var buildMethod = _buildDistinctByViaOuterApplyMethodInfo.MakeGenericMethod(elementType, selector.Body.Type);

				var expression = (Expression)buildMethod.InvokeExt(null, [builder, nonOrderedPart, orderByPart, selector])!;

				var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, expression));
				return buildResult;
			}

			return BuildSequenceResult.NotSupported();
		}
	}
}

#endif
