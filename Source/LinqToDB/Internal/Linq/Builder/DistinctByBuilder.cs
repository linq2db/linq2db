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

		static Expression BuildDistinctByViaRowNumber<T>(ExpressionBuilder builder, IBuildContext sequence, Expression[] partitionPart, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderByPart)
		{
			var           contextRef = new ContextRefExpression(typeof(IQueryable<T>), sequence);
			IQueryable<T> query      = new ExpressionQueryImpl<T>(builder.DataContext, contextRef);

			var rnCall = WindowFunctionHelpers.BuildRowNumber(partitionPart, orderByPart);

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
			var selector           = methodCall.Arguments[1].UnwrapLambda();

			BuildSequenceResult                   buildResult;
			IBuildContext                         sequence;
			(Expression expr, bool descending, Sql.NullsPosition nulls)[] captured;

			// Isolate OrderBy state across the *inner* sequence build only. DistinctBy
			// consumes the upstream OrderBy semantically (row-number partition order);
			// the inner registration must not leak to downstream readers. The outer
			// ApplyOrderBy below runs OUTSIDE the scope so OrderByBuilder publishes the
			// result's OrderBy to _currentOrderBy where downstream consumers (e.g. CteUnion)
			// see it.
			using (builder.IsolateOrderBy())
			{
				// OrderByBuilder fires inside the recursive TryBuildSequence and populates
				// _currentOrderBy with prepared bodies tied to the freshly-built sequence's
				// context. Snapshot the entries before the scope disposes.
				buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceExpression));
				if (buildResult.BuildContext == null)
				{
					// A non-SQL preceding ordering (e.g. an OrderBy/ThenBy carrying a custom IComparer<T>) has no SQL form,
					// so the sequence build fails with a generic "cannot convert" error. When no SQL-translatable ordering
					// key exists at all, surface the clearer DistinctBy error instead, matching the no-OrderBy case below.
					if (WindowFunctionHelpers.ExtractOrderByPart(sequenceExpression, out _).Length == 0)
						return BuildSequenceResult.Error(sequenceExpression, ErrorHelper.Error_DistinctByRequiresOrderBy);

					return buildResult;
				}

				sequence = buildResult.BuildContext;
				captured = builder.CurrentOrderBy is { Count: > 0 } current ? current.ToArray() : [];
			}

			if (captured.Length == 0)
				return BuildSequenceResult.Error(sequenceExpression, ErrorHelper.Error_DistinctByRequiresOrderBy);

			// On providers that support PostgreSQL-style DISTINCT ON, lower DistinctBy to it directly instead of the
			// ROW_NUMBER() emulation: the key selector becomes the ON list and the preceding OrderBy — already applied
			// to the sequence built above — is reordered to lead with those keys. Reuses that sequence (no second
			// TryBuildSequence, which would leak an extra table reference). Falls through to ROW_NUMBER when the key
			// has no usable SQL form (empty ON list, e.g. DistinctBy(x => 1)).
			if (builder.DataContext.SqlProviderFlags.IsDistinctOnSupported)
			{
				var partitionBody = SequenceHelper.PrepareBody(selector, sequence);
				var keySql        = builder.BuildSqlExpression(sequence, partitionBody, BuildPurpose.Sql, BuildFlags.ForKeys);

				if (SequenceHelper.IsSqlReady(keySql))
				{
					var onExpressions = ExpressionBuilder.CollectDistinctPlaceholders(keySql, false)
						.Select(static p => p.Sql)
						.Where(static s => !QueryHelper.IsConstant(s))
						.ToList();

					if (onExpressions.Count > 0)
					{
						ApplyDistinctOn(sequence.SelectQuery, onExpressions);
						return BuildSequenceResult.FromContext(sequence);
					}
				}
			}

			if (builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
			{
				var partitionBody = SequenceHelper.PrepareBody(selector, sequence);

				var partitionPart = ExpressionHelpers.CollectMembers(partitionBody).ToArray();
				if (partitionPart.Length == 0)
					partitionPart = [Expression.Constant(1)];

				var buildMethod = _buildDistinctByViaRowNumberMethodInfo.MakeGenericMethod(sequence.ElementType);
				var expression  = (Expression)buildMethod.InvokeExt(null, [builder, sequence, partitionPart, captured])!;

				// Re-apply OrderBy on the outer result. The OVER clause's ORDER BY only governs
				// which row in each partition wins rn=1; it doesn't order the surviving rows.
				var orderByLambdas = new (LambdaExpression lambda, bool isDescending, Sql.NullsPosition nulls)[captured.Length];
				for (var i = 0; i < captured.Length; i++)
					orderByLambdas[i] = (BuildOrderByLambda(captured[i].expr, sequence.ElementType), captured[i].descending, captured[i].nulls);

				expression = WindowFunctionHelpers.ApplyOrderBy(expression, orderByLambdas);

				return builder.TryBuildSequence(new BuildInfo(buildInfo, expression));
			}

			// Rare case when outer apply is supported and window functions are not.
			// The helper builds an expression tree that re-emits OrderBy as method calls,
			// so we synthesize lambdas from captured prepared bodies (replacing the matching
			// ContextRefExpression with the lambda parameter).
			if (builder.DataContext.SqlProviderFlags.IsOuterApplyJoinSupportsCondition && buildInfo.Parent == null)
			{
				var orderByLambdas = new (LambdaExpression lambda, bool isDescending, Sql.NullsPosition nulls)[captured.Length];
				for (var i = 0; i < captured.Length; i++)
					orderByLambdas[i] = (BuildOrderByLambda(captured[i].expr, sequence.ElementType), captured[i].descending, captured[i].nulls);

				var elementType = selector.Parameters[0].Type;

				var buildMethod = _buildDistinctByViaOuterApplyMethodInfo.MakeGenericMethod(elementType, selector.Body.Type);

				var expression = (Expression)buildMethod.InvokeExt(null, [builder, sequenceExpression, orderByLambdas, selector])!;

				buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, expression));
				return buildResult;
			}

			return BuildSequenceResult.NotSupported();
		}

		/// <summary>
		/// Wraps a prepared body captured by <see cref="OrderByBuilder"/> as a lambda usable by
		/// <c>WindowFunctionHelpers.ApplyOrderBy</c>. The body's <see cref="ContextRefExpression"/>s
		/// are left intact — they resolve through the surrounding build chain at SQL-generation time.
		/// The lambda parameter is unused.
		/// </summary>
		static LambdaExpression BuildOrderByLambda(Expression body, Type elementType)
		{
			var param = Expression.Parameter(elementType, "x");
			return Expression.Lambda(body, param);
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
