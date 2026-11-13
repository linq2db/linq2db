using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;

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

					foreach (var (expr, _, _) in captured)
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

				if (flags.IsTable() || flags.IsSubquery())
					return corrected;

				Expression result;
				if (flags.IsSql() || flags.IsExpression())
				{
					result = Builder.BuildSqlExpression(Context, corrected);
					result = Builder.UpdateNesting(Context, result);

					// A client-side value has no DISTINCT column, so it is exact for a SQL consumer only when the columns it reads are DISTINCT columns.
					if (flags.IsSql() && result.Find(static e => e is ContextRefExpression) != null && FindValueOutsideDistinct(result) is { } clientValue)
						return new SqlErrorExpression(clientValue, "Distinct() has no column for this client-side value.", path.Type);
				}
				else
				{
					result = Builder.BuildExpression(Context, corrected);
				}

				return result;
			}

			Expression? FindValueOutsideDistinct(Expression projection)
			{
				if (projection is SqlGenericConstructorExpression constructor)
				{
					foreach (var parameter in constructor.Parameters)
						if (FindValueOutsideDistinct(parameter.Expression) is { } found)
							return found;

					foreach (var assignment in constructor.Assignments)
						if (FindValueOutsideDistinct(assignment.Expression) is { } found)
							return found;

					return null;
				}

				if (projection.Find(static e => e is ContextRefExpression) == null)
					return null;

				if (Context is not SubQueryContext { SubQuery.SelectQuery.Select.Columns: var distinctColumns })
					return projection;

				var carried = true;

				projection.Visit(e =>
				{
					if (!carried || e is SqlPlaceholderExpression)
						return false;

					var root = e;
					while (root is MemberExpression { Expression: { } inner })
						root = inner;

					if (root is not ContextRefExpression)
						return true;

					carried = ReadsOnlyDistinctColumns(e, distinctColumns);
					return false;
				});

				return carried ? null : projection;
			}

			bool ReadsOnlyDistinctColumns(Expression input, List<SqlColumn> distinctColumns)
			{
				// Anything reached through an association is a function of its keys.
				Expression? association = null;
				for (var current = input; current is MemberExpression member; current = member.Expression)
					if (Builder.IsAssociation(member, out _))
						association = member;

				var inputs = new List<Expression>();

				if (association is MemberExpression { Expression: { } owner })
				{
					var descriptor = Builder.GetAssociationDescriptor(association, out _);
					if (descriptor is null or { ThisKey.Length: 0 } || descriptor.ExpressionPredicate != null || descriptor.Predicate != null || descriptor.ExpressionQueryMethod != null || descriptor.ExpressionQuery != null)
						return false;

					var entityDescriptor = MappingSchema.GetEntityDescriptor(owner.Type);

					foreach (var key in descriptor.ThisKey)
					{
						var column = entityDescriptor.Columns.FirstOrDefault(c => string.Equals(c.MemberName, key, StringComparison.Ordinal));
						if (column?.MemberInfo.DeclaringType?.IsAssignableFrom(owner.Type) != true)
							return false;

						inputs.Add(Expression.MakeMemberAccess(owner, column.MemberInfo));
					}
				}
				else
				{
					inputs.Add(input);
				}

				foreach (var item in inputs)
				{
					var root = item;
					while (root is MemberExpression { Expression: { } inner })
						root = inner;

					var inputContext = ((ContextRefExpression)root).BuildContext;
					var sql          = Builder.BuildSqlExpression(inputContext, item);

					if (sql.Find(static e => e is ContextRefExpression or SqlErrorExpression) != null)
						return false;

					foreach (var placeholder in ExpressionBuilder.CollectDistinctPlaceholders(sql, false))
						if (!distinctColumns.Exists(c => SelectsPlaceholder(c, placeholder)))
							return false;
				}

				return true;
			}

			// The placeholder is in the input query's coordinates; each sub-query between that query and the DISTINCT wraps its column once more.
			static bool SelectsPlaceholder(SqlColumn distinctColumn, SqlPlaceholderExpression placeholder)
			{
				for (var current = distinctColumn.Expression as SqlColumn; current != null; current = current.Expression as SqlColumn)
					if (current.Parent == placeholder.SelectQuery)
						return ReferenceEquals(current.Expression, placeholder.Sql);

				return false;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DistinctContext(context.CloneContext(Context));
			}
		}
	}
}
