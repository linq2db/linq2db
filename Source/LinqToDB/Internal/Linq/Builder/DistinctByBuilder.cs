#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.DistinctBy))]
	sealed class DistinctByBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call is { IsQueryable: true, Arguments.Count: 2 };

		static readonly MethodInfo _buildDistinctByViaRowNumberMethodInfo = MemberHelper.MethodOfGeneric(() => BuildDistinctByViaRowNumber<int>(null!, null!, null!, null!));

		static Expression BuildDistinctByViaRowNumber<T>(ExpressionBuilder builder, IBuildContext sequence, Expression[] partitionPart, (LambdaExpression lambda, bool isDescending, Sql.NullsPosition nulls)[] orderByPart)
		{
			var           contextRef = new ContextRefExpression(typeof(IQueryable<T>), sequence);
			IQueryable<T> query      = new ExpressionQueryImpl<T>(builder.DataContext, contextRef);

			var orderByPrepared = orderByPart
				.Select(o => (SequenceHelper.PrepareBody(o.lambda, sequence), o.isDescending, o.nulls))
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

		static Expression BuildDistinctByViaOuterApply<T, TSelector>(ExpressionBuilder builder, Expression nonOrdered, (LambdaExpression lambda, bool isDescending, Sql.NullsPosition nulls)[] orderByPart,
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

			var extractedOrderBy = WindowFunctionHelpers.ExtractOrderByPart(sequenceExpression, out var nonOrderedPart);
			if (extractedOrderBy.Length == 0)
				return BuildSequenceResult.Error(sequenceExpression, ErrorHelper.Error_DistinctByRequiresOrderBy);

			// The preceding OrderBy is extracted here and bypasses OrderByBuilder, so resolve the configured default
			// NULLS position for keys that did not specify one (null), matching OrderByBuilder behavior. An explicit
			// position (including Sql.NullsPosition.None) is kept as-is so it can opt out of the configured default.
			var defaultNulls = builder.DataOptions.SqlOptions.DefaultNullsPosition;
			var orderByPart  = extractedOrderBy
				.Select(o => (o.lambda, o.isDescending, o.nulls ?? defaultNulls))
				.ToArray();

			var selector = methodCall.Arguments[1].UnwrapLambda();

			// On providers that support PostgreSQL-style DISTINCT ON, lower DistinctBy to it directly instead of the
			// ROW_NUMBER() emulation: the key selector becomes the ON list and the preceding OrderBy is forced to lead
			// with those keys (the syntax requires it). Falls through to ROW_NUMBER when the key has no usable SQL form.
			if (builder.DataContext.SqlProviderFlags.IsDistinctOnSupported)
			{
				var orderedExpression = WindowFunctionHelpers.ApplyOrderBy(nonOrderedPart, orderByPart);

				var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, orderedExpression));
				if (buildResult.BuildContext == null)
					return buildResult;

				var sequence      = buildResult.BuildContext;
				var partitionBody = SequenceHelper.PrepareBody(selector, sequence);
				var keySql        = builder.BuildSqlExpression(sequence, partitionBody, BuildPurpose.Sql, BuildFlags.ForKeys);

				if (SequenceHelper.IsSqlReady(keySql))
				{
					var onExpressions = ExpressionBuilder.CollectDistinctPlaceholders(keySql, false)
						.Select(static p => p.Sql)
						.Where(static s => !QueryHelper.IsConstant(s))
						.ToList();

					// An empty ON list (e.g. DistinctBy(x => 1)) has no valid DISTINCT ON form — fall back to ROW_NUMBER below.
					if (onExpressions.Count > 0)
					{
						ApplyDistinctOn(sequence.SelectQuery, onExpressions);
						return BuildSequenceResult.FromContext(sequence);
					}
				}
			}

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

		// PostgreSQL/DuckDB require ORDER BY to begin with the DISTINCT ON expressions. Move (or synthesize) the ON
		// keys to the front of the existing OrderBy, preserving the user's ordering for the remaining keys, then mark
		// the select clause as DISTINCT ON.
		static void ApplyDistinctOn(SelectQuery selectQuery, List<ISqlExpression> onExpressions)
		{
			var items   = selectQuery.OrderBy.Items;
			var leading = new List<SqlOrderByItem>(onExpressions.Count);

			foreach (var on in onExpressions)
			{
				var existing = items.Find(i => i.Expression.Equals(on));
				leading.Add(existing ?? new SqlOrderByItem(on, false, false, Sql.NullsPosition.None));
			}

			var tail = items.FindAll(i => !onExpressions.Exists(on => on.Equals(i.Expression)));

			items.Clear();
			items.AddRange(leading);
			items.AddRange(tail);

			selectQuery.Select.IsDistinct = true;
			selectQuery.Select.DistinctOn = onExpressions;
		}
	}
}

#endif
