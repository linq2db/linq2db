using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Distinct", nameof(LinqInternalExtensions.SelectDistinct))]
	sealed class DistinctBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Queryable.Distinct, Methods.Enumerable.Distinct, Methods.LinqToDB.SelectDistinct };

		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(_supportedMethods);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (buildResult.BuildContext == null)
				return buildResult;
			var sequence = buildResult.BuildContext;

			var sql      = sequence.SelectQuery;
			if (sql.Select.TakeValue != null || sql.Select.SkipValue != null)
			{
				sequence = new SubQueryContext(sequence);
			}

			var subQueryContext = new SubQueryContext(sequence);

			subQueryContext.SelectQuery.Select.IsDistinct = true;

			var outerSubqueryContext = new SubQueryContext(subQueryContext);

			// We do not need all fields for SelectDistinct
			//
			if (methodCall.IsSameGenericMethod(Methods.LinqToDB.SelectDistinct))
			{
				subQueryContext.SelectQuery.Select.OptimizeDistinct = true;
			}
			else
			{
				// create all columns
				var sqlExpr = builder.BuildSqlExpression(
					outerSubqueryContext,
					new ContextRefExpression(
						methodCall.Method.GetGenericArguments()[0],
						subQueryContext
					)
				);

				SequenceHelper.EnsureNoErrors(sqlExpr);
				sqlExpr = builder.UpdateNesting(outerSubqueryContext, sqlExpr);
			}

			// Drop captured OrderBy state entries that don't survive this Distinct projection.
			// Skip the placeholder collection entirely when no OrderBy has been recorded —
			// there's nothing to filter, and the entity-ref build is wasted work.
			var captured = builder.CurrentOrderBy;
			if (captured is { Count: > 0 } && !methodCall.IsSameGenericMethod(Methods.LinqToDB.SelectDistinct))
			{
				// Collect projection placeholders against `sequence` (the Distinct input) so they
				// share a coordinate system with OrderBy bodies — those were captured by
				// OrderByBuilder using PrepareBody on the same sequence, so building them here
				// against `sequence` produces structurally-comparable placeholders.
				var distinctProjection   = builder.BuildSqlExpression(sequence, SequenceHelper.CreateRef(sequence));
				var distinctPlaceholders = ExpressionBuilder.CollectDistinctPlaceholders(distinctProjection, false);

				if (distinctPlaceholders.Count > 0)
				{
					var toDrop = new HashSet<Expression>(Utils.ObjectReferenceEqualityComparer<Expression>.Default);

					foreach (var (expr, _) in captured)
					{
						var sqlExpr = builder.BuildSqlExpression(sequence, expr);

						var found = false;
						if (sqlExpr is SqlPlaceholderExpression placeholder)
						{
							foreach (var projected in distinctPlaceholders)
							{
								if (ExpressionEqualityComparer.Instance.Equals(placeholder, projected))
								{
									found = true;
									break;
								}
							}
						}

						if (!found)
							toDrop.Add(expr);
					}

					if (toDrop.Count > 0)
						builder.RemoveOrderByEntries(toDrop.Contains);
				}
			}

			return BuildSequenceResult.FromContext(new DistinctContext(outerSubqueryContext));
		}

		sealed class DistinctContext : PassThroughContext
		{
			public DistinctContext(IBuildContext context) : base(context)
			{
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsTraverse() || flags.IsRoot() || flags.IsAssociationRoot())
					return path;

				var corrected = SequenceHelper.CorrectExpression(path, this, Context);

				if (flags.IsTable() || flags.IsTraverse() || flags.IsSubquery())
					return corrected;

				Expression result;
				if (flags.IsSql() || flags.IsExpression())
				{
					result = Builder.BuildSqlExpression(Context, corrected);
					result = Builder.UpdateNesting(Context, result);
				}
				else
				{
					result = Builder.BuildExpression(Context, corrected);
				}

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DistinctContext(context.CloneContext(Context));
			}
		}
	}
}
